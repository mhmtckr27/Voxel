using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

using VertexData = System.Tuple<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector2>;

public static class MeshUtils
{
    public static Mesh CombineMeshes(Mesh[] meshes)
    {
        Mesh combinedMesh = new Mesh()
        {
            name = "MyCombinedMesh"
        };

        Dictionary<VertexData, int> vertexOrders = new Dictionary<VertexData, int>();
        HashSet<VertexData> vertexHashSet = new HashSet<VertexData>();
        List<int> triangles = new List<int>();

        int vertexIndex = 0;

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i] == null)
            {
                continue;
            }
            
            for (int j = 0; j < meshes[i].vertices.Length; j++)
            {
                Vector3 v = meshes[i].vertices[j];
                Vector3 n = meshes[i].normals[j];
                Vector2 u = meshes[i].uv[j];

                VertexData vData = new VertexData(v, n, u);

                if (!vertexHashSet.Contains(vData))
                {
                    vertexOrders.Add(vData, vertexIndex);
                    vertexHashSet.Add(vData);

                    vertexIndex++;
                }
            }

            for (int k = 0; k < meshes[i].triangles.Length; k++)
            {
                int triIndex = meshes[i].triangles[k];
                
                Vector3 v = meshes[i].vertices[triIndex];
                Vector3 n = meshes[i].normals[triIndex];
                Vector2 u = meshes[i].uv[triIndex];

                VertexData vData = new VertexData(v, n, u);

                int index;
                vertexOrders.TryGetValue(vData, out index);
                triangles.Add(index);
            }

            meshes[i] = null;
        }

        Vector3[] vertices = new Vector3[vertexOrders.Count];
        Vector3[] normals = new Vector3[vertexOrders.Count];
        Vector2[] uvs = new Vector2[vertexOrders.Count];


        for (int i = 0; i < vertexOrders.Count; i++)
        {
            var keyValuePair = vertexOrders.ElementAt(i);
            vertices[i] = keyValuePair.Key.Item1;
            normals[i] = keyValuePair.Key.Item2;
            uvs[i] = keyValuePair.Key.Item3;
        }

        combinedMesh.vertices = vertices;
        combinedMesh.normals = normals;
        combinedMesh.uv = uvs;
        combinedMesh.triangles = triangles.ToArray();
        
        combinedMesh.RecalculateBounds();

        return combinedMesh;
    }
    
    
    public static float FractalBrownianMotion(float x, float z, int octaveCount, float xzMultiplier, float heightMultiplier, float yOffset)
    {
        float total = 0;
        float frequency = 1;
        for (int i = 0; i < octaveCount; i++)
        {
            total += Mathf.PerlinNoise(x * xzMultiplier * frequency, z * xzMultiplier * frequency) * heightMultiplier;
            frequency *= 2;
        }

        total += yOffset;
        
        return total;
    }
}
