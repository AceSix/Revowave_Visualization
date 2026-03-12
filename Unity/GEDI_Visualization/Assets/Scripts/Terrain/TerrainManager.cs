using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq; // <--- Add this
using UnityEngine;
using UnityEngine.UI; // Required for UI elements

using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;

using GEDIGlobals;
public class TerrainManager : MonoBehaviour
{
    [Header("Data and Texture")]
    public DataManager dataManager;
    public Material terrainMaterial;  // Material for ground terrain
    public Material wireframeMaterial;

    [Header("GEDI Terrain")]
    //// handles the wireframe mode of GEDI terrain
    public Button ToggleGEDITerrain;

    [Header("Tandem-X Terrain")]
    public int resolution = 256;
    public Button ToggleDemTerrain;

    private int gediTerrainDisplayState = 2; // 0 = Solid, 1 = Wireframe, 2 = Off
    private int tandemxTerrainDisplayState = 0; // 0 = Solid, 1 = Wireframe, 2 = Off
    private const int TERRAIN_NUM_STATES = 3; // Total number of states


    private Texture2D terrainTexture;
    private Texture2D demSourceTandemX;
    private GameObject terrainGEDI;
    private GameObject terrainTandemX;
    private Mesh terrainWireframeTandemX;
    private Mesh terrainSolidTandemX;
    private Mesh terrainWireframeGEDI;
    private Mesh terrainSolidGEDI;

    public void LoadTexture(string texturePath)
    {
        byte[] bytes = File.ReadAllBytes(texturePath);
        terrainTexture = new Texture2D(2,2);
        terrainTexture.LoadImage(bytes);
    }

    public void LoadTandemX(string demPath)
    {
        using BinaryReader br = new BinaryReader(File.OpenRead(demPath));

        int height = br.ReadInt32();
        int width = br.ReadInt32();

        float[] heights = new float[width * height];

        for (int i = 0; i < heights.Length; i++)
            heights[i] = br.ReadSingle();

        demSourceTandemX = new Texture2D(width, height, TextureFormat.RFloat, false, true);

        demSourceTandemX.SetPixelData(heights, 0);
        demSourceTandemX.wrapMode = TextureWrapMode.Clamp;
        demSourceTandemX.filterMode = FilterMode.Bilinear;
        demSourceTandemX.Apply();
    }

    public void CreateTerrainTandemX()
    {
        terrainTandemX = new GameObject("TerrainTandemX");
        MeshFilter meshFilter = terrainTandemX.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainTandemX.AddComponent<MeshRenderer>();

        Vector3 referenceCenter = dataManager.GetReferenceCenter();

        float height = (dataManager.geoBounds.w - dataManager.geoBounds.z) * 111000f * Params.SCALE;
        float cosLat = Mathf.Cos(referenceCenter.z * Mathf.Deg2Rad);
        float width = (dataManager.geoBounds.y - dataManager.geoBounds.x) * 111000f * cosLat * Params.SCALE;

        terrainMaterial.mainTexture = terrainTexture;
        
        terrainSolidTandemX = DEMTerrainCreator.GenerateSolid(demSourceTandemX, resolution, dataManager.geoBounds, dataManager.textureGeoBounds);
        terrainWireframeTandemX = DEMTerrainCreator.GenerateWireframe(demSourceTandemX, resolution, dataManager.geoBounds, dataManager.textureGeoBounds);

        meshFilter.mesh = terrainSolidTandemX;
        meshRenderer.material = terrainMaterial;
        terrainTandemX.transform.localScale = new Vector3(width, 1, height);

        // Align with same degree→meter conversion for both axes
        float translateX = (dataManager.geoBounds.x - referenceCenter.x) * 111000f * cosLat * Params.SCALE; // lon shift
        float translateY = -referenceCenter.y * Params.TerrainScale;
        float translateZ = (dataManager.geoBounds.z - referenceCenter.z) * 111000f * Params.SCALE;            // lat shift
        terrainTandemX.transform.Translate(translateX, translateY, translateZ, Space.World);

        tandemxTerrainDisplayState = 2;
        ChangeTerrainStateTandemX();
        ToggleDemTerrain.onClick.AddListener(ChangeTerrainStateTandemX);
    }

