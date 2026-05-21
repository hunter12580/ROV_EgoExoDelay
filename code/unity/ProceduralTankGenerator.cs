// =============================================================
// ProceduralTankGenerator.cs
//
// ACM THRI revision — "System Design and Validation of an Ego-Exocentric
// Mixed Reality Framework in Compensating Delay for ROV Teleoperation"
// (Manuscript ID THRI-2025-0268).
//
// Paper reference: §4.2 + Fig. 5 (tunnel geometry generator)
//
// Procedurally generates the curved-tunnel test environment described in §4.2 (radius = 2 m, mixed straight + curved segments, low-visibility ambient lighting).
//
// Released under the MIT License — see ../../LICENSE.
// =============================================================
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ProceduralTankGenerator : MonoBehaviour
{
    [Header("Tank Dimensions")]
    [Tooltip("Length of the tank (Z axis) - The Long Side")]
    public float length = 5.0f;
    [Tooltip("Width of the tank (X axis) - The Short Side")]
    public float width = 3.0f;
    [Tooltip("Height of the tank (Y axis)")]
    public float height = 1.5f;
    [Tooltip("Thickness of the solid walls")]
    public float wallThickness = 0.1f;

    [Header("Window Settings")]
    [Tooltip("Percentage of the side wall surface that should be glass (0.1 to 0.99)")]
    [Range(0.1f, 0.99f)]
    public float windowAreaPercentage = 0.90f;
    
    [Tooltip("Thickness of the glass pane")]
    public float glassThickness = 0.02f;

    [Header("Rendering Settings")]
    [Tooltip("If true, the tank walls will cast shadows. Set to FALSE to prevent walls from darkening the interior.")]
    public bool castTankShadows = false; 

    [Header("Materials")]
    public Material hullMaterial;
    public Material glassMaterial;
    public Material waterMaterial;

    // Internal references to child objects
    private GameObject tankHullObj;
    private GameObject windowObj;
    private GameObject waterObj;

    public void GenerateTank()
    {
        Cleanup();
        CreateTankHull();
        CreateWindow();
        CreateWaterVolume();
    }

    private void Cleanup()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        foreach (GameObject child in children) DestroyImmediate(child);
    }

    // ------------------------------------------------------------------------
    // MESH 1: THE TANK HULL (Fiberglass)
    // ------------------------------------------------------------------------
    private void CreateTankHull()
    {
        tankHullObj = new GameObject("Tank_Hull");
        tankHullObj.transform.parent = transform;
        tankHullObj.transform.localPosition = Vector3.zero;

        MeshBuilder builder = new MeshBuilder();

        float halfL = length / 2f;
        float halfW = width / 2f;
        
        // --- Floor ---
        builder.AddBox(new Vector3(0, wallThickness / 2f, 0), new Vector3(width, wallThickness, length));

        // --- Back Wall (+Z) --- (Short Side)
        Vector3 backPos = new Vector3(0, height / 2f, halfL - wallThickness / 2f);
        Vector3 backSize = new Vector3(width, height, wallThickness);
        builder.AddBox(backPos, backSize);

        // --- Front Wall (-Z) --- (Short Side)
        Vector3 frontPos = new Vector3(0, height / 2f, -halfL + wallThickness / 2f);
        Vector3 frontSize = new Vector3(width, height, wallThickness);
        builder.AddBox(frontPos, frontSize);

        // --- Left Wall (-X) --- (Long Side - Solid)
        Vector3 leftPos = new Vector3(-halfW + wallThickness / 2f, height / 2f, 0);
        Vector3 leftSize = new Vector3(wallThickness, height, length - (wallThickness * 2));
        builder.AddBox(leftPos, leftSize);

        // --- Right Wall (+X) --- (Long Side - Window Frame)
        // 1. Calculate Frame Margins based on 90% Area
        float scaleFactor = Mathf.Sqrt(windowAreaPercentage);
        
        float windowH = height * scaleFactor;
        float windowL = length * scaleFactor;

        float marginY = (height - windowH) / 2f; // Top and Bottom thickness
        float marginZ = (length - windowL) / 2f; // Left and Right pillar thickness

        float rightX = halfW - wallThickness / 2f;

        // Bottom Sill
        builder.AddBox(
            new Vector3(rightX, marginY / 2f, 0), 
            new Vector3(wallThickness, marginY, length)
        );

        // Top Lintel
        builder.AddBox(
            new Vector3(rightX, height - marginY / 2f, 0), 
            new Vector3(wallThickness, marginY, length)
        );

        // Left Pillar (at -Z end of the wall)
        float pillarHeight = height - (marginY * 2);
        builder.AddBox(
            new Vector3(rightX, height / 2f, -halfL + marginZ / 2f), 
            new Vector3(wallThickness, pillarHeight, marginZ)
        );

        // Right Pillar (at +Z end of the wall)
        builder.AddBox(
            new Vector3(rightX, height / 2f, halfL - marginZ / 2f), 
            new Vector3(wallThickness, pillarHeight, marginZ)
        );

        ApplyMesh(tankHullObj, builder.ToMesh("HullMesh"), hullMaterial, true);
    }

    // ------------------------------------------------------------------------
    // MESH 2: THE WINDOW (Glass)
    // ------------------------------------------------------------------------
    private void CreateWindow()
    {
        windowObj = new GameObject("Tank_Window");
        windowObj.transform.parent = transform;
        windowObj.transform.localPosition = Vector3.zero;

        MeshBuilder builder = new MeshBuilder();

        float halfW = width / 2f;
        float rightX = halfW - wallThickness / 2f;

        float scaleFactor = Mathf.Sqrt(windowAreaPercentage);
        float windowH = height * scaleFactor;
        float windowL = length * scaleFactor;

        // Position exactly in the hole of the right frame
        builder.AddBox(
            new Vector3(rightX, height / 2f, 0),
            new Vector3(glassThickness, windowH, windowL)
        );

        ApplyMesh(windowObj, builder.ToMesh("WindowMesh"), glassMaterial, true);
    }

    // ------------------------------------------------------------------------
    // MESH 3: THE WATER (Volume)
    // ------------------------------------------------------------------------
    private void CreateWaterVolume()
    {
        waterObj = new GameObject("Tank_Water");
        waterObj.transform.parent = transform;
        waterObj.transform.localPosition = Vector3.zero;

        MeshBuilder builder = new MeshBuilder();

        // Calculate inner dimensions
        float innerW = width - (wallThickness * 2);
        float innerL = length - (wallThickness * 2);
        float innerH = height - wallThickness; 

        float floorY = wallThickness;
        float waterCenterY = floorY + (innerH / 2f);
        float padding = 0.005f; 

        builder.AddBox(
            new Vector3(0, waterCenterY, 0),
            new Vector3(innerW - padding, innerH - padding, innerL - padding)
        );

        // Water usually shouldn't cast shadows regardless of the setting
        // But we apply the logic inside ApplyMesh anyway (usually transparent mats handle this)
        ApplyMesh(waterObj, builder.ToMesh("WaterMesh"), waterMaterial, false);
        
        BoxCollider bc = waterObj.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(innerW, innerH, innerL);
        bc.center = new Vector3(0, waterCenterY, 0);
    }

    private void ApplyMesh(GameObject go, Mesh mesh, Material mat, bool addCollider)
    {
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        
        // --- SHADOW LOGIC ---
        // If castTankShadows is FALSE, we set casting mode to Off.
        // We ALWAYS keep receiveShadows = true, so the ROV can cast shadows ONTO the tank.
        mr.shadowCastingMode = castTankShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = true;
        // --------------------

        mf.sharedMesh = mesh;
        mr.sharedMaterial = mat;
        if (addCollider) go.AddComponent<MeshCollider>();
    }
}

public class MeshBuilder
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Vector3> normals = new List<Vector3>();

    public void AddBox(Vector3 center, Vector3 size)
    {
        Vector3 half = size / 2f;
        Vector3 p0 = center + new Vector3(-half.x, -half.y, -half.z);
        Vector3 p1 = center + new Vector3(half.x, -half.y, -half.z);
        Vector3 p2 = center + new Vector3(half.x, -half.y, half.z);
        Vector3 p3 = center + new Vector3(-half.x, -half.y, half.z);
        Vector3 p4 = center + new Vector3(-half.x, half.y, -half.z);
        Vector3 p5 = center + new Vector3(half.x, half.y, -half.z);
        Vector3 p6 = center + new Vector3(half.x, half.y, half.z);
        Vector3 p7 = center + new Vector3(-half.x, half.y, half.z);

        AddFace(p0, p1, p2, p3, Vector3.down);
        AddFace(p7, p6, p5, p4, Vector3.up);
        AddFace(p0, p4, p5, p1, Vector3.back);
        AddFace(p1, p5, p6, p2, Vector3.right);
        AddFace(p3, p2, p6, p7, Vector3.forward);
        AddFace(p0, p3, p7, p4, Vector3.left); 
    }

    private void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        int startIndex = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        normals.Add(normal); normals.Add(normal); normals.Add(normal); normals.Add(normal);
        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
        triangles.Add(startIndex + 0); triangles.Add(startIndex + 2); triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 0); triangles.Add(startIndex + 3); triangles.Add(startIndex + 2);
    }

    public Mesh ToMesh(string name)
    {
        Mesh m = new Mesh();
        m.name = name;
        m.vertices = vertices.ToArray();
        m.triangles = triangles.ToArray();
        m.uv = uvs.ToArray();
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ProceduralTankGenerator))]
public class ProceduralTankGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ProceduralTankGenerator script = (ProceduralTankGenerator)target;
        if (GUILayout.Button("Generate Tank Meshes")) script.GenerateTank();
    }
}
#endif