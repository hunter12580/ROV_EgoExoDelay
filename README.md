# ROV-EgoExo Teleoperation Study — Code & Materials

**Repository:** [https://github.com/hunter12580/ROV_EgoExoDelay](https://github.com/hunter12580/ROV_EgoExoDelay)
**Manuscript:** ACM Transactions on Human-Robot Interaction (THRI-2025-0268)

This repository accompanies our ACM THRI paper *"System Design and Validation
of an Ego-Exocentric Mixed Reality Framework in Compensating Delay for ROV
Teleoperation"* (manuscript ID THRI-2025-0268).

It provides the **system source** used to run the user study and a
reference ROS-bridge configuration for the hardware-in-the-loop
demonstration mode described in §3.3 and shown in Supplemental Video S1.

> **Data availability.** The de-identified per-trial behavioural and
> questionnaire data underlying §5 are **not redistributed in this
> repository** owing to the Institutional Review Board (IRB) restrictions of
> the approving protocol (IRB202301365, University of Florida). The data
> are available from the corresponding author upon reasonable request,
> subject to those IRB conditions.

---

## Repository layout

```
.
├── README.md                          (this file)
├── LICENSE                            MIT (code)
├── CITATION.cff                       Citation metadata
│
├── code/
│   ├── README.md                      Claim → file map; experiment vs. demo mode
│   │
│   ├── unity/                         User-study system (Unity C#)
│   │   ├── MultiDelayedCameraSystem.cs
│   │   ├── HapticSwim1.cs
│   │   ├── ROVWallDetector.cs
│   │   ├── CaveRayEmitter.cs
│   │   ├── ExperimentDataLogger.cs
│   │   ├── ConditionSelector.cs
│   │   ├── RovKeyboardControl.cs
│   │   ├── RovPoseControl.cs
│   │   ├── DirectROVController.cs
│   │   └── ProceduralTankGenerator.cs
│   │
│   ├── python_udp/                    User-study support process
│   │   ├── server.py
│   │   └── UdpComms.py
│   │
│   └── ros_bridge_demo/               Reference wiring for §3.3 demo mode
│       ├── README.md
│       ├── ros_to_unity_relay.py
│       └── rosbridge_params.yaml
│
├── study_materials/                   IRB-approved instruments
│   └── README.md                      (PDFs to be added by corresponding author)
│
└── environment/
    └── ros_workspace.md               ROS Noetic setup notes for the demo mode
```

---

## Two operating modes

The codebase supports two configurations of the framework, as described in
the manuscript:

### 1. User-study mode (Unity + Python over UDP)

The configuration used for the experiment reported in §5. Unity runs the
simulated tunnel environment, applies the per-trial visual delay via
`MultiDelayedCameraSystem`, drives the TactSuit, and logs per-trial data
locally. A small Python process (`code/python_udp/server.py`) provides a
UDP transport for external utilities (eye-tracker bridge, optional ROS
bridge, logging).

Run order:

```bash
# 1) start the Python support process
python3 code/python_udp/server.py

# 2) open the Unity project (Unity 2021 LTS, HDRP), copy the contents of
#    code/unity/ into Assets/Delay_exp/Delay_exp/, attach the components
#    to the scene as documented in each script's header, and press Play.
```

The HMD (HTC Vive Pro) and TactSuit X40 must be connected for the
immersive and haptic modes.

### 2. Hardware-in-the-loop demonstration mode (Unity ↔ ROS ↔ BlueROV2)

The configuration shown in **Supplemental Video S1** and described in
**§3.3**. The same Unity scene speaks to MAVROS through a ROS#/ROSbridge
WebSocket using the relay in `code/ros_bridge_demo/`.

> **This mode was not used in the user study.** It is provided for readers
> who want to extend the framework to a real ROV.

See `code/ros_bridge_demo/README.md` for the bring-up sequence.

---

## EgoExo++ reference

The exocentric-view synthesis is from EgoExo++, introduced in:

> Abdullah, A., Chen, R., Rekleitis, I., & Islam, M. J. (2025).
> *EgoExo++: Integrating On-demand Exocentric Visuals with 2.5D Ground
> Surface Estimation for Interactive Teleoperation of Underwater ROVs.*
> International Journal of Robotics Research — arxiv
> [2407.00848](https://arxiv.org/abs/2407.00848).
> Demo video: [https://youtu.be/xpvnzIJ_YbM](https://youtu.be/xpvnzIJ_YbM).

The EgoExo++ algorithm implementation is not bundled here — please
contact the RoboPI lab (adnanabdullah@ufl.edu) for access.

---

## Requesting access to the trial data

Researchers seeking the de-identified per-trial behavioural metrics,
NASA-TLX, and SUS data underlying §5 may request access by emailing the
corresponding author at the address listed in the manuscript. Requests
that satisfy the IRB protocol (academic research purpose, no
re-identification attempts, no redistribution) will be granted. Approved
requests typically receive the data within one to two weeks, with a
data-use addendum specifying the IRB-imposed conditions.

---

## License

The source code in this repository (everything in `code/`) is released
under the **MIT License** — see [`LICENSE`](LICENSE). The study
materials and the (privately-released) data are governed by IRB
conditions described above.

---

## How to cite

See [`CITATION.cff`](CITATION.cff) (machine-readable) and the THRI paper
itself. If you reuse this code directly, please cite both.
