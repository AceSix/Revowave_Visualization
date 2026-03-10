using UnityEngine;
using System;
using System.Collections.Generic;


using GEDIGlobals;

public class WaveformTools
{

    public static Mesh GenerateCylinderMesh(float[] rawWaveform, float[] rawPositions, Vector3 slantDirection)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> heights = new List<Vector2>(); // UV2 for height data
        
        int circleResolution = Params.RevolutionResolution;
        float angleIncrement = Mathf.PI * 2 / circleResolution;
        
        // vertices for each layer
        for (int i = 0; i < rawWaveform.Length; i++)
        {
            float y = rawPositions[i] * Params.TerrainScale;
            Vector3 slantOffset = slantDirection * y;
            float radius = rawWaveform[i];
            radius = radius * Params.RadiusScale * 25.0f;
            // radius = Math.Clamp(radius, 0f, 120f * Params.SCALE);
            // radius = Math.Clamp(radius, 0f, 12f * Params.SCALE);
            
            // create vertices for this circle
            for (int j = 0; j < circleResolution; j++)
            {
                float angle = j * angleIncrement;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);
                
                // slant offset
                vertices.Add(new Vector3(x + slantOffset.x, y, z + slantOffset.z));
                
                // UV mapping using height ratio
                float u = j / (float) circleResolution;
                float v = (rawPositions[i] - 10f)/80f; // height ratio for v
                uvs.Add(new Vector2(u, v));
                
                // actual height in meters (not scaled) in UV2
                heights.Add(new Vector2(rawPositions[i], 0));
            }
        }
        // generate triangles
        for (int i = 0; i < rawWaveform.Length - 1; i++)
        {
            int baseIndex = i * circleResolution;
            for (int j = 0; j < circleResolution; j++)
            {
                int nextJ = (j + 1) % circleResolution;
                
                // 1st triangle (ccw)
                triangles.Add(baseIndex + j);
                triangles.Add(baseIndex + nextJ);
                triangles.Add(baseIndex + circleResolution + j);
                
                // 2nd triangle
                triangles.Add(baseIndex + nextJ);
                triangles.Add(baseIndex + circleResolution + nextJ);
                triangles.Add(baseIndex + circleResolution + j);
            }
        }

        // add top and bottom caps
        int bottomStart = 0;
        int topStart = vertices.Count - circleResolution;

        // bottom cap
        for (int i = 1; i < circleResolution - 1; i++)
        {
            triangles.Add(bottomStart);
            triangles.Add(bottomStart + i);
            triangles.Add(bottomStart + i + 1);
        }

        // top cap
        for (int i = 1; i < circleResolution - 1; i++)
        {
            triangles.Add(topStart);
            triangles.Add(topStart + i + 1);
            triangles.Add(topStart + i);
        }

        Mesh mesh = new Mesh {
            name = "WaveformCylinderMesh",
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            uv2 = heights.ToArray() // Include UV2 with height data
        };
        mesh.RecalculateNormals();
    
        return mesh;
    }


    public static Vector3 CalculateISSDirection(Footprint dataPoint)
    {   
        // testing purposes only
        float slantAmplification = 1f;

        // raw direction vector in geographic coordinates
        float lonDiff = dataPoint.instrumentLon - dataPoint.longitude;
        float latDiff = dataPoint.instrumentLat - dataPoint.latitude;
        float elevDiff = dataPoint.instrumentAlt - dataPoint.elevation;

        // conv to meters
        float latInMeters = latDiff * 111000f;
        float cosLat = Mathf.Cos(dataPoint.latitude * Mathf.Deg2Rad);
        float lonInMeters = lonDiff * 111000f * cosLat;
        Vector3 rawDirection = new Vector3(lonInMeters, elevDiff, latInMeters);
        
        // extract horizontal and vertical
        float horizontalMagnitude = Mathf.Sqrt(rawDirection.x * rawDirection.x + rawDirection.z * rawDirection.z);
        float verticalComponent = rawDirection.y;
        
        // exaggerate horizontal by testing slant amp for testing
        float amplifiedHorizontal = horizontalMagnitude * slantAmplification;
        float directionRatio = amplifiedHorizontal / horizontalMagnitude;
        Vector3 amplifiedDirection = new Vector3(
            rawDirection.x * directionRatio,
            verticalComponent,
            rawDirection.z * directionRatio
        ).normalized;
        
        // elevation * 0.01, but not sure if correct
        return new Vector3(amplifiedDirection.x, amplifiedDirection.y * 0.01f, amplifiedDirection.z);
    }



}