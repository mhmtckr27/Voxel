using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class Chunk : MonoBehaviour
{
    [SerializeField] private Vector3Int chunkSize;
    [SerializeField] private BlockData grassData;
    [SerializeField] private Material atlasMat;
    
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private Block[,,] _blocks;
    
    void Start()
    {
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = atlasMat;
        int chunkSizeTotal = chunkSize.x * chunkSize.y * chunkSize.z; 
        _blocks = new Block[chunkSize.x, chunkSize.y, chunkSize.z];


        Mesh[] meshes = new Mesh[chunkSizeTotal];
        int vertexStart = 0;
        int triangleStart = 0;
        int meshCount = chunkSizeTotal;
        int index = 0;
        
        var jobs = new ProcessMeshDataJob();
        jobs.VertexStartArray = new NativeArray<int>(chunkSizeTotal, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobs.TriangleStartArray = new NativeArray<int>(chunkSizeTotal, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    _blocks[x, y, z] = new Block(grassData, new Vector3(x, y, z));
                    meshes[index] = _blocks[x, y, z].Mesh;
                    int vertexCount = _blocks[x, y, z].Mesh.vertexCount;
                    int indexCount = (int) _blocks[x, y, z].Mesh.GetIndexCount(0);
                    jobs.VertexStartArray[index] = vertexStart;
                    jobs.TriangleStartArray[index] = triangleStart;
                    vertexStart += vertexCount;
                    triangleStart += indexCount;
                    index++;
                }
            }
        }

        jobs.MeshDataArray = Mesh.AcquireReadOnlyMeshData(meshes);
        Mesh.MeshDataArray outputMeshDataArray = Mesh.AllocateWritableMeshData(1);
        jobs.OutputMesh = outputMeshDataArray[0];
        jobs.OutputMesh.SetIndexBufferParams(triangleStart, IndexFormat.UInt32);
        jobs.OutputMesh.SetVertexBufferParams(vertexStart,
            new VertexAttributeDescriptor(VertexAttribute.Position, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2));

        JobHandle jobHandle = jobs.Schedule(meshCount, 4);

        Mesh newMesh = new Mesh()
        {
            name = "Chunk_0"
        };

        SubMeshDescriptor subMeshDescriptor = new SubMeshDescriptor(0, triangleStart, MeshTopology.Triangles);
        subMeshDescriptor.firstVertex = 0;
        subMeshDescriptor.vertexCount = vertexStart;
        
        jobHandle.Complete();

        jobs.OutputMesh.subMeshCount = 1;
        jobs.OutputMesh.SetSubMesh(0, subMeshDescriptor);

        Mesh.ApplyAndDisposeWritableMeshData(outputMeshDataArray, new[] { newMesh });
        jobs.MeshDataArray.Dispose();
        jobs.VertexStartArray.Dispose();
        jobs.TriangleStartArray.Dispose();
        
        newMesh.RecalculateBounds();

        _meshFilter.mesh = newMesh;
    }

    struct ProcessMeshDataJob : IJobParallelFor
    {
        [ReadOnly] public Mesh.MeshDataArray MeshDataArray;
        public Mesh.MeshData OutputMesh;
        public NativeArray<int> VertexStartArray;
        public NativeArray<int> TriangleStartArray;

        public void Execute(int index)
        {
            Mesh.MeshData mesh = MeshDataArray[index];
            int vertexCount = mesh.vertexCount;
            int vertexStart = VertexStartArray[index];

            var vertices = new NativeArray<float3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            mesh.GetVertices(vertices.Reinterpret<Vector3>());
            
            var normals = new NativeArray<float3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            mesh.GetNormals(normals.Reinterpret<Vector3>());
            
            var uvs = new NativeArray<float3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            mesh.GetUVs(0, uvs.Reinterpret<Vector3>());

            var outputVertices = OutputMesh.GetVertexData<Vector3>(stream: 0);
            var outputNormals = OutputMesh.GetVertexData<Vector3>(stream: 1);
            var outputUVs = OutputMesh.GetVertexData<Vector3>(stream: 2);

            for (int i = 0; i < vertexCount; i++)
            {
                outputVertices[vertexStart + i] = vertices[i];
                outputNormals[vertexStart + i] = normals[i];
                outputUVs[vertexStart + i] = uvs[i];
            }

            vertices.Dispose();
            normals.Dispose();
            uvs.Dispose();

            int triangleStart = TriangleStartArray[index];
            int triangleCount = mesh.GetSubMesh(0).indexCount;
            var outputTriangles = mesh.GetIndexData<int>();

            
            
            if (mesh.indexFormat == IndexFormat.UInt16)
            {
                var triangles = mesh.GetIndexData<ushort>();
                for (int i = 0; i < triangleCount; i++)
                {
                    int idx = triangles[i];
                    outputTriangles[triangleStart + i] = vertexStart + idx;
                }
            }
            else
            {
                var triangles = mesh.GetIndexData<int>();
                for (int i = 0; i < triangleCount; i++)
                {
                    int idx = triangles[i];
                    outputTriangles[triangleStart + i] = vertexStart + idx;
                }
            }
        }
    }
}
