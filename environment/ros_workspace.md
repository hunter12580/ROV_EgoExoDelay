# ROS workspace notes (for `code/`)

The integration code in `../code/` is written against the following
environment, used for the study reported in the paper:

| Component         | Version                                |
|-------------------|----------------------------------------|
| Host OS           | Ubuntu 20.04 LTS                       |
| ROS               | ROS Noetic                             |
| Unity             | 2021.x LTS                             |
| SteamVR           | Current stable (≥ 2022)                |
| ROS#              | Latest, with WebSocket / ROSbridge     |
| bHaptics SDK      | TactSuit X40 Unity SDK                 |
| HMD               | HTC Vive Pro                           |

## Quick setup

```bash
# ROS workspace
sudo apt install ros-noetic-rosbridge-server
mkdir -p ~/catkin_ws/src && cd ~/catkin_ws
catkin_make
source devel/setup.bash

# Drop the code/ contents into a ROS package or into the Unity project
# (the delay-injection node is a stand-alone Python ROS node; the
# tactsuit_mapping and trial_logger are Unity-side C# scripts in the
# original implementation — adapt as needed).
```

## EgoExo++ dependency

The full system also requires the EgoExo++ implementation from
Abdullah et al. 2024 (ISRR), which is not redistributed here. See the
RoboPI lab page for access.
