// =============================================================
// ConditionSelector.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §4.1 (Latin-square condition randomization)
//
// Selects and configures the active interaction mode (Egocentric / Haptic-augmented Egocentric / Exocentric) and the per-trial delay (0 / 0.2 / 0.5 / 1.0 s) according to a Latin-square ordering.
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
using System.Collections.Generic;
using UnityEngine;

public class ConditionSelector : MonoBehaviour
{
    [Header("Tube Objects (Assign in Order: Condition 1 to 8)")]
    public List<GameObject> tubeObjects;

    [Header("ROV GameObject with RovPoseControl")]
    public GameObject rov;

    [Header("Haptic Controller (Assign HapticSwim1 Script Here)")]
    public HapticSwim1 hapticSwimController;

    private ExperimentDataLogger dataLogger;
    private bool experimentStarted = false;

    [Header("Wall Detector (ROVWallDetector)")]
    public ROVWallDetector wallDetector;

    [Header("Visual Delay System")]
    public MultiDelayedCameraSystem cameraSystem;

    public GameObject ArrowUI;

    void Start()
    {
        dataLogger = FindObjectOfType<ExperimentDataLogger>();

        if (dataLogger == null)
        {
            Debug.LogError("ExperimentDataLogger not found in the scene.");
            return;
        }

        int condition = dataLogger.condition;

        // Deactivate all tubes
        foreach (var tube in tubeObjects)
        {
            if (tube != null)
                tube.SetActive(false);
        }

        // Activate the selected tube based on condition (1�8)
        //if (condition >= 1 && condition <= tubeObjects.Count)
        //{
        //    GameObject selectedTube = tubeObjects[condition - 1];
        //    if (selectedTube != null)
        //        selectedTube.SetActive(true);
        //}
        if (condition >= 1 && condition <= tubeObjects.Count)
        {
            GameObject selectedTube = tubeObjects[condition - 1];
            if (selectedTube != null)
            {
                selectedTube.SetActive(true);

                // Assign tube collider to wall detector
                if (wallDetector != null)
                {
                    Collider tubeCol = selectedTube.GetComponent<Collider>();
                    if (tubeCol != null)
                    {
                        wallDetector.tubeCollider = tubeCol;
                        Debug.Log($"[ConditionSelector] Assigned tube collider to wall detector (Condition {condition})");
                    }
                    else
                    {
                        Debug.LogWarning("[ConditionSelector] Selected tube has no Collider component.");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Condition out of range. No tube activated.");
        }

        // Disable ROV control script at start
        if (rov != null)
        {
            var controlScript = rov.GetComponent<RovPoseControl>();
            if (controlScript != null)
                controlScript.enabled = false;
        }

        // Set haptic feedback settings based on the condition
        if (hapticSwimController != null)
        {
            // Default: haptics disabled
            hapticSwimController.hapticsEnabled = false;
            hapticSwimController.defaultHapticDelay = 0f;

            switch (condition)
            {
                case 5:
                    hapticSwimController.hapticsEnabled = true;
                    hapticSwimController.defaultHapticDelay = 0f;
                    break;
                case 6:
                    hapticSwimController.hapticsEnabled = true;
                    hapticSwimController.defaultHapticDelay = 0.5f;
                    break;
                case 7:
                    hapticSwimController.hapticsEnabled = true;
                    hapticSwimController.defaultHapticDelay = 1.0f;
                    break;
                case 8:
                    hapticSwimController.hapticsEnabled = true;
                    hapticSwimController.defaultHapticDelay = 0.2f;
                    break;
            }

            Debug.Log($"[ConditionSelector] Haptics Enabled: {hapticSwimController.hapticsEnabled}, Delay: {hapticSwimController.defaultHapticDelay}s");
        }

        // Set visual delay based on conditions C1�C4
        if (cameraSystem != null && cameraSystem.cameraFeeds != null)
        {
            float visualDelay = 0f;

            switch (condition)
            {
                case 1:
                    visualDelay = 0f;
                    break;
                case 2:
                    visualDelay = 0.5f;
                    break;
                case 3:
                    visualDelay = 1f;
                    break;
                case 4:
                    visualDelay = 0.2f;
                    break;
                case 5:
                    visualDelay = 0f;
                    break;
                case 6:
                    visualDelay = 0.5f;
                    break;
                case 7:
                    visualDelay = 1f;
                    break;
                case 8:
                    visualDelay = 0.2f;
                    break;
                default:
                    visualDelay = 0f;
                    break;
            }

            foreach (var feed in cameraSystem.cameraFeeds)
            {
                feed.delayInSeconds = visualDelay;
            }

            Debug.Log($"[ConditionSelector] Visual delay set to {visualDelay}s for condition {condition}");
        }

        // Hide ArrowUI during haptic feedback conditions (C5�C8)
        if (ArrowUI != null)
        {
            bool isHapticCondition = condition >= 5 && condition <= 8;
            ArrowUI.SetActive(!isHapticCondition);

            Debug.Log($"[ConditionSelector] ArrowUI visibility set to {!isHapticCondition} (Condition {condition})");
        }
    }

    void Update()
    {
        if (!experimentStarted && Input.GetKeyDown(KeyCode.S))
        {
            experimentStarted = true;
            EnableROVControl();
            Debug.Log("Experiment started: ROV control enabled.");
        }
    }

    void EnableROVControl()
    {
        if (rov != null)
        {
            var controlScript = rov.GetComponent<RovKeyboardControl>();
            if (controlScript != null)
                controlScript.enabled = true;
        }
    }
}
