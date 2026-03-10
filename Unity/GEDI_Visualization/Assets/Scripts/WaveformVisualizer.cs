using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.Linq;
using System;

using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using System.Collections;

using  GEDIGlobals;
using Unity.VisualScripting; // loading global params and structs



public class WaveformVisualizer : MonoBehaviour
{
    [Header("Data and Material")]
    public BinaryParser dataParser;
    public BinaryParser clusterParser;
    public BinaryParser subclusterParser;
    public Material waveformMaterial; // Material for waveform cylinders
    public Material terrainMaterial;  // Material for ground terrain
    public Material wireframeMaterial;

    [Header("Controller")]
    public Button ToggleDataScale;
    private int gediVizState = 0; // 0 = footprints, 1 = subclusters, 2 = clusters
    private const int VIZ_STATE_FOOTPRINT = 0;
    private const int VIZ_STATE_SUBCLUSTER = 1;
    private const int VIZ_STATE_CLUSTER = 2;
    private const int VIZ_NUM_STATES = 3; // Total number of states
    private Dictionary<Vector2Int, Vector3> gridPositions = new Dictionary<Vector2Int, Vector3>();

    [Header("Filtering Options")]
    public float waveformHeightThreshold = 5f; // Minimum height in meters for waveforms
    public float waveformEnergyThreshold = 5f; // Only render parts of the waveform above this energy

    private Dictionary<Vector2Int, TerrainPoint> terrainPoints = new Dictionary<Vector2Int, TerrainPoint>();
    [Header("Terrain Visualization")]
    public Texture2D terrainTexture;

    //// handles the wireframe mode of GEDI terrain
    public Button WireframeToggleGEDITerrain;
    private GameObject terrainGEDI;

    private int gediTerrainDisplayState = 0; // 0 = Solid, 1 = Wireframe, 2 = Off
    private const int GEDI_STATE_SOLID = 0;
    private const int GEDI_STATE_WIREFRAME = 1;
    private const int GEDI_STATE_OFF = 2;
    private const int GEDI_NUM_STATES = 3; // Total number of states


    // private bool enableWireframeGEDI = false; // Whether to render terrain as wireframe
    private Mesh terrainWireframeGEDI;
    private Mesh terrainSolidGEDI;

    //// handles the wireframe mode of Tandem X terrain
    ///
    // public Button WireframeToggleTandemX;   // use when Tandem-X terrain is implemented
    // TODO
    

    [Tooltip("Geographic bounds: [West, East, South, North]")]
    public Vector4 geoBounds = new Vector4(-71.5f, -71.4f, -46.5f, -46.6f); // left, right, bottom, top
    public Vector4 textureGeoBounds = new Vector4(-71.5f, -71.4f, -46.5f, -46.6f); // left, right, bottom, top
                        // (lng>-71.5) & (lng<-71.4) & (lat>46.5) & (lat<46.6)
    private float referenceLatitude;
    private float referenceLongitude;
    private float referenceElevation;

    

    void Start()
    {
        referenceLongitude = (geoBounds.x + geoBounds.y)/2f;
        referenceLatitude = (geoBounds.z + geoBounds.w)/2f;
        referenceElevation = 50f;  // placeholder

        if (dataParser != null && clusterParser != null && subclusterParser != null)
        {
            if (dataParser.GetDataPoints() == null || dataParser.GetDataPoints().Count == 0) dataParser.Load();
            if (subclusterParser.GetDataPoints() == null || subclusterParser.GetDataPoints().Count == 0) subclusterParser.Load();
            if (clusterParser.GetDataPoints() == null || clusterParser.GetDataPoints().Count == 0) clusterParser.Load();

            List<Footprint> dataPoints = dataParser.GetDataPoints();
            List<Footprint> subclusters = subclusterParser.GetDataPoints();
            List<Footprint> clusters = clusterParser.GetDataPoints();
            if (dataPoints != null && dataPoints.Count > 0) VisualizeData(dataPoints, subclusters, clusters);

            ToggleDataScale.onClick.AddListener(ToggleVizScale);
        }
        else
        {
            Debug.LogError("Data Parser is not assigned.");
        }
    }

    private Vector3 LatLong2Unity(float latitude, float longitude, float elevation)
    {
        float latDiff = latitude - referenceLatitude;
        float lonDiff = longitude - referenceLongitude;
        float elevDiff = elevation - referenceElevation;

        float latInMeters = latDiff * 111000f;
        float cosLat = Mathf.Cos(referenceLatitude * Mathf.Deg2Rad);
        float lonInMeters = lonDiff * 111000f * cosLat;

        float x = lonInMeters * Params.SCALE;
        float y = elevDiff * Params.TerrainScale;
        float z = latInMeters * Params.SCALE;

        // EXTREME ELEVATION
        return new Vector3(x, y, z);  // All in meters and w/ regard to reference centers
        // return new Vector3(x, elevation*0.75f, z);
    }

    public int GetVizScale()
    {
        return this.gediVizState;
    }

    private void CreateCylinder(Vector3 position, Footprint dataPoint, string tagname, float local_scale=1f)
    {
        GameObject waveformObject = new GameObject("WaveformCylinder");
        waveformObject.transform.position = position;
        waveformObject.tag = tagname;

        MeshFilter meshFilter = waveformObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = waveformObject.AddComponent<MeshRenderer>();
        meshRenderer.material = waveformMaterial;

        // calc direction from ground to ISS
        Vector3 slantDirection = WaveformTools.CalculateISSDirection(dataPoint);
        
        // cylinder mesh with slant
        Mesh mesh = WaveformTools.GenerateCylinderMesh(dataPoint.rawWaveformValues, dataPoint.rawWaveformPositions, slantDirection);
        meshFilter.mesh = mesh;
        waveformObject.GetComponent<Renderer>().enabled = false;

        // float bottomOffset = 0.1172f * 76.8f * Params.SCALE; // CHECK
        waveformObject.transform.localScale = new Vector3(local_scale, local_scale, local_scale);
    }



