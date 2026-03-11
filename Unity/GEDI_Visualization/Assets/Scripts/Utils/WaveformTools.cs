using UnityEngine;
using System;
using System.Collections.Generic;
using GEDIGlobals;

public class WaveformTools
{
    public static Mesh GenerateCylinderMesh(float[] rawWaveform, float[] rawPositions, Vector3 slantDirection)
    {
        if (rawWaveform == null) throw new ArgumentNullException(nameof(rawWaveform));
        if (rawPositions == null) throw new ArgumentNullException(nameof(rawPositions));

        if (rawWaveform.Length != rawPositions.Length)
        {
            throw new ArgumentException(
                $"rawWaveform.Length ({rawWaveform.Length}) != rawPositions.Length ({rawPositions.Length})");
        }

        if (rawWaveform.Length < 2)
        {
            throw new ArgumentException(
                $"Need at least 2 waveform samples to build a cylinder, got {rawWaveform.Length}");
        }

        int circleResolution = Params.RevolutionResolution;
        if (circleResolution < 3)
        {
            throw new ArgumentException(
                $"Params.RevolutionResolution must be >= 3, got {circleResolution}");
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector2> heights = new List<Vector2>();

        float angleIncrement = Mathf.PI * 2f / circleResolution;

        // Build rings
        for (int i = 0; i < rawWaveform.Length; i++)
        {
            float y = rawPositions[i] * Params.TerrainScale;
            Vector3 slantOffset = slantDirection * y;

            float radius = rawWaveform[i] * Params.RadiusScale * 25.0f;

            // Optional: clamp negative / invalid radii
            if (float.IsNaN(radius) || float.IsInfinity(radius))
                radius = 0f;

            if (radius < 0f)
                radius = 0f;

            for (int j = 0; j < circleResolution; j++)
            {
                float angle = j * angleIncrement;
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);

                vertices.Add(new Vector3(
                    x + slantOffset.x,
                    y,
                    z + slantOffset.z
                ));

                float u = j / (float)circleResolution;
                float v = (rawPositions[i] - 10f) / 80f;
                uvs.Add(new Vector2(u, v));
                heights.Add(new Vector2(rawPositions[i], 0f));
            }
        }

        int ringCount = rawWaveform.Length;

        // Side faces
        for (int i = 0; i < ringCount - 1; i++)
        {
            int baseIndex = i * circleResolution;
            int nextBaseIndex = (i + 1) * circleResolution;

            for (int j = 0; j < circleResolution; j++)
            {
                int nextJ = (j + 1) % circleResolution;

                int a = baseIndex + j;
                int b = baseIndex + nextJ;
                int c = nextBaseIndex + j;
                int d = nextBaseIndex + nextJ;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        // Real cap center vertices
        int bottomCenterIndex = vertices.Count;
        {
            float y = rawPositions[0] * Params.TerrainScale;
            Vector3 slantOffset = slantDirection * y;
            vertices.Add(new Vector3(slantOffset.x, y, slantOffset.z));
            uvs.Add(new Vector2(0.5f, 0.5f));
            heights.Add(new Vector2(rawPositions[0], 0f));
        }

        int topCenterIndex = vertices.Count;
        {
            int last = ringCount - 1;
            float y = rawPositions[last] * Params.TerrainScale;
            Vector3 slantOffset = slantDirection * y;
            vertices.Add(new Vector3(slantOffset.x, y, slantOffset.z));
            uvs.Add(new Vector2(0.5f, 0.5f));
            heights.Add(new Vector2(rawPositions[last], 0f));
        }

        // Bottom cap
        int bottomRingStart = 0;
        for (int j = 0; j < circleResolution; j++)
        {
            int nextJ = (j + 1) % circleResolution;
            triangles.Add(bottomCenterIndex);
            triangles.Add(bottomRingStart + j);
            triangles.Add(bottomRingStart + nextJ);
        }

        // Top cap
        int topRingStart = (ringCount - 1) * circleResolution;
        for (int j = 0; j < circleResolution; j++)
        {
            int nextJ = (j + 1) % circleResolution;
            triangles.Add(topCenterIndex);
            triangles.Add(topRingStart + nextJ);
            triangles.Add(topRingStart + j);
        }

        // Validate indices before assigning
        int vertexCount = vertices.Count;
        if (triangles.Count % 3 != 0)
        {
            throw new Exception($"Triangle list length is not divisible by 3: {triangles.Count}");
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            int idx = triangles[i];
            if (idx < 0 || idx >= vertexCount)
            {
                throw new Exception(
                    $"Triangle index out of range at triangles[{i}] = {idx}, vertexCount = {vertexCount}");
            }
        }

        if (uvs.Count != vertexCount || heights.Count != vertexCount)
        {
            throw new Exception(
                $"Attribute count mismatch. vertices={vertexCount}, uv={uvs.Count}, uv2={heights.Count}");
        }

        Mesh unityMesh = new Mesh();
        unityMesh.name = "WaveformCylinderMesh";
        unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        unityMesh.SetVertices(vertices);
        unityMesh.SetUVs(0, uvs);
        unityMesh.SetUVs(1, heights);
        unityMesh.SetTriangles(triangles, 0, true);
        unityMesh.RecalculateNormals();
        unityMesh.RecalculateBounds();

        return unityMesh;
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