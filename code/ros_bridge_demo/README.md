# `ros_bridge_demo/` — reference wiring for the §3.3 demonstration mode

This folder contains the ROS#/ROSbridge configuration described in
**§3.3** of the manuscript. It is the wiring used for the
**hardware-in-the-loop demonstration** with a real BlueROV2 — i.e., what
Supplemental Video S1 shows.

> **This bridge is not the user-study loop.** The user study in §5 was
> run in fully simulated mode and communicated between Unity and a
> support Python process over UDP (see `../python_udp/`). The ROS bridge
> here is provided so a reader can extend the framework to a real ROV.

## Files

| File                     | Purpose                                                                                           |
|--------------------------|---------------------------------------------------------------------------------------------------|
| `ros_to_unity_relay.py`  | Bidirectional ROS ↔ Unity relay: forwards MAVROS pose / IMU / battery to Unity as JSON; forwards Unity-side velocity setpoints to MAVROS. |
| `rosbridge_params.yaml`  | ROSbridge WebSocket parameters (port, message size, topic whitelist, twin update rate).            |

## Quick start (only needed if you want to drive a real ROV)

```bash
# Ubuntu 20.04 + ROS Noetic prerequisites
sudo apt install ros-noetic-rosbridge-server ros-noetic-mavros

# Bring up rosbridge with these params
roslaunch rosbridge_server rosbridge_websocket.launch \
    port:=9090 fragment_size:=16384 max_message_size:=100000000

# Run the relay
python3 ros_to_unity_relay.py --rate-hz 60
```

In Unity, configure the ROS# `RosConnector` component to point at
`ws://<host>:9090` and subscribe to the topics listed in
`rosbridge_params.yaml` → `outbound`.

## EgoExo++ exocentric stream

The `/egoexo/exo_view` topic in `outbound` carries the synthesised
third-person frames produced by the **EgoExo++** algorithm (Abdullah et
al., IJRR 2025 — arxiv [2407.00848](https://arxiv.org/abs/2407.00848)).
The EgoExo++ implementation itself is **not** included here — please
contact the RoboPI lab (adnanabdullah@ufl.edu) for access.
