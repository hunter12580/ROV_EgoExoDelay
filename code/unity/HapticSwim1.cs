// =============================================================
// HapticSwim1.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §3.2 (TactSuit 8-zone mapping; Fig. 2 and Fig. 2b)
//
// Maps the ROV's per-sector proximity to vibrotactile intensities on the bHaptics TactSuit X40. The 40 actuators are grouped into 8 directional zones around the torso; intensity is a monotonically decreasing function of zone-minimum distance to the tunnel mesh.
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
﻿using UnityEngine;
using Bhaptics.SDK2;
using System.Collections;

public class HapticSwim1 : MonoBehaviour
{
    public ROVWallDetector wallDetector;
    public float refreshRate = 0.1f;

    [Header("Haptic Settings")]
    public int duration = 200;
    public bool hapticsEnabled = true;

    [Header("Tube Settings")]
    public float tubeScaleBy2;
    public float rovScale;
    public float proximityTriggerDistance = 1f;

    [Header("Intensity Range")]
    public int startIntensity;
    public int topIntensity;

    [Header("UI Elements")]
    public GameObject centerCircle;
    public GameObject arrow;

    private float timer = 0f;

    [HideInInspector]
    public float defaultHapticDelay = 0f;


    private readonly int[][] motorZones = new int[][]
    {
        new int[] { 19, 23, 27, 31 },
        new int[] { 18, 22, 26, 30 },
        new int[] { 3, 7, 11, 15 },
        new int[] { 2, 6, 10, 14 },
        new int[] { 17, 21, 25, 29 },
        new int[] { 16, 20, 24, 28 },
        new int[] { 0, 4, 8, 12 },
        new int[] { 1, 5, 9, 13 }
    };

    void Update()
    {
        if (wallDetector == null || wallDetector.closestDirectionIndex == -1)
            return;

        timer += Time.deltaTime;
        if (timer >= refreshRate)
        {
            timer = 0f;
            HandleHapticAndUI(wallDetector.closestDirectionIndex);
        }
    }

    void HandleHapticAndUI(int directionIndex)
    {
        Vector3 localDir = GetDirectionVector(directionIndex);
        float angle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;
        int zone = GetZoneFromAngle(angle);
        float distance = wallDetector.GetRayHitDistance(directionIndex);

        float maxTriggerDist = (tubeScaleBy2 - rovScale) / 2f;

        if (distance <= 0f || distance > maxTriggerDist || distance > proximityTriggerDistance)
        {
            ShowCenterUI(true);
            return;
        }

        ShowCenterUI(false);

        float normalized = Mathf.InverseLerp(proximityTriggerDistance, 0f, distance);
        int scaledIntensity = Mathf.RoundToInt(Mathf.Lerp(startIntensity, topIntensity, normalized));

        if (!hapticsEnabled) return;

        // Example default delay for internal call; you can remove this block if delays are fully external
        float internalDelay = defaultHapticDelay;

        TriggerHaptic(zone, scaledIntensity, duration, internalDelay);

    }

    public void TriggerHaptic(int zone, int intensity, int durationMs, float delaySeconds)
    {
        int[] motorValues = new int[32];
        foreach (int index in motorZones[zone])
        {
            motorValues[index] = intensity;
        }

        StartCoroutine(PlayHapticWithDelay(motorValues, durationMs, delaySeconds));
    }

    private IEnumerator PlayHapticWithDelay(int[] motorValues, int durationMs, float delaySeconds)
    {
        if (delaySeconds > 0f)
            yield return new WaitForSeconds(delaySeconds);

        BhapticsLibrary.PlayMotors((int)PositionType.Vest, motorValues, durationMs);
    }

    private void ShowCenterUI(bool isCentered)
    {
        if (centerCircle != null) centerCircle.SetActive(isCentered);
        if (arrow != null) arrow.SetActive(!isCentered);
    }

    private Vector3 GetDirectionVector(int index)
    {
        float angleDeg = index * 10f;
        float angleRad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f).normalized;
    }

    private int GetZoneFromAngle(float angle)
    {
        if (angle >= 0 && angle < 45) return 0;
        if (angle >= 45 && angle < 90) return 1;
        if (angle < 0 && angle >= -45) return 2;
        if (angle < -45 && angle >= -90) return 3;
        if (angle >= 90 && angle < 135) return 4;
        if (angle >= 135 && angle <= 180) return 5;
        if (angle <= -135 && angle >= -180) return 6;
        if (angle < -90 && angle > -135) return 7;
        return 0;
    }
}