    public void VisualizeData(List<Footprint> footprints, List<Footprint> subclusters, List<Footprint> clusters)
    {

        terrainPoints.Clear();
        foreach (var point in footprints)
        {
            Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point, "footprint");

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

        CreateTerrainMeshDELNET(terrainPoints);

        foreach (var point in subclusters)
        {
            Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point, "subcluster", 10);
        }

        foreach (var point in clusters)
        {
            Vector3 position = LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point, "cluster", 30);
        }
    }

    

    private void CreateTerrainMeshDELNET(Dictionary<Vector2Int, TerrainPoint> points)
    {
        if (points.Count < 3)
        {
            Debug.LogWarning("Not enough points for triangulation (minimum 3 required)!");
            return;
        }

        
        Polygon polygon = new Polygon();  // create a polygon for triangulation
        Dictionary<int, TerrainPoint> pointMap = new Dictionary<int, TerrainPoint>(); // dict k: vertex ID, v: terrain points
        
        // add vertices to the polygon (still using Unity positions for geometry)
        var pointList = points.Values.ToList();
        int id = 0;
        foreach (var point in pointList)
        {
            polygon.Add(new Vertex(point.position.x, point.position.z, id));
            pointMap[id] = point;
            id++;
        }
        
        // Debug.Log($"Geographic bounds - Lat: {minLat} to {maxLat}, Lon: {minLon} to {maxLon}");

        // save both solid and wireframe as member variables
        terrainSolidGEDI = GEDITerrainCreator.generateSolid(polygon, pointMap, textureGeoBounds);
        terrainWireframeGEDI = GEDITerrainCreator.generateWireframe(polygon, pointMap);
        
        // if a texture is assigned
        if (terrainTexture != null) {
            terrainMaterial.mainTexture = terrainTexture;
            // Debug.Log($"Applied texture to terrain with geo bounds: {textureGeoBounds}");
        }


        // use solid as default
        terrainGEDI = new GameObject("DelaunayTerrainSolid");
        MeshFilter meshFilter = terrainGEDI.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainGEDI.AddComponent<MeshRenderer>();
        meshFilter.mesh = terrainSolidGEDI;
        meshRenderer.material = terrainMaterial;

        
        WireframeToggleGEDITerrain.onClick.AddListener(ToggleWireframeGEDI);
    }

    void ToggleVizScale()
    {
        // Cycle through states: 0 -> 1 -> 2 -> 0
        gediVizState = (gediVizState + 1) % VIZ_NUM_STATES;
        Text buttonText = ToggleDataScale.GetComponentInChildren<Text>();
        
        
        switch (gediVizState)
        {
            case VIZ_STATE_FOOTPRINT: // State 0: Footprints
                buttonText.text = "Footprints";
                GameObject[] subclusters = GameObject.FindGameObjectsWithTag("subcluster");
                foreach (GameObject obj in subclusters) obj.GetComponent<Renderer>().enabled = false;
                break;

            case VIZ_STATE_SUBCLUSTER: // State 1: Sub-Clusters
                buttonText.text = "Clusters";
                GameObject[] footprints = GameObject.FindGameObjectsWithTag("footprint");
                foreach (GameObject obj in footprints) obj.GetComponent<Renderer>().enabled = false;
                break;

            case VIZ_STATE_CLUSTER: // State 2: Clusters
                buttonText.text = "Sub-Clusters";
                GameObject[] clusters = GameObject.FindGameObjectsWithTag("cluster");
                foreach (GameObject obj in clusters) obj.GetComponent<Renderer>().enabled = false;
                break;
        }

    }

    void ToggleWireframeGEDI()
    {
        // Cycle through states: 0 -> 1 -> 2 -> 0
        gediTerrainDisplayState = (gediTerrainDisplayState + 1) % GEDI_NUM_STATES;

        if (terrainGEDI == null) return; 

        MeshFilter meshFilter = terrainGEDI.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainGEDI.GetComponent<MeshRenderer>();
        Text buttonText = WireframeToggleGEDITerrain.GetComponentInChildren<Text>();

        if (meshFilter == null || meshRenderer == null || buttonText == null)
        {
            Debug.LogError("Missing components on terrainGEDI or Button Text.");
            return;
        }

        switch (gediTerrainDisplayState)
        {
            case GEDI_STATE_SOLID: // State 0: Solid
                buttonText.text = "GEDI Terrain (Solid)";
                meshFilter.mesh = terrainSolidGEDI;
                meshRenderer.material = terrainMaterial;
                terrainGEDI.SetActive(true); // Gameobject active
                break;

            case GEDI_STATE_WIREFRAME: // State 1: Wireframe
                buttonText.text = "GEDI Terrain (Wireframe)";
                meshFilter.mesh = terrainWireframeGEDI;
                meshRenderer.material = wireframeMaterial;
                terrainGEDI.SetActive(true); // Gameobject active
                break;

            case GEDI_STATE_OFF: // State 2: Off
                buttonText.text = "GEDI Terrain (Off)";
                terrainGEDI.SetActive(false); // Hide GameObject
                break;
        }
    }
    


}