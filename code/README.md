# `code/` — system source for the THRI artifact

This folder contains the source used to run the experiment and to drive
the hardware-in-the-loop demonstration described in the manuscript.

The codebase has two operating modes:

1. **User-study mode** (the experiment reported in §5)
   Unity + Python communicating over UDP.
   → see `unity/` and `python_udp/`.

2. **Hardware-in-the-loop demonstration mode** (Supplemental Video S1, described in §3.3)
   Unity ↔ ROS#/ROSbridge ↔ real BlueROV2 via MAVROS.
   → see `ros_bridge_demo/`.

> **Note on EgoExo++:** the exocentric-view synthesis algorithm itself is
> from Abdullah et al. (IJRR 2025 — arxiv
> [2407.00848](https://arxiv.org/abs/2407.00848)) and is not bundled
> here. Contact the RoboPI lab (adnanabdullah@ufl.edu) for access to the
> EgoExo++ implementation.

---

## File → paper-claim map

### `unity/` — the user-study system (Unity, C#)

| File                              | Paper reference                                            |
|-----------------------------------|------------------------------------------------------------|
| `MultiDelayedCameraSystem.cs`     | §4 — per-trial delay injection on the visual channel (FIFO of timestamped RenderTextures; 0 / 0.2 / 0.5 / 1.0 s). |
| `HapticSwim1.cs`                  | §3.2, Fig. 2, Fig. 2b — TactSuit X40 8-zone proximity → vibrotactile mapping. |
| `ROVWallDetector.cs`              | §3.2, Fig. 2b — proximity-detection raycasts (8 sectors).  |
| `CaveRayEmitter.cs`               | Fig. 2b panel (d) — detection-ray visualisation in the GUI overlay. |
| `ExperimentDataLogger.cs`         | §4 — per-frame trial logging that feeds `../data/per_trial_metrics.csv`. |
| `ConditionSelector.cs`            | §4.1 — Latin-square condition randomization (modality × delay). |
| `RovKeyboardControl.cs`           | §3.2 — operator-input handling and safety filters (smoothing, dead-zone, motion-scaling). |
| `RovPoseControl.cs`               | §3.2 / §3.3 — body-frame motion integration with BlueROV2-matched dynamics. |
| `DirectROVController.cs`          | §3.2 / §3.3 — low-level ROV controller binding.            |
| `ProceduralTankGenerator.cs`      | §4.2, Fig. 5 — procedurally-generated tunnel geometry.     |

### `python_udp/` — support process for the user study

| File           | Paper reference                                                          |
|----------------|--------------------------------------------------------------------------|
| `server.py`    | §3.3 — UDP server that mediates between Unity and external processes.    |
| `UdpComms.py`  | §3.3 — async UDP transport helper used by `server.py`.                   |

### `ros_bridge_demo/` — extension wiring for the §3.3 demo (NOT the experiment loop)

| File                      | Paper reference                                                            |
|---------------------------|----------------------------------------------------------------------------|
| `ros_to_unity_relay.py`   | §3.3 — bidirectional ROS ↔ Unity relay (MAVROS pose / IMU / battery in; velocity setpoints out). |
| `rosbridge_params.yaml`   | §3.3 — ROSbridge WebSocket configuration (port, message size, topic whitelist). |

---

## How to reproduce the user study

1. Open the Unity project (Unity 2021 LTS, HDRP).
2. Drop the `unity/` scripts into a folder under `Assets/` (they were
   originally under `Delay_exp/`).
3. Start the Python side: `python3 python_udp/server.py`.
4. Enter Play mode in Unity. `ConditionSelector` walks through the
   Latin-square trial order; `ExperimentDataLogger` writes per-trial CSV
   files into `Assets/Result/` (re-aggregate into `per_trial_metrics.csv`).
5. The HMD (HTC Vive Pro) and TactSuit X40 must be connected for the
   immersive and haptic modes.

---

## License

All source files in this folder are released under the MIT License — see
[`../LICENSE`](../LICENSE).
