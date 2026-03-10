using System;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using System.Collections.Generic;
using TriangleNet.Geometry;

using GEDIGlobals;
public class DEMTerrainCreator
{

    public static Mesh GenerateSolid(Texture2D demSrc, int resolution, Vector4 geoBounds, Vector4 textureBounds)
    {
        int verticesPerSide = resolution;
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int z = 0; z < verticesPerSide; z++)
        {
            for (int x = 0; x < verticesPerSide; x++)
            {
                // normalize grid coordinates in the range [0, 1]
                float u = x / (float)(verticesPerSide - 1);
                float v = z / (float)(verticesPerSide - 1);

                // using the grid's normalized u,v as image coordinates.

                float world_u = (u * (geoBounds.y - geoBounds.x) + geoBounds.x - textureBounds.x ) / (textureBounds.y - textureBounds.x);
                float world_v = (v * (geoBounds.w - geoBounds.z) + geoBounds.z - textureBounds.z ) / (textureBounds.w - textureBounds.z);

                float demValue = demSrc.GetPixelBilinear(world_u, world_v).r * Params.TerrainScale;
                // Debug.Log(demValue);

                vertices.Add(new Vector3(u, demValue, v));
                uvs.Add(new Vector2(world_u, world_v));
            }
        }

        // Create triangles (two per quad).
        for (int z = 0; z < verticesPerSide - 1; z++)
        {
            for (int x = 0; x < verticesPerSide - 1; x++)
            {
                int topLeft = z * verticesPerSide + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * verticesPerSide + x;
                int bottomRight = bottomLeft + 1;

                triangles.Add(topLeft);
                triangles.Add(bottomLeft);
                triangles.Add(topRight);
                triangles.Add(topRight);
                triangles.Add(bottomLeft);
                triangles.Add(bottomRight);
            }
        }

        // Create and assign the mesh.
        Mesh unityMesh = new Mesh();
        unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        unityMesh.vertices = vertices.ToArray();
        unityMesh.triangles = triangles.ToArray();
        unityMesh.uv = uvs.ToArray();
        unityMesh.RecalculateNormals();
        return unityMesh;
    }

    public static Mesh GenerateWireframe(Texture2D demSrc, int resolution, Vector4 geoBounds, Vector4 textureBounds)
    {
        int verticesPerSide = resolution;

        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        for (int z = 0; z < verticesPerSide; z++)
        {
            for (int x = 0; x < verticesPerSide; x++)
            {
                float u = x / (float)(verticesPerSide - 1);
                float v = z / (float)(verticesPerSide - 1);

                float world_u = (u * (geoBounds.y - geoBounds.x) + geoBounds.x - textureBounds.x) /
                                (textureBounds.y - textureBounds.x);

                float world_v = (v * (geoBounds.w - geoBounds.z) + geoBounds.z - textureBounds.z) /
                                (textureBounds.w - textureBounds.z);

                float demValue = demSrc.GetPixelBilinear(world_u, world_v).r * Params.TerrainScale;

                vertices.Add(new Vector3(u, demValue, v));
            }
        }

        // Create edges instead of triangles
        for (int z = 0; z < verticesPerSide - 1; z++)
        {
            for (int x = 0; x < verticesPerSide - 1; x++)
            {
                int topLeft = z * verticesPerSide + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * verticesPerSide + x;
                int bottomRight = bottomLeft + 1;

                // horizontal edge
                indices.Add(topLeft);
                indices.Add(topRight);

                // vertical edge
                indices.Add(topLeft);
                indices.Add(bottomLeft);
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();

        mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);

        return mesh;
    }

}
