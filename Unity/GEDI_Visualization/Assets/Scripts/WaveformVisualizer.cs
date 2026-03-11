using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.IO;


using GEDIGlobals;


public class WaveformVisualizer : MonoBehaviour
{
    [Header("Data and Material")]
    public DataManager dataManager;
    public TerrainManager terrainManager;
    public Material waveformMaterial; // Material for waveform cylinders

    [Header("Controller")]
    public Button toggleDataScale;
    private int gediVizState = 0; // 0 = footprints, 1 = subclusters, 2 = clusters
    private const int VIZ_NUM_STATES = 3; // Total number of states
    
    [Header("Filtering Options")]
    public float waveformHeightThreshold = 5f; // Minimum height in meters for waveforms
    public float waveformEnergyThreshold = 5f; // Only render parts of the waveform above this energy


    void Start()
    {
        AppConfig config = LoadConfig();

        dataManager.LoadData(config.footprints_bin,
                             config.subclusters_bin,
                             config.clusters_bin);

        VisualizeData(this.dataManager.GetFootprints(), 
                      this.dataManager.GetSubclusters(), 
                      this.dataManager.GetClusters());

        terrainManager.LoadTexture(config.terrain_texture);
        terrainManager.LoadTandemX(config.dem_file);
        terrainManager.CreateTerrainTandemX();
        terrainManager.CreateTerrainMeshDELNET(this.dataManager.GetFootprints());


        Camera mainCamera = Camera.main;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
    }

    private AppConfig LoadConfig()
    {
        string configPath = Path.Combine(Application.dataPath, "..", "config.json");
        configPath = Path.GetFullPath(configPath);

        if (!File.Exists(configPath))
        {
            Debug.LogError("Config file not found: " + configPath);
            return null;
        }

        string json = File.ReadAllText(configPath);
        return JsonUtility.FromJson<AppConfig>(json);
    }

    public void VisualizeData(List<Footprint> footprints, List<Footprint> subclusters, List<Footprint> clusters)
    {

        foreach (var point in footprints)
        {
            Vector3 position = dataManager.LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point, "footprint");
        }

        foreach (var point in subclusters)
        {
            Vector3 position = dataManager.LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point, "subcluster", 10);
        }

        foreach (var point in clusters)
        {
            Vector3 position = dataManager.LatLong2Unity(point.latitude, point.longitude, point.elevation);
            CreateCylinder(position, point, "cluster", 30);
        }

        toggleDataScale.onClick.AddListener(ChangeDataScale);
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


    public int GetVizScale()
    {
        return this.gediVizState;
    }

    void ChangeDataScale()
    {
        // Cycle through states: 0 -> 1 -> 2 -> 0
        gediVizState = (gediVizState + 1) % VIZ_NUM_STATES;
        Text buttonText = toggleDataScale.GetComponentInChildren<Text>();
        
        switch (gediVizState)
        {
            case 0: // State 0: Footprints
                buttonText.text = "Footprints";
                GameObject[] subclusters = GameObject.FindGameObjectsWithTag("subcluster");
                foreach (GameObject obj in subclusters) obj.GetComponent<Renderer>().enabled = false;
                break;

            case 1: // State 1: Sub-Clusters
                buttonText.text = "Clusters";
                GameObject[] footprints = GameObject.FindGameObjectsWithTag("footprint");
                foreach (GameObject obj in footprints) obj.GetComponent<Renderer>().enabled = false;
                break;

            case 2: // State 2: Clusters
                buttonText.text = "Sub-Clusters";
                GameObject[] clusters = GameObject.FindGameObjectsWithTag("cluster");
                foreach (GameObject obj in clusters) obj.GetComponent<Renderer>().enabled = false;
                break;
        }

    }

    



}