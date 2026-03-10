using UnityEngine;
using System;
using System.Collections.Generic;

namespace GEDIGlobals
{

    public class Params
    {
        public const float SCALE = 0.015f;
        public const float TerrainScale = 0.015f;
        public const float RadiusScale = 0.2f;
        public const int RevolutionResolution = 12;


        // public Vector4 geoBounds = new Vector4(-71.5f, -71.4f, -46.6f, -46.5f);

        // public Vector4 textureBounds = new Vector4(-71.5f, -71.4f, -46.6f, -46.5f);

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

    