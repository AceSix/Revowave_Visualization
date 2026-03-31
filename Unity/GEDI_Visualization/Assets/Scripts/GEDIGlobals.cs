using UnityEngine;
using System;
using System.Collections.Generic;

namespace GEDIGlobals
{
    [System.Serializable]
    public class AppConfig
    {
        public string footprints_bin;
        public string subclusters_bin;
        public string clusters_bin;
        public string terrain_texture;
        public string dem_file;
        public float SCALE;
        public float VerticalSCALE;
        
        public float TerrainScale;
        public float RadiusScale;
        public int RevolutionResolution;
        public float[] GeoBounds;
        public float[] TextureBounds;
        public float maxRenderDistance;
    }

    [Serializable]
    public class PathConfig
    {
        public string dataFolder;
    }

    public static class Params
    {
        public static float SCALE = 0.015f;
        public static float TerrainScale = 0.015f;
        public static float RadiusScale = 0.2f;
        public static int RevolutionResolution = 12;
        public static Vector3 referenceCenter = new Vector3(0f, 0f, 0f);    
        public static float maxRenderDistance = 2500f;

        public static Vector3 Unity2LatLong(Vector3 unity_pos)
        {
            float cosLat = Mathf.Cos(referenceCenter.z * Mathf.Deg2Rad);
            float lon = unity_pos.x/SCALE/111000f;
            float lat = unity_pos.z/SCALE/111000f/cosLat;
            lon = lon + referenceCenter.x;
            lat = lat + referenceCenter.z;

            return new Vector3(lon, lat, 0);
        }

        public static Vector3 LatLong2Unity(float latitude, float longitude, float elevation)
        {
            float latDiff = latitude - referenceCenter.z;
            float lonDiff = longitude - referenceCenter.x;
            float elevDiff = elevation - referenceCenter.y;

            float latInMeters = latDiff * 111000f;
            float cosLat = Mathf.Cos(referenceCenter.z * Mathf.Deg2Rad);
            float lonInMeters = lonDiff * 111000f * cosLat;

            float x = lonInMeters * SCALE;
            float y = elevDiff * TerrainScale;
            float z = latInMeters * SCALE;

            // EXTREME ELEVATION
            return new Vector3(x, y, z);  // All in meters and w/ regard to reference centers
            // return new Vector3(x, elevation*0.75f, z);
        }
    }
    



    public class TerrainPoint
    {
        public Vector3 position;
        public float latitude;
        public float longitude;
        public float elevation;
        
        public TerrainPoint(Vector3 pos, float lat, float lon, float elev)
        {
            position = pos;
            latitude = lat;
            longitude = lon;
            elevation = elev;
        }
    }

    
    public class Footprint
    {
        public int N;
        public float latitude; // degree
        public float longitude; // degree
        public float elevation; // meters
        public float instrumentLat; // degree
        public float instrumentLon; // degree
        public float instrumentAlt; // meters
        public float[] rawWaveformValues; // relative signal strength
        public float[] rawWaveformPositions; // meters
        public Footprint(int N)
        {
            this.N = N;
            this.rawWaveformValues = new float[N];
            this.rawWaveformPositions = new float[N];
        }

        public void LoadValues(float[] rawWaveformValues)
        {
            for (int i=0;i<N;i++) this.rawWaveformValues[i] = rawWaveformValues[i];
        }

        public void LoadPositions(float[] rawWaveformPositions)
        {
            for (int i=0;i<N;i++) this.rawWaveformPositions[i] = rawWaveformPositions[i];
        }
    }

    
}

    