    void ChangeTerrainStateTandemX()
    {
        tandemxTerrainDisplayState = (tandemxTerrainDisplayState + 1) % TERRAIN_NUM_STATES;

        if (terrainTandemX == null) return; 
        MeshFilter meshFilter = terrainTandemX.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainTandemX.GetComponent<MeshRenderer>();
        Text buttonText = ToggleDemTerrain.GetComponentInChildren<Text>();

        if (meshFilter == null || meshRenderer == null || buttonText == null)
        {
            Debug.LogError("Missing components on terrainTandemX or Button Text.");
            return;
        }
        
        switch (tandemxTerrainDisplayState)
        {
            case 0: // State 0: Solid
                buttonText.text = "Tandem-X (Solid)";
                meshFilter.mesh = terrainSolidTandemX;
                meshRenderer.material = terrainMaterial;
                terrainTandemX.SetActive(true); // Gameobject active
                break;

            case 1: // State 1: Wireframe
                buttonText.text = "Tandem-X (Wireframe)";
                meshFilter.mesh = terrainWireframeTandemX;
                meshRenderer.material = wireframeMaterial;
                terrainTandemX.SetActive(true); // Gameobject active
                break;

            case 2: // State 2: Off
                buttonText.text = "Tandem-X (Off)";
                terrainTandemX.SetActive(false); // Hide GameObject
                break;
        }
    }

    public void CreateTerrainMeshDELNET(List<Footprint> footprints)
    {
        Dictionary<Vector2Int, TerrainPoint> terrainPoints = new Dictionary<Vector2Int, TerrainPoint>();
        terrainPoints.Clear();

        foreach (var point in footprints)
        {
            Vector3 position = dataManager.LatLong2Unity(point.latitude, point.longitude, point.elevation);

            Vector3 bottomPosition = new Vector3(
                position.x,
                position.y,
                position.z
            );

            Vector2Int gridKey = new Vector2Int(
                Mathf.RoundToInt(position.x * 1000),
                Mathf.RoundToInt(position.z * 1000)
            );

            terrainPoints[gridKey] = new TerrainPoint(
                bottomPosition,         // unity pos
                point.latitude,
                point.longitude,
                point.elevation
            );
        }

        if (terrainPoints.Count < 3)
        {
            Debug.LogWarning("Not enough points for triangulation (minimum 3 required)!");
            return;
        }
        
        Polygon polygon = new Polygon();  // create a polygon for triangulation
        Dictionary<int, TerrainPoint> pointMap = new Dictionary<int, TerrainPoint>(); // dict k: vertex ID, v: terrain points
        
        // add vertices to the polygon (still using Unity positions for geometry)
        var pointList = terrainPoints.Values.ToList();
        int id = 0;
        foreach (var point in pointList)
        {
            polygon.Add(new Vertex(point.position.x, point.position.z, id));
            pointMap[id] = point;
            id++;
        }
        
        // Debug.Log($"Geographic bounds - Lat: {minLat} to {maxLat}, Lon: {minLon} to {maxLon}");

        // save both solid and wireframe as member variables
        terrainSolidGEDI = GEDITerrainCreator.generateSolid(polygon, pointMap, dataManager.textureGeoBounds);
        terrainWireframeGEDI = GEDITerrainCreator.generateWireframe(polygon, pointMap);
        
        // if a texture is assigned
        if (terrainTexture != null) {
            terrainMaterial.mainTexture = terrainTexture;
            // Debug.Log($"Applied texture to terrain with geo bounds: {textureGeoBounds}");
        }


        // use solid as default
        terrainGEDI = new GameObject("TerrainGEDI");
        MeshFilter meshFilter = terrainGEDI.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainGEDI.AddComponent<MeshRenderer>();
        meshFilter.mesh = terrainSolidGEDI;
        meshRenderer.material = terrainMaterial;

        gediTerrainDisplayState = 1;
        ChangeTerrainStateGEDI();
        ToggleGEDITerrain.onClick.AddListener(ChangeTerrainStateGEDI);
    }

    void ChangeTerrainStateGEDI()
    {
        // Cycle through states: 0 -> 1 -> 2 -> 0
        gediTerrainDisplayState = (gediTerrainDisplayState + 1) % TERRAIN_NUM_STATES;

        if (terrainGEDI == null) return; 

        MeshFilter meshFilter = terrainGEDI.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainGEDI.GetComponent<MeshRenderer>();
        Text buttonText = ToggleGEDITerrain.GetComponentInChildren<Text>();

        if (meshFilter == null || meshRenderer == null || buttonText == null)
        {
            Debug.LogError("Missing components on terrainGEDI or Button Text.");
            return;
        }

        switch (gediTerrainDisplayState)
        {
            case 0: // State 0: Solid
                buttonText.text = "GEDI Terrain (Solid)";
                meshFilter.mesh = terrainSolidGEDI;
                meshRenderer.material = terrainMaterial;
                terrainGEDI.SetActive(true); // Gameobject active
                break;

            case 1: // State 1: Wireframe
                buttonText.text = "GEDI Terrain (Wireframe)";
                meshFilter.mesh = terrainWireframeGEDI;
                meshRenderer.material = wireframeMaterial;
                terrainGEDI.SetActive(true); // Gameobject active
                break;

            case 2: // State 2: Off
                buttonText.text = "GEDI Terrain (Off)";
                terrainGEDI.SetActive(false); // Hide GameObject
                break;
        }
    }
    
}