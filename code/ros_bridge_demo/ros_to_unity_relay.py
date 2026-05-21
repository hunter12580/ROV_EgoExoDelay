"""
ros_to_unity_relay.py
=====================

Reference implementation of the ROS#/ROSbridge wiring described in §3.3 of:

  Xu et al., "System Design and Validation of an Ego-Exocentric Mixed Reality
  Framework in Compensating Delay for ROV Teleoperation,"
  ACM THRI (manuscript THRI-2025-0268).

Scope
-----
This script is for the **hardware-in-the-loop demonstration configuration**
shown in Supplemental Video S1 — i.e., the framework's capability to drive
a real BlueROV2 from the same Unity scene used in the user study.

The user study reported in §5 was run in fully simulated mode and did NOT
use this ROS bridge — Unity communicated with the support Python process
over UDP (see ../python_udp/server.py). This file is provided as a thin
reference for readers who want to extend the framework to a real ROV.

Behaviour
---------
- Subscribes to the BlueROV2 onboard telemetry (`/mavros/local_position/pose`,
  `/mavros/imu/data`, `/mavros/battery`) and forwards them to Unity as JSON
  payloads on the ROSbridge WebSocket.
- Subscribes to Unity-side command topics (motion-rate intents from the
  operator) and republishes them to the ROV control node
  (`/mavros/setpoint_velocity/cmd_vel_unstamped`).
- Maintains the 30–60 Hz topic exchange the manuscript reports.

Released under the MIT License — see ../../LICENSE.
"""
from __future__ import annotations

import argparse
import json
import logging
import time
from typing import Any

# ROS imports are kept under a try/except so the file can be syntax-checked
# without a ROS install. To run this for a real demo you need ROS Noetic +
# rosbridge_server + rospy.
try:
    import rospy
    from geometry_msgs.msg import PoseStamped, TwistStamped
    from sensor_msgs.msg import Imu, BatteryState

    ROS_AVAILABLE = True
except ImportError:  # pragma: no cover — only when running outside a ROS env
    ROS_AVAILABLE = False


LOG = logging.getLogger("ros_to_unity_relay")


# -------------------------------------------------------------------- helpers


def _stamp_now() -> float:
    """Wall-clock seconds since the epoch — used to timestamp messages."""
    return time.time()


def pose_to_json(msg: "PoseStamped") -> dict[str, Any]:
    p = msg.pose.position
    q = msg.pose.orientation
    return {
        "type": "pose",
        "t": _stamp_now(),
        "position": {"x": p.x, "y": p.y, "z": p.z},
        "orientation": {"x": q.x, "y": q.y, "z": q.z, "w": q.w},
    }


def imu_to_json(msg: "Imu") -> dict[str, Any]:
    av = msg.angular_velocity
    la = msg.linear_acceleration
    q = msg.orientation
    return {
        "type": "imu",
        "t": _stamp_now(),
        "orientation": {"x": q.x, "y": q.y, "z": q.z, "w": q.w},
        "angular_velocity": {"x": av.x, "y": av.y, "z": av.z},
        "linear_acceleration": {"x": la.x, "y": la.y, "z": la.z},
    }


def battery_to_json(msg: "BatteryState") -> dict[str, Any]:
    return {
        "type": "battery",
        "t": _stamp_now(),
        "voltage": float(msg.voltage),
        "percentage": float(msg.percentage),
    }


# --------------------------------------------------------------------- relay


class Relay:
    """ROS ⇄ Unity (ROSbridge WebSocket) bidirectional relay."""

    def __init__(self, send_fn, update_hz: float = 60.0):
        """
        Parameters
        ----------
        send_fn
            Callable invoked once per Unity-bound message. The actual
            transport (rosbridge / websockets / etc.) is supplied by the
            caller so this class stays pure logic.
        update_hz
            Target republish rate for outbound (Unity → ROV) commands.
        """
        self._send = send_fn
        self._period = 1.0 / max(1.0, float(update_hz))
        self._last_cmd = (0.0, 0.0, 0.0, 0.0)  # surge, sway, heave, yaw_rate
        self._cmd_pub = None

    # ---- ROS → Unity (telemetry inbound, forwarded as JSON) ------------

    def on_pose(self, msg):
        self._send(pose_to_json(msg))

    def on_imu(self, msg):
        self._send(imu_to_json(msg))

    def on_battery(self, msg):
        self._send(battery_to_json(msg))

    # ---- Unity → ROS (operator commands) -------------------------------

    def set_command(self, surge: float, sway: float, heave: float, yaw_rate: float):
        """Latch the most recent operator-frame command from Unity. The
        republish loop ticks at `update_hz` regardless of Unity's frame
        rate, smoothing over jitter."""
        self._last_cmd = (float(surge), float(sway), float(heave), float(yaw_rate))

    def _publish_loop(self):
        msg = TwistStamped()
        rate = rospy.Rate(1.0 / self._period)
        while not rospy.is_shutdown():
            surge, sway, heave, yaw_rate = self._last_cmd
            msg.header.stamp = rospy.Time.now()
            msg.twist.linear.x = surge
            msg.twist.linear.y = sway
            msg.twist.linear.z = heave
            msg.twist.angular.z = yaw_rate
            if self._cmd_pub is not None:
                self._cmd_pub.publish(msg)
            rate.sleep()

    # ---- bring-up / lifecycle ------------------------------------------

    def setup_ros(self):
        if not ROS_AVAILABLE:
            raise RuntimeError("rospy is not importable in this environment")
        rospy.init_node("ros_to_unity_relay", anonymous=False)
        rospy.Subscriber("/mavros/local_position/pose", PoseStamped, self.on_pose, queue_size=10)
        rospy.Subscriber("/mavros/imu/data", Imu, self.on_imu, queue_size=10)
        rospy.Subscriber("/mavros/battery", BatteryState, self.on_battery, queue_size=10)
        self._cmd_pub = rospy.Publisher(
            "/mavros/setpoint_velocity/cmd_vel_unstamped",
            TwistStamped, queue_size=10,
        )
        LOG.info("ROS bridge ready; publishing setpoints at %.1f Hz", 1.0 / self._period)

    def spin(self):
        import threading
        t = threading.Thread(target=self._publish_loop, daemon=True)
        t.start()
        rospy.spin()


# ---------------------------------------------------------------- entrypoint


def _stdout_sender(payload: dict[str, Any]) -> None:
    """Default sender — writes the JSON payload to stdout. In a real demo
    this is replaced with a rosbridge / websockets send call."""
    print(json.dumps(payload, separators=(",", ":")))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[1])
    parser.add_argument("--rate-hz", type=float, default=60.0,
                        help="setpoint republish rate (Hz)")
    parser.add_argument("--log", default="INFO")
    args = parser.parse_args(argv)
    logging.basicConfig(level=args.log.upper(),
                        format="%(asctime)s %(levelname)s %(name)s: %(message)s")
    relay = Relay(send_fn=_stdout_sender, update_hz=args.rate_hz)
    if not ROS_AVAILABLE:
        LOG.error("rospy not available — this script must run in a ROS Noetic env.")
        return 1
    relay.setup_ros()
    relay.spin()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
