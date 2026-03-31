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

    private List<Footprint> footprints;
    private List<Footprint> clusters;
    private List<Footprint> subclusters;

    // [Tooltip("Geographic bounds: [West, East, South, North]")]
    public Vector4 geoBounds;
    public Vector4 textureGeoBounds;

    public void LoadData(AppConfig config)
    {
        geoBounds = new Vector4(
            config.GeoBounds[0],
            config.GeoBounds[1],
            config.GeoBounds[2],
            config.GeoBounds[3]
        ); 

        textureGeoBounds = new Vector4(
            config.TextureBounds[0],
            config.TextureBounds[1],
            config.TextureBounds[2],
            config.TextureBounds[3]
        ); 

        //// calculate scene center in real-world coordinate
        float referenceLongitude = (geoBounds.x + geoBounds.y)/2f;
        float referenceLatitude = (geoBounds.z + geoBounds.w)/2f;
        float referenceElevation = 50f;  // placeholder

        Params.referenceCenter.x = referenceLongitude;
        Params.referenceCenter.y = referenceElevation;
        Params.referenceCenter.z = referenceLatitude;

        Debug.Log("Reference: " + Params.referenceCenter);
        //// load data
        this.footprints = BinaryParser.Load(config.footprints_bin);
        this.clusters = BinaryParser.Load(config.clusters_bin);
        this.subclusters = BinaryParser.Load(config.subclusters_bin);
    }



    public List<Footprint> GetFootprints() {return this.footprints;}
    public List<Footprint> GetClusters() {return this.clusters;}
    public List<Footprint> GetSubclusters() {return this.subclusters;}


}