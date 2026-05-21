// =============================================================
// CaveRayEmitter.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: Fig. 2b (detection-ray visualisation)
//
// Renders the 8 proximity-detection rays emanating from the ROV; ray
// length scales with the minimum distance per sector. Used for the GUI
// overlay shown to the operator (Figure 2b panel d).
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================

using UnityEngine;
using System.Collections.Generic;

public class CaveRayEmitter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Number of rays to emit.")]
    public int rayCount = 40;
    
    [Tooltip("How far the rays travel if they don't hit anything.")]
    public float maxDistance = 10f;

    [Tooltip("The width of the laser/ray lines.")]
    public float rayWidth = 0.05f;

    [Tooltip("Color of the rays.")]
    public Color rayColor = Color.red;

    [Tooltip("The specific tag that blocks the rays.")]
    public string blockingTag = "cave";

    [Tooltip("Check this if your project uses 2D Colliders (BoxCollider2D). Uncheck for 3D Colliders.")]
    public bool usePhysics2D = false;

    [Header("References")]
    [Tooltip("Assign a simple material here (e.g., Default-Line or Sprites-Default)")]
    public Material lineMaterial;

    // Internal storage for our line objects
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private GameObject rayContainer;

    void Start()
    {
        InitializeRays();
    }

    void Update()
    {
        CastRays();
    }

    void InitializeRays()
    {
        // Create a clean container for the rays so they don't clutter the hierarchy
        rayContainer = new GameObject("RayContainer");
        rayContainer.transform.SetParent(this.transform);
        rayContainer.transform.localPosition = Vector3.zero;

        // Initialize the pool of LineRenderers
        for (int i = 0; i < rayCount; i++)
        {
            GameObject rayObj = new GameObject($"Ray_{i}");
            rayObj.transform.SetParent(rayContainer.transform);
            rayObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = rayObj.AddComponent<LineRenderer>();
            
            // Visual settings
            lr.startWidth = rayWidth;
            lr.endWidth = rayWidth;
            lr.useWorldSpace = true; // Easier to work with absolute positions
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startColor = rayColor;
            lr.endColor = rayColor;

            lineRenderers.Add(lr);
        }
    }

    void CastRays()
    {
        float angleStep = 360f / rayCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < rayCount; i++)
        {
            // 1. Calculate Direction in X-Y Plane
            // We use trigonometry to find the x and y components based on the current angle
            float angle = i * angleStep;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = Mathf.Sin(angle * Mathf.Deg2Rad);
            
            // Direction vector (Z is 0 because we are in X-Y plane)
            Vector3 direction = new Vector3(x, y, 0).normalized;

            Vector3 targetPoint;

            // 2. Perform Physics Raycast
            if (usePhysics2D)
            {
                // -- 2D PHYSICS LOGIC --
                // Cast a ray in 2D space
                RaycastHit2D hit2D = Physics2D.Raycast(origin, direction, maxDistance);

                // Check if we hit something and if that something is tagged "cave"
                if (hit2D.collider != null && hit2D.collider.CompareTag(blockingTag))
                {
                    targetPoint = hit2D.point;
                }
                else if (hit2D.collider != null)
                {
                    // Optional: If you want NON-cave objects to also block the ray, 
                    // keep this line. If you want rays to pass through non-caves, 
                    // you would need to use LayerMasks or RaycastAll.
                    // Currently, this blocks on ANY collider.
                    targetPoint = hit2D.point;
                }
                else
                {
                    // Hit nothing
                    targetPoint = origin + (direction * maxDistance);
                }
            }
            else
            {
                // -- 3D PHYSICS LOGIC --
                Ray ray = new Ray(origin, direction);
                RaycastHit hit3D;

                if (Physics.Raycast(ray, out hit3D, maxDistance))
                {
                    if (hit3D.collider.CompareTag(blockingTag))
                    {
                        targetPoint = hit3D.point;
                    }
                    else
                    {
                        // Hit a non-cave object (still blocks by default physics rules)
                        targetPoint = hit3D.point;
                    }
                }
                else
                {
                    // Hit nothing
                    targetPoint = origin + (direction * maxDistance);
                }
            }

            // 3. Update the Visual Line
            lineRenderers[i].SetPosition(0, origin); // Start at center
            lineRenderers[i].SetPosition(1, targetPoint); // End at wall or max distance
        }
    }

    // Update visual properties in real-time if changed in Inspector
    void OnValidate()
    {
        if (lineRenderers.Count > 0)
        {
            foreach (var lr in lineRenderers)
            {
                lr.startWidth = rayWidth;
                lr.endWidth = rayWidth;
                lr.startColor = rayColor;
                lr.endColor = rayColor;
            }
        }
    }
}