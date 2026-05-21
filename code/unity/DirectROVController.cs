// =============================================================
// DirectROVController.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §3.2 / §3.3 (direct ROV control)
//
// Low-level controller binding for the simulated ROV; sits between the operator input pipeline and the pose integrator.
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DirectROVController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float maxLinearSpeed = 2.0f;   // Meters per second
    public float maxRotSpeed = 90.0f;     // Degrees per second

    [Header("Depth Settings")]
    public bool depthMode = false;
    public float depthCorrectionSpeed = 1.0f; // How fast it auto-corrects to target depth
    public float depthAdjustSpeed = 0.5f;     // How fast stick moves the target depth
    [Range(-10.0f, 0.0f)] public float desiredDepth = 0.0f;

    [Header("Input Settings")]
    [Range(0f, 0.3f)] public float deadzone = 0.1f;
    public string rightStickVertical = "RightStickY";   // Surge (Forward/Back)
    public string rightStickHorizontal = "RightStickX"; // Sway (Left/Right)
    public string leftStickVertical = "Vertical";       // Heave (Up/Down)
    public string leftStickHorizontal = "Horizontal";   // Yaw
    public string triggerAxis = "Triggers";             // Pitch
    public string bumperLeftButton = "Fire3";           // Roll -
    public string bumperRightButton = "Fire2";          // Roll +

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // IMPORTANT: For direct velocity control, we usually turn off gravity 
        // so it doesn't fight our Y-axis velocity command.
        rb.useGravity = false;

        // We set drag to 0 because we are controlling the stop manually.
        rb.drag = 0f; // (Was .drag in older Unity)
        rb.angularDrag = 0f; // (Was .angularDrag in older Unity)

        desiredDepth = transform.position.y;
    }

    private void Update()
    {
        // Calculate the desired depth setpoint in Update (runs every frame)
        if (depthMode)
        {
            float heaveIn = GetAxisSafe(leftStickVertical);
            float dt = Time.deltaTime;
            desiredDepth = Mathf.Clamp(desiredDepth + heaveIn * depthAdjustSpeed * dt, -50.0f, 0.0f);
        }
    }

    private void FixedUpdate()
    {
        // Read Inputs
        float surgeIn = GetAxisSafe(rightStickVertical);
        float swayIn = GetAxisSafe(rightStickHorizontal);
        float heaveIn = GetAxisSafe(leftStickVertical);
        float yawIn = GetAxisSafe(leftStickHorizontal);
        float pitchIn = GetAxisOptional(triggerAxis);

        bool rollNeg = GetButtonSafe(bumperLeftButton);
        bool rollPos = GetButtonSafe(bumperRightButton);
        float rollIn = (rollPos ? 1f : 0f) - (rollNeg ? 1f : 0f);

        // --- 1. Calculate Linear Velocity ---

        // Based on your previous code's axis mapping:
        // Surge = Unity X
        // Sway  = Unity Z (flipped)
        // Heave = Unity Y

        Vector3 targetLocalVelocity = Vector3.zero;

        targetLocalVelocity.x = surgeIn * maxLinearSpeed;
        targetLocalVelocity.z = -swayIn * maxLinearSpeed; // (Negative to match your previous setup)

        if (!depthMode)
        {
            // Manual Heave
            targetLocalVelocity.y = heaveIn * maxLinearSpeed;

            // Convert Local Velocity (X/Z) to World Velocity
            // But keep Y (Heave) mostly independent if you want it relative to the horizon
            // However, usually ROVs move relative to their own nose.
            // Let's transform the whole vector to World Space:
            rb.velocity = transform.TransformDirection(targetLocalVelocity);
        }
        else
        {
            // Depth Hold Mode
            // We calculate the X/Z movement relative to the ROV
            Vector3 movePlanar = new Vector3(targetLocalVelocity.x, 0, targetLocalVelocity.z);
            Vector3 worldPlanar = transform.TransformDirection(movePlanar);

            // We calculate Y movement based on Error (P-Controller for velocity)
            float depthError = desiredDepth - transform.position.y;
            float verticalSpeed = depthError * depthCorrectionSpeed;

            // Combine them
            rb.velocity = new Vector3(worldPlanar.x, verticalSpeed, worldPlanar.z);
        }

        // --- 2. Calculate Angular Velocity ---

        // Degrees/sec -> Radians/sec
        float radSpeed = maxRotSpeed * Mathf.Deg2Rad;

        Vector3 targetLocalAngularVel = new Vector3(
            pitchIn * radSpeed,  // X axis (Pitch)
            yawIn * radSpeed,    // Y axis (Yaw)
            rollIn * radSpeed    // Z axis (Roll)
        );

        // Apply directly to Rigidbody
        // This makes it stop instantly when input is 0
        rb.angularVelocity = transform.TransformDirection(targetLocalAngularVel);
    }

    // ---- Helper Methods (Same as before) ----
    private float GetAxisSafe(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0f;
        try
        {
            float v = Input.GetAxis(name);
            return Mathf.Abs(v) < deadzone ? 0f : v;
        }
        catch { return 0f; }
    }
    private float GetAxisOptional(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0f;
        try { return Input.GetAxis(name); } catch { return 0f; }
    }
    private bool GetButtonSafe(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        try { return Input.GetButton(name); } catch { return false; }
    }

    public void EnableDepthMode() { depthMode = true; desiredDepth = transform.position.y; }
    public void DisableDepthMode() { depthMode = false; }

    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 300, 60),
            $"Mode: {(depthMode ? "DEPTH HOLD" : "MANUAL")}\n" +
            $"Vel: {rb.velocity.magnitude:F1} m/s\n" +
            $"Depth: {transform.position.y:F2} / Tgt: {desiredDepth:F2}");
    }
}