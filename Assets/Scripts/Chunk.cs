using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Chunk : MonoBehaviour
{
    [SerializeField] private Vector3Int chunkSize;
    [ShowInInspector] private Dictionary<BlockType, BlockData> blockDatas;
    [SerializeField] private Material atlasMat;
    
    [Title("Perlin Noise")]
    [SerializeField] private float heightMultiplier = 2f;
    [SerializeField] private float xzMultiplier = 0.5f;
    [SerializeField] private int octaveCount = 1;
    [SerializeField] private float yOffset;

    private int ChunkSizeTotal => chunkSize.x * chunkSize.y * chunkSize.z;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private Block[,,] _blocks;
    
    /* Flat[x + chunkSizeX * (y + chunkSizeY * z)] = Original[x, y, z] */
    // x = index % chunkSizeX
    // y = (index / chunkSizeX) % chunkSizeZ
    // z = index / (chunkSizeX * chunkSizeZ)
    private BlockType[] _blockTypes;

    private void PopulateChunkTypesArray()
    {
        _blockTypes = new BlockType[ChunkSizeTotal];

        // int counter = 0;
        // for (int x = 0; x < chunkSize.x; x++)
        // {
        //     for (int y = 0; y < chunkSize.y; y++)
        //     {
        //         for (int z = 0; z < chunkSize.z; z++)
        //         {
        //             float yFract = MeshUtils.FractalBrownianMotion(x, z, octaveCount, xzMultiplier, heightMultiplier, yOffset);
        //             
        //             if(y < yFract)
        //                 _blockTypes[counter] = BlockType.Dirt;
        //             else
        //                 _blockTypes[counter] = BlockType.Air;
        //
        //             counter++;
        //         }
        //     }
        // }
        
        for (int i = 0; i < ChunkSizeTotal; i++)
        {
            Vector3Int coords = GetCoordinates(i);
            float y = MeshUtils.FractalBrownianMotion(coords.x, coords.z, octaveCount, xzMultiplier, heightMultiplier, yOffset);
            if(coords.y < y)
                _blockTypes[i] = BlockType.Dirt;
            else
                _blockTypes[i] = BlockType.Air;
        }
    }
    
    [Button]
    void Test()
    {
        RefreshBlockDatas();
        _meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = atlasMat;
        _blocks = new Block[chunkSize.x, chunkSize.y, chunkSize.z];
        PopulateChunkTypesArray();

        List<Mesh> inputMeshes = new List<Mesh>();
        int vertexStart = 0;
        int triangleStart = 0;
        int meshCount = ChunkSizeTotal;
        int index = 0;
        
        var jobs = new ProcessMeshDataJob();
        jobs.VertexStartArray = new NativeArray<int>(ChunkSizeTotal, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        jobs.TriangleStartArray = new NativeArray<int>(ChunkSizeTotal, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        
        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    BlockType blockType = GetBlockDataFrom(new Vector3Int(x, y, z));
                    _blocks[x, y, z] = new Block(blockDatas[blockType], new Vector3Int(x, y, z), this);
                    if (_blocks[x, y, z].Mesh != null)
                    {
                        inputMeshes.Add(_blocks[x, y, z].Mesh);
                        int vertexCount = _blocks[x, y, z].Mesh.vertexCount;
                        int indexCount = (int)_blocks[x, y, z].Mesh.GetIndexCount(0);
                        jobs.VertexStartArray[index] = vertexStart;
                        jobs.TriangleStartArray[index] = triangleStart;
                        vertexStart += vertexCount;
                        triangleStart += indexCount;
                        index++;
                    }
                }
            }
        }

        // _meshFilter.mesh = MeshUtils.CombineMeshes(meshes);
        // _meshFilter.mesh.RecalculateBounds();
        // return;
        
        jobs.MeshDataArray = Mesh.AcquireReadOnlyMeshData(inputMeshes);
        Mesh.MeshDataArray outputMeshDataArray = Mesh.AllocateWritableMeshData(1);
        jobs.OutputMesh = outputMeshDataArray[0];
        jobs.OutputMesh.SetIndexBufferParams(triangleStart, IndexFormat.UInt32);
        jobs.OutputMesh.SetVertexBufferParams(vertexStart,
            new VertexAttributeDescriptor(VertexAttribute.Position, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2));

        JobHandle jobHandle = jobs.Schedule(inputMeshes.Count, 4);

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

    private void RefreshBlockDatas()
    {
        blockDatas = new Dictionary<BlockType, BlockData>();

        var blockDataAssets = Resources.LoadAll("Blocks", typeof(BlockData)).Cast<BlockData>();

        foreach (BlockData blockDataAsset in blockDataAssets)
        {
            blockDatas.Add(blockDataAsset.blockType, blockDataAsset);
        }
    }

    struct ProcessMeshDataJob : IJobParallelFor
    {
        [Unity.Collections.ReadOnly] public Mesh.MeshDataArray MeshDataArray;
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
            var outputTriangles = OutputMesh.GetIndexData<int>();
            
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

    public Vector3Int GetCoordinates(int index)
    {
        /* Flat[x + chunkSizeX * (y + chunkSizeY * z)] = Original[x, y, z] */
        // x = index % chunkSizeX
        // y = (index / chunkSizeX) % chunkSizeZ
        // z = index / (chunkSizeX * chunkSizeZ)
        
        Vector3Int toReturn = new Vector3Int();
        
        toReturn.x = index % chunkSize.x;
        toReturn.y = (index / chunkSize.x) % chunkSize.z;
        toReturn.z = index / (chunkSize.x * chunkSize.z);
        
        return toReturn;
    }
    
    public bool HasNeighbour(Vector3Int coords, QuadSide quadSide)
    {
        BlockType neighbourType = GetNeighbourInDirection(coords, quadSide);
        return neighbourType != BlockType.Air;
    }

    private BlockType GetNeighbourInDirection(Vector3Int coords, QuadSide quadSide)
    {
        BlockType neighbourType = BlockType.Air;

        switch (quadSide)
        {
            case QuadSide.Front:
                neighbourType = GetBlockDataFrom(new Vector3Int(coords.x,  coords.y, coords.z + 1));
                break;
            case QuadSide.Back:
                neighbourType = GetBlockDataFrom(new Vector3Int(coords.x, coords.y, coords.z - 1));
                break;
            case QuadSide.Left:
                neighbourType = GetBlockDataFrom(new Vector3Int(coords.x + 1, coords.y, coords.z));
                break;
            case QuadSide.Right:
                neighbourType = GetBlockDataFrom(new Vector3Int(coords.x - 1, coords.y, coords.z));
                break;
            case QuadSide.Top:
                neighbourType = GetBlockDataFrom(new Vector3Int(coords.x, coords.y + 1, coords.z));
                break;
            case QuadSide.Bottom:
                neighbourType = GetBlockDataFrom(new Vector3Int(coords.x, coords.y - 1, coords.z));
                break;
            default:
                break;
        }

        return neighbourType;
    }
    
    private BlockType GetBlockDataFrom(Vector3Int coords)
    {
        /* Flat[x + chunkSizeX * (y + chunkSizeY * z)] = Original[x, y, z] */
        // x = index % chunkSizeX
        // y = (index / chunkSizeX) % chunkSizeZ
        // z = index / (chunkSizeX * chunkSizeZ)
        
        int index = coords.x + chunkSize.x * (coords.y + chunkSize.y * coords.z);
        if (coords.x < 0 || coords.x > chunkSize.x - 1 ||
            coords.y < 0 || coords.y > chunkSize.y - 1 ||
            coords.z < 0 || coords.z > chunkSize.z - 1)
            return BlockType.Air;
        
        return _blockTypes[index];
    }
}
