// =============================================================
// ROVWallDetector.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §3.2 + Fig. 2b (proximity detection)
//
// Casts proximity rays from the ROV in 8 directions and computes the minimum distance to the tunnel boundary per sector. Feeds the haptic mapping (HapticSwim1) and the GUI overlay (CaveRayEmitter).
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
using UnityEngine;

public class ROVWallDetector : MonoBehaviour
{
    [Header("ROV Settings")]
    public Transform rovCenter;              // The center of the ROV

    [Tooltip("Auto find active 'Tube' tag")]
    public Collider tubeCollider;            // Tube wall collider

    [Header("Raycast Settings")]
    public int rayCount = 36;                // Number of rays around the ROV
    public float rayDistance = 3f;           // Max ray length

    [Header("Visualization")]
    public bool showRayVisualization = true; // Toggle to show/hide rays
    public Material lineMaterial;            // HDRP Unlit material
    public float lineWidth = 0.01f;

    private LineRenderer[] rayLines;         // LineRenderers for each ray

    [Header("Wall Direction Output")]
    public float angleDegrees;               // Angle in local X-Y plane
    public Vector3 localDirectionToWall;     // Closest direction in local space
    public Vector3 worldClosestPoint;        // Closest world-space hit point

    [Header("Collision Detection")]
    public float collisionDistanceThreshold = 0.2f;
    [HideInInspector] public int wallCollisionCount = 0;

    [HideInInspector] public bool isCollidingWithWall = false;

    [Header("Collision Detection")]
    [Tooltip("Flag is set to 1 for one frame when collision occurs")]
    [HideInInspector] public bool collisionTriggeredThisFrame = false;

    public int closestDirectionIndex = -1;   // for haptic control

    private float[] rayDistances;            // for haptic scaling


    void Start()
    {
        rayLines = new LineRenderer[rayCount];
        rayDistances = new float[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            GameObject lineObj = new GameObject("RayLine_" + i);
            lineObj.transform.parent = this.transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("HDRP/Unlit"));
            lr.widthMultiplier = lineWidth;
            lr.positionCount = 2;
            rayLines[i] = lr;
        }
    }

    void Update()
    {
        float minDistance = Mathf.Infinity;
        Vector3 bestWorldDir = Vector3.zero;
        int bestRayIndex = -1;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (360f / rayCount) * i;
            Vector3 localDir = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.right;
            Vector3 worldDir = rovCenter.TransformDirection(localDir);

            Ray ray = new Ray(rovCenter.position, worldDir);
            Vector3 start = ray.origin;
            Vector3 end = start + worldDir * rayDistance;

            bool hitWall = false;

            if (tubeCollider.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                end = hit.point;
                hitWall = true;

                float dist = hit.distance;
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestWorldDir = worldDir;
                    worldClosestPoint = hit.point;
                    bestRayIndex = i;
                    closestDirectionIndex = bestRayIndex;

                }
            }

            rayDistances[i] = hitWall ? hit.distance : -1f;


            // Draw the ray if visualization is enabled
            if (showRayVisualization)
            {
                rayLines[i].enabled = true;
                rayLines[i].SetPosition(0, start);
                rayLines[i].SetPosition(1, end);

                Color color = (i == bestRayIndex) ? Color.red : (hitWall ? Color.green : Color.gray);
                rayLines[i].startColor = rayLines[i].endColor = color;
            }
            else
            {
                rayLines[i].enabled = false;
            }
        }

        if (bestWorldDir == Vector3.zero)
        {
            Debug.LogWarning("No wall hit by any ray!");
            closestDirectionIndex = -1; // No direction
            return;
        }

        Vector3 localBestDir = rovCenter.InverseTransformDirection(bestWorldDir.normalized);
        localDirectionToWall = localBestDir;

        Vector2 flatDir = new Vector2(localBestDir.x, localBestDir.y);
        angleDegrees = Mathf.Atan2(flatDir.y, flatDir.x) * Mathf.Rad2Deg;
        if (angleDegrees < 0f) angleDegrees += 360f;

        closestDirectionIndex = bestRayIndex; // ray cast for haptic
                                              //Debug.Log("RAYINDEX  " + bestRayIndex);
                                              // Count wall collisions (based on threshold)
        
        Debug.Log("Is colliding: " + isCollidingWithWall );

    }

    public float GetRayHitDistance(int index)
    {
        if (index >= 0 && index < rayDistances.Length)
        {
            return rayDistances[index];
        }
        return -1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Tube"))
        {
            isCollidingWithWall = true;
            Debug.Log("ROV started colliding with wall.");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("Tube"))
        {
            isCollidingWithWall = false;
            Debug.Log("ROV exited collision with wall.");
        }
    }

}
