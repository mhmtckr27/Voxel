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
    [SerializeField] private Material atlasMat;

    private int ChunkSizeTotal => chunkSize.x * chunkSize.y * chunkSize.z;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    private Block[,,] _blocks;
    
    /* Flat[x + chunkSizeX * (y + chunkSizeY * z)] = Original[x, y, z] */
    // x = index % chunkSizeX
    // y = (index / chunkSizeX) % chunkSizeZ
    // z = index / (chunkSizeX * chunkSizeZ)
    
    private BlockType[] _blockTypes;
    private PopulateBlockTypesJob _populateBlockTypesJob;
    
    private JobHandle _populateBlockTypesJobHandle;

    public void Init(Vector3Int chunkSize, Material atlasMat)
    {
        this.chunkSize = chunkSize;
        this.atlasMat = atlasMat;
    }

    public void ShowChunk(bool show)
    {
        _meshRenderer.enabled = show;
    }

    private void PopulateBlockTypesArray()
    {
        _blockTypes = new BlockType[ChunkSizeTotal];

        NativeArray<BlockType> blockTypes = new NativeArray<BlockType>(_blockTypes, Allocator.Persistent);

        var randomGenerators = new Unity.Mathematics.Random[ChunkSizeTotal];

        for (int i = 0; i < ChunkSizeTotal; i++)
        {
            randomGenerators[i] = new Unity.Mathematics.Random((uint) Time.time);
        }

        var _randomGenerators = new NativeArray<Unity.Mathematics.Random>(randomGenerators, Allocator.Persistent);
        
        _populateBlockTypesJob = new PopulateBlockTypesJob()
        {
            BlockTypes = blockTypes,
            ChunkSize = chunkSize,
            Location = transform.position,
            RandomGenerators = _randomGenerators,
            RandomGenerator = new Unity.Mathematics.Random((uint) Time.time)
        };

        _populateBlockTypesJobHandle = _populateBlockTypesJob.Schedule(ChunkSizeTotal, 64);
        _populateBlockTypesJobHandle.Complete();

        _populateBlockTypesJob.BlockTypes.CopyTo(_blockTypes);
        
        blockTypes.Dispose();
        _randomGenerators.Dispose();
    }

    [Button]
    public void GenerateChunk(bool populateBlockTypesArray = true)
    {
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = atlasMat;
        _blocks = new Block[chunkSize.x, chunkSize.y, chunkSize.z];
        
        if(populateBlockTypesArray)
            PopulateBlockTypesArray();

        List<Mesh> inputMeshes = new List<Mesh>();
        int vertexStart = 0;
        int triangleStart = 0;
        int meshCount = ChunkSizeTotal;
        int index = 0;
        
        var jobs = new ProcessMeshDataJob
        {
            VertexStartArray = new NativeArray<int>(ChunkSizeTotal, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
            TriangleStartArray = new NativeArray<int>(ChunkSizeTotal, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)
        };

        for (int x = 0; x < chunkSize.x; x++)
        {
            for (int y = 0; y < chunkSize.y; y++)
            {
                for (int z = 0; z < chunkSize.z; z++)
                {
                    BlockType blockType = GetBlockDataFrom(new Vector3Int(x, y, z));
                    _blocks[x, y, z] = new Block(WorldCreator.BlockDatas[blockType], new Vector3Int(x, y, z), this);
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
        _meshCollider = gameObject.AddComponent<MeshCollider>();
        _meshCollider.sharedMesh = _meshFilter.sharedMesh;
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

    public Vector3Int ToCoordinates(int index)
    {
        Vector3Int toReturn = new Vector3Int();
        
        toReturn.x = index % chunkSize.x;
        toReturn.y = (index / chunkSize.x) % chunkSize.z;
        toReturn.z = index / (chunkSize.x * chunkSize.z);
        
        return toReturn;
    }

    public int FromCoordinates(Vector3Int coordinates)
    {
       return coordinates.x + chunkSize.x * (coordinates.y + chunkSize.y * coordinates.z);   
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
    
    public void Dig(Vector3Int blockCoord)
    {
        int blockIndex = FromCoordinates(blockCoord);
        _blockTypes[blockIndex] = BlockType.Air;
        DestroyImmediate(_meshRenderer);
        DestroyImmediate(_meshFilter);
        DestroyImmediate(_meshCollider);
        GenerateChunk(false);
    }

    public void Build(Vector3Int buildCoord)
    {
        int buildIndex = FromCoordinates(buildCoord);
        _blockTypes[buildIndex] = BlockType.Dirt;
        DestroyImmediate(_meshRenderer);
        DestroyImmediate(_meshFilter);
        DestroyImmediate(_meshCollider);
        GenerateChunk(false);
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

    struct PopulateBlockTypesJob : IJobParallelFor
    {
        public NativeArray<BlockType> BlockTypes;
        public Vector3Int ChunkSize;
        public Vector3 Location;
        public NativeArray<Unity.Mathematics.Random> RandomGenerators;
        public Unity.Mathematics.Random RandomGenerator;
        
        public void Execute(int i)
        {
            Vector3Int coords = GetCoordinates(i);
            
            int coordX = coords.x + (int) Location.x;
            int coordY = coords.y + (int) Location.y;
            int coordZ = coords.z + (int) Location.z;

            var randomGenerator = RandomGenerators[i];
            
            List<int> yList = new List<int>();
            for (int j = 0; j < WorldCreator.Layers.Count; j++)
            {
                int y = (int) MeshUtils.FractalBrownianMotion(coordX, coordZ, WorldCreator.Layers[j].layerParams);
                yList.Add(y);
            }

            float yCave = MeshUtils.FractalBrownianMotion3D(coordX, coordY, coordZ, WorldCreator.CaveGrapher.layerParams);
            // Debug.LogError("HAYRIII " + yCave);

            if (coordY == 0)
            {
                BlockTypes[i] = BlockType.WorldBottom;
                return;
            }
            else if (coordY > yList.Last())
            {
                BlockTypes[i] = BlockType.Air;
                return;
            }
            else if(coordY == yList.Last())
            {
                List<ProbabilityData> probabilityDatas = WorldCreator.Layers.Last().layerParams.probabilityDatas;
                
                if(probabilityDatas == null || probabilityDatas.Count == 0)
                    BlockTypes[i] = WorldCreator.Layers.Last().blockType;
                else
                {
                    float random = RandomGenerator.NextFloat(0f, 1f);
                    float totalSoFar = 0f;
                    for (int k = 0; k < probabilityDatas.Count; k++)
                    {
                        totalSoFar += probabilityDatas[k].probability;
                        if (random < totalSoFar)
                        {
                            BlockTypes[i] = probabilityDatas[k].blockType;
                            break;
                        }
                    }
                }
            }
            else
            {
                int index = 0;
                while (index < WorldCreator.Layers.Count && coordY > yList[index])
                    index++;

                List<ProbabilityData> probabilityDatas = WorldCreator.Layers[index - 1].layerParams.probabilityDatas;
                
                if(probabilityDatas == null || probabilityDatas.Count == 0)
                    BlockTypes[i] = WorldCreator.Layers[index - 1].blockType;
                else
                {
                    float random = RandomGenerator.NextFloat(0f, 1f);
                    float totalSoFar = 0f;
                    for (int k = 0; k < probabilityDatas.Count; k++)
                    {
                        totalSoFar += probabilityDatas[k].probability;
                        if (random < totalSoFar)
                        {
                            BlockTypes[i] = probabilityDatas[k].blockType;
                            break;
                        }
                    }
                }
            }

            if (yCave < WorldCreator.CaveGrapher.drawCutoff)
                BlockTypes[i] = BlockType.Air;
        }
        
        public Vector3Int GetCoordinates(int index)
        {
            /* Flat[x + chunkSizeX * (y + chunkSizeY * z)] = Original[x, y, z] */
            // x = index % chunkSizeX
            // y = (index / chunkSizeX) % chunkSizeZ
            // z = index / (chunkSizeX * chunkSizeZ)
        
            Vector3Int toReturn = new Vector3Int();
        
            toReturn.x = index % ChunkSize.x;
            toReturn.y = (index / ChunkSize.x) % ChunkSize.z;
            toReturn.z = index / (ChunkSize.x * ChunkSize.z);
        
            return toReturn;
        }
    }
}
