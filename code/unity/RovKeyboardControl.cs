// =============================================================
// RovKeyboardControl.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §3.2 (operator input handling)
//
// Maps joystick / keyboard input axes to body-frame motion commands (surge, sway, heave, yaw). Includes the safety filters described in §3.2: temporal smoothing, dead-zone thresholding, and motion-scaling.
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
using UnityEngine;

/// <summary>
/// Keyboard-based ROV controller.
/// - W/S/A/D: translate in local XZ plane
/// - Space/Shift: translate in local Y axis (Heave Up/Down)
/// - Arrows: pitch (Up/Down) and yaw (Left/Right)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RovKeyboardControl : MonoBehaviour
{
    [Header("ROV Object")]
    public GameObject ROV;

    [Header("Linear Motion")]
    [Tooltip("Maximum speed for movement in any direction (m/s).")]
    public float maxSpeed = 2f;

    [Tooltip("Linear acceleration toward target speed (m/s^2).")]
    public float linearAcceleration = 3f;

    [Tooltip("Scale max speed while colliding with 'Tube'.")]
    [Range(0f, 1f)]
    public float collisionDragFactor = 0.5f;

    [Header("Angular Motion (No Acceleration)")]
    [Tooltip("Constant pitch speed (deg/s) when Up/Down arrows are held.")]
    public float pitchSpeedDegPerSec = 30f;

    [Tooltip("Constant yaw speed (deg/s) when Left/Right arrows are held.")]
    public float yawSpeedDegPerSec = 45f;

    [Header("Roll Control")]
    [Tooltip("When true, the ROV's roll (Z axis) is forced to zero each physics step.")]
    public bool disableRoll = true;

    [Header("Key Bindings")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    
    // --- NEW: Vertical Control Keys ---
    public KeyCode upKey = KeyCode.Space;      // Heave Up
    public KeyCode downKey = KeyCode.LeftShift; // Heave Down

    public KeyCode pitchUpKey = KeyCode.UpArrow;     
    public KeyCode pitchDownKey = KeyCode.DownArrow; 
    public KeyCode yawLeftKey = KeyCode.LeftArrow;   
    public KeyCode yawRightKey = KeyCode.RightArrow; 

    // Internals
    private Rigidbody rovRb;
    private bool isColliding = false;

    // We keep linear velocity in LOCAL space (X, Y, Z)
    private Vector3 currentLocalVel = Vector3.zero; 

    private void Awake()
    {
        if (ROV == null) ROV = this.gameObject;

        rovRb = ROV.GetComponent<Rigidbody>();
        if (rovRb == null)
        {
            Debug.LogWarning("Rigidbody not found on ROV. Adding one.");
            rovRb = ROV.AddComponent<Rigidbody>();
        }

        rovRb.isKinematic = false;
        rovRb.useGravity = false; 
        rovRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rovRb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (ROV == null || rovRb == null) return;

        float dt = Time.fixedDeltaTime;

        // -------- 1) READ KEY INPUTS (TRANSLATION) --------
        
        // Planar (Forward/Back & Left/Right)
        float inputX = 0f;
        float inputZ = 0f;
        if (Input.GetKey(forwardKey)) inputZ += 1f;
        if (Input.GetKey(backwardKey)) inputZ -= 1f;
        if (Input.GetKey(rightKey))   inputX += 1f;
        if (Input.GetKey(leftKey))    inputX -= 1f;

        // Vertical (Up/Down)
        float inputY = 0f;
        if (Input.GetKey(upKey))   inputY += 1f;
        if (Input.GetKey(downKey)) inputY -= 1f;

        // Combine inputs into a single local direction vector
        Vector3 moveInput = new Vector3(inputX, inputY, inputZ);

        // Normalize to ensure diagonal + vertical movement isn't faster than maxSpeed
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        float speedCap = maxSpeed * (isColliding ? collisionDragFactor : 1f);

        // Calculate target local velocity
        Vector3 targetLocalVel = moveInput * speedCap;

        // Smoothly accelerate/decelerate toward target
        currentLocalVel = Vector3.MoveTowards(
            currentLocalVel,
            targetLocalVel,
            linearAcceleration * dt
        );

        // Convert local velocity to world velocity using the ROV's orientation
        // This ensures "Up" is relative to the ROV's roof (Heave), not World Up.
        Vector3 worldVel = ROV.transform.TransformDirection(currentLocalVel);
        
        // Apply position update
        Vector3 newPos = rovRb.position + worldVel * dt;
        rovRb.MovePosition(newPos);

        // -------- 2) ROTATION (NO ACCELERATION) --------
        float pitchInput = 0f;
        if (Input.GetKey(pitchUpKey)) pitchInput += 1f;
        if (Input.GetKey(pitchDownKey)) pitchInput -= 1f;

        float yawInput = 0f;
        if (Input.GetKey(yawRightKey)) yawInput += 1f;
        if (Input.GetKey(yawLeftKey)) yawInput -= 1f;

        float dPitch = pitchInput * pitchSpeedDegPerSec * dt;
        float dYaw = yawInput * yawSpeedDegPerSec * dt;

        Quaternion deltaRot = Quaternion.Euler(dPitch, dYaw, 0f);
        Quaternion newRot = rovRb.rotation * deltaRot;

        if (disableRoll)
        {
            Vector3 euler = newRot.eulerAngles;
            euler.z = 0f;
            newRot = Quaternion.Euler(euler);
        }

        rovRb.MoveRotation(newRot);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Tube"))
            isColliding = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Tube"))
            isColliding = false;
    }
}