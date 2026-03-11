using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.Linq;
using System;

using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using System.Collections;

using GEDIGlobals;

public class DataManager : MonoBehaviour
{
    [Header("Data and Material")]
    
    public string footprintPath = "Ardeche.bin";
    public string clusterPath = "Ardeche_cluster.bin";
    public string subclusterPath = "Ardeche_subcluster.bin";

    private List<Footprint> footprints;
    private List<Footprint> clusters;
    private List<Footprint> subclusters;


    [Tooltip("Geographic bounds: [West, East, South, North]")]
    public Vector4 geoBounds = new Vector4(-71.5f, -71.4f, -46.5f, -46.6f); // left, right, bottom, top
    public Vector4 textureGeoBounds = new Vector4(-71.5f, -71.4f, -46.5f, -46.6f); // left, right, bottom, top
                        // (lng>-71.5) & (lng<-71.4) & (lat>46.5) & (lat<46.6)

    private Vector3 referenceCenter;

    public void LoadData(string footprintPath, string subclusterPath, string clusterPath)
    {
        //// calculate scene center in real-world coordinate
        float referenceLongitude = (geoBounds.x + geoBounds.y)/2f;
        float referenceLatitude = (geoBounds.z + geoBounds.w)/2f;
        float referenceElevation = 50f;  // placeholder
        this.referenceCenter = new Vector3(referenceLongitude, referenceElevation, referenceLatitude);
        //// load data
        this.footprints = BinaryParser.Load(footprintPath);
        this.clusters = BinaryParser.Load(clusterPath);
        this.subclusters = BinaryParser.Load(subclusterPath);
    }

    public Vector3 LatLong2Unity(float latitude, float longitude, float elevation)
    {
        float latDiff = latitude - referenceCenter.z;
        float lonDiff = longitude - referenceCenter.x;
        float elevDiff = elevation - referenceCenter.y;

        float latInMeters = latDiff * 111000f;
        float cosLat = Mathf.Cos(referenceCenter.z * Mathf.Deg2Rad);
        float lonInMeters = lonDiff * 111000f * cosLat;

        float x = lonInMeters * Params.SCALE;
        float y = elevDiff * Params.TerrainScale;
        float z = latInMeters * Params.SCALE;

        // EXTREME ELEVATION
        return new Vector3(x, y, z);  // All in meters and w/ regard to reference centers
        // return new Vector3(x, elevation*0.75f, z);
    }
    public Vector3 GetReferenceCenter() {return this.referenceCenter;}
    public List<Footprint> GetFootprints() {return this.footprints;}
    public List<Footprint> GetClusters() {return this.clusters;}
    public List<Footprint> GetSubclusters() {return this.subclusters;}


}