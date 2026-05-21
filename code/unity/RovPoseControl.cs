// =============================================================
// RovPoseControl.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §3.2 / §3.3 (pose control and dynamics)
//
// Integrates body-frame command-rate inputs into the simulated BlueROV2 pose. Replicates the BlueROV2 acceleration, inertia, and thrust-response envelopes used in the user study.
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
using UnityEngine;
using Valve.VR;

public class RovPoseControl : MonoBehaviour
{
    [Header("ROV Object")]
    public GameObject ROV; // The ROV GameObject

    [Header("Speed Settings")]
    public float maxSpeed = 2f; // Maximum forward speed from trigger input

    // Rotation speeds (degrees/sec) when controller is at �maxAngle
    public float maxPitchSpeed = 20f;
    public float maxYawSpeed = 30f;
    public float maxRollSpeed = 20f;

    // Maximum tilt offsets (degrees)
    public float maxPitchAngle = 20f;
    public float maxYawAngle = 20f;
    public float maxRollAngle = 20f;

    [Header("Roll Control Settings")]
    // When true, the ROV's roll (Z-axis rotation) is forced to zero.
    public bool disableRoll = true;

    [Header("Neutral Handle Orientation")]
    public Vector3 neutralEulerAngles = Vector3.zero;

    [Header("Stabilization Settings")]
    public float stableZone = 5f;   // e.g. �5 degrees
    public float stableTime = 1f;   // Time before stabilization is applied
    private float stableTimer = 0f;

    [Header("Smoothing")]
    public float rotationSmoothSpeed = 5f;
    private Vector3 currentRotSpeed;

    [Header("SteamVR Input")]
    public SteamVR_Behaviour_Pose rightControllerPose;
    public SteamVR_Action_Single triggerAction;

    [Header("Forward Movement Settings")]
    public float forwardAcceleration = 2f;   // Acceleration rate (m/s�)
    public float collisionDragFactor = 0.5f;   // When colliding, target speed is multiplied by this factor (e.g., 0.5 for 50%)

    private float currentForwardSpeed = 0f; // Current forward speed, used to simulate inertia

    private Quaternion neutralRotation;
    private bool initDone = false;

    // Rigidbody reference for the ROV.
    private Rigidbody rovRigidbody;

    // Flag to indicate if the ROV is colliding with the tube's wall.
    private bool isColliding = false;

    void Start()
    {
        if (!ROV)
        {
            Debug.LogWarning("ROV not assigned!");
            return;
        }
        if (!rightControllerPose)
        {
            Debug.LogWarning("RightControllerPose not assigned!");
            return;
        }

        // Build the neutral rotation from inspector values.
        neutralRotation = Quaternion.Euler(neutralEulerAngles);
        currentRotSpeed = Vector3.zero;

        rovRigidbody = ROV.GetComponent<Rigidbody>();
        if (rovRigidbody == null)
        {
            Debug.LogWarning("Rigidbody not found on ROV. Please add one.");
        }
        else
        {
            // Use a dynamic rigidbody for proper collision resolution.
            rovRigidbody.isKinematic = false;
            rovRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        initDone = true;
    }

    void FixedUpdate()
    {
        if (!initDone || ROV == null || rightControllerPose == null || triggerAction == null)
            return;

        // 1) Calculate offset from the neutral orientation.
        Quaternion controllerRotation = rightControllerPose.transform.rotation;
        Quaternion offsetQ = Quaternion.Inverse(neutralRotation) * controllerRotation;
        Vector3 offsetEuler = offsetQ.eulerAngles;

        // Convert angles to the range [-180, 180]
        float offsetPitch = NormalizeAngle(offsetEuler.x);
        float offsetYaw = NormalizeAngle(offsetEuler.y);
        float offsetRoll = NormalizeAngle(offsetEuler.z);

        // 2) Clamp the offsets.
        offsetPitch = Mathf.Clamp(offsetPitch, -maxPitchAngle, maxPitchAngle);
        offsetYaw = Mathf.Clamp(offsetYaw, -maxYawAngle, maxYawAngle);
        offsetRoll = Mathf.Clamp(offsetRoll, -maxRollAngle, maxRollAngle);

        // 3) Stabilization: if all offsets are within the stable zone for stableTime, set them to zero.
        bool withinStableZone =
            Mathf.Abs(offsetPitch) < stableZone &&
            Mathf.Abs(offsetYaw) < stableZone &&
            Mathf.Abs(offsetRoll) < stableZone;

        if (withinStableZone)
            stableTimer += Time.fixedDeltaTime;
        else
            stableTimer = 0f;

        if (stableTimer >= stableTime)
        {
            offsetPitch = 0f;
            offsetYaw = 0f;
            offsetRoll = 0f;
        }

        // 4) Calculate desired rotation speeds.
        float desiredPitchSpeed = (offsetPitch / maxPitchAngle) * maxPitchSpeed;
        float desiredYawSpeed = (offsetYaw / maxYawAngle) * maxYawSpeed;
        float desiredRollSpeed = (offsetRoll / maxRollAngle) * maxRollSpeed;
        if (disableRoll)
            desiredRollSpeed = 0f;
        Vector3 desiredRotSpeed = new Vector3(desiredPitchSpeed, desiredYawSpeed, desiredRollSpeed);

        // 5) Smoothly adjust the rotation speeds.
        currentRotSpeed = Vector3.Lerp(currentRotSpeed, desiredRotSpeed, rotationSmoothSpeed * Time.fixedDeltaTime);

        float deltaPitch = currentRotSpeed.x * Time.fixedDeltaTime;
        float deltaYaw = currentRotSpeed.y * Time.fixedDeltaTime;
        float deltaRoll = currentRotSpeed.z * Time.fixedDeltaTime;

        Quaternion deltaRotation = Quaternion.Euler(deltaPitch, deltaYaw, deltaRoll);
        Quaternion newRotation = rovRigidbody.rotation * deltaRotation;
        if (disableRoll)
        {
            Vector3 euler = newRotation.eulerAngles;
            euler.z = 0f;
            newRotation = Quaternion.Euler(euler);
        }
        rovRigidbody.MoveRotation(newRotation);

        // 6) Forward movement: compute target speed from trigger input and apply acceleration.
        float triggerValue = triggerAction.GetAxis(SteamVR_Input_Sources.RightHand);
        float targetSpeed = triggerValue * maxSpeed;

        // Apply a continuous downward velocity to make control more challenging
        Vector3 downwardVelocity = Vector3.down * 0.0f; // Tune this value for difficulty
        rovRigidbody.velocity += downwardVelocity * Time.fixedDeltaTime;


        // If colliding with the tube, reduce the target speed by the drag factor.
        if (isColliding)
            targetSpeed *= collisionDragFactor;

        // Gradually accelerate (or decelerate) to the target speed.
        currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, forwardAcceleration * Time.fixedDeltaTime);

        // Move the ROV forward.
        Vector3 movement = ROV.transform.forward * currentForwardSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = rovRigidbody.position + movement;
        rovRigidbody.MovePosition(newPosition);
    }

    // Collision detection for applying collision drag.
    // This example assumes that the tube's wall GameObject has the tag "Tube".
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Tube"))
            isColliding = true;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Tube"))
            isColliding = false;
    }

    /// <summary>
    /// Converts an angle from the range [0, 360] to [-180, 180].
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
