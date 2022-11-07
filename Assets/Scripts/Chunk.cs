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
    
    [ShowInInspector] private BlockType[] _blockTypes;
    private float[] _blockHealths;
    private PopulateBlockTypesJob _populateBlockTypesJob;
    
    private JobHandle _populateBlockTypesJobHandle;

    public BlockType[] GetBlockTypes() => _blockTypes;
    
    public void Init(Vector3Int chunkSize, Material atlasMat, BlockType[] blockTypes)
    {
        this.chunkSize = chunkSize;
        this.atlasMat = atlasMat;
        this._blockTypes = blockTypes;
        _blockHealths = new float[ChunkSizeTotal];
    }

    public void ShowChunk(bool show)
    {
        _meshRenderer.enabled = show;
    }

    private void PopulateBlockTypesArray()
    {
        _blockTypes = new BlockType[ChunkSizeTotal];
        _blockHealths = new float[ChunkSizeTotal];

        NativeArray<BlockType> blockTypes = new NativeArray<BlockType>(_blockTypes, Allocator.Persistent);
        NativeArray<float> damageLevels = new NativeArray<float>(_blockHealths, Allocator.Persistent);

        var randomGenerators = new Unity.Mathematics.Random[ChunkSizeTotal];

        uint randomSeed = (uint) Time.time;
        if (randomSeed == 0)
            randomSeed++;
        
        for (int i = 0; i < ChunkSizeTotal; i++)
        {
            randomGenerators[i] = new Unity.Mathematics.Random(randomSeed);
        }

        var _randomGenerators = new NativeArray<Unity.Mathematics.Random>(randomGenerators, Allocator.Persistent);
        
        _populateBlockTypesJob = new PopulateBlockTypesJob()
        {
            BlockTypes = blockTypes,
            BlockHealths = damageLevels,
            ChunkSize = chunkSize,
            Location = transform.position,
            RandomGenerators = _randomGenerators,
            RandomGenerator = new Unity.Mathematics.Random(randomSeed)
        };

        _populateBlockTypesJobHandle = _populateBlockTypesJob.Schedule(ChunkSizeTotal, 64);
        _populateBlockTypesJobHandle.Complete();

        _populateBlockTypesJob.BlockTypes.CopyTo(_blockTypes);
        _populateBlockTypesJob.BlockHealths.CopyTo(_blockHealths);
        
        
        blockTypes.Dispose();
        damageLevels.Dispose();
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
                    BlockType blockType = GetBlockTypeFrom(new Vector3Int(x, y, z));
                    float blockHealth = _blockHealths[FromCoordinates(new Vector3Int(x, y, z))];
                    // damageLevel = FromCoordinates(new Vector3Int(x, y, z)) % 11;
                    _blocks[x, y, z] = new Block(World.BlockDatas[blockType], new Vector3Int(x, y, z), this, blockHealth);
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
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, stream: 2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1, stream: 3));

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
                neighbourType = GetBlockTypeFrom(new Vector3Int(coords.x,  coords.y, coords.z + 1));
                break;
            case QuadSide.Back:
                neighbourType = GetBlockTypeFrom(new Vector3Int(coords.x, coords.y, coords.z - 1));
                break;
            case QuadSide.Left:
                neighbourType = GetBlockTypeFrom(new Vector3Int(coords.x + 1, coords.y, coords.z));
                break;
            case QuadSide.Right:
                neighbourType = GetBlockTypeFrom(new Vector3Int(coords.x - 1, coords.y, coords.z));
                break;
            case QuadSide.Top:
                neighbourType = GetBlockTypeFrom(new Vector3Int(coords.x, coords.y + 1, coords.z));
                break;
            case QuadSide.Bottom:
                neighbourType = GetBlockTypeFrom(new Vector3Int(coords.x, coords.y - 1, coords.z));
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
    

    private BlockType GetBlockTypeFrom(Vector3Int coords)
    {
        /* Flat[x + chunkSizeX * (y + chunkSizeY * z)] = Original[x, y, z] */
        // x = index % chunkSizeX
        // y = (index / chunkSizeX) % chunkSizeZ
        // z = index / (chunkSizeX * chunkSizeZ)
        
        int index = FromCoordinates(coords);
        if (coords.x < 0 || coords.x > chunkSize.x - 1 ||
            coords.y < 0 || coords.y > chunkSize.y - 1 ||
            coords.z < 0 || coords.z > chunkSize.z - 1)
            return BlockType.Air;
        
        return _blockTypes[index];
    }

    private BlockData GetBlockDataFrom(Vector3Int coords)
    {
        /* Flat[x + chunkSizeX * (y + chunkSizeY * z)] = Original[x, y, z] */
        // x = index % chunkSizeX
        // y = (index / chunkSizeX) % chunkSizeZ
        // z = index / (chunkSizeX * chunkSizeZ)

        if (coords.x < 0 || coords.x > chunkSize.x - 1 ||
            coords.y < 0 || coords.y > chunkSize.y - 1 ||
            coords.z < 0 || coords.z > chunkSize.z - 1)
            return null;

        return _blocks[coords.x, coords.y, coords.z].BlockData;
    }
    
    public void Dig(Vector3Int blockCoord, float damageAmount)
    {
        int blockIndex = FromCoordinates(blockCoord);
        
        if(_blockTypes[blockIndex] == BlockType.WorldBottom)
            return;

        BlockData blockData = GetBlockDataFrom(blockCoord);
        
        _blockHealths[blockIndex] -= damageAmount * blockData.blockDurability;
        if (_blockHealths[blockIndex] <= 0)
        {
            _blockTypes[blockIndex] = BlockType.Air;
            _blockHealths[blockIndex] = 100;
        }
        DestroyImmediate(_meshRenderer);
        DestroyImmediate(_meshFilter);
        DestroyImmediate(_meshCollider);
        GenerateChunk(false);
    }

    public void Build(Vector3Int buildCoord)
    {
        int buildIndex = FromCoordinates(buildCoord);
        _blockTypes[buildIndex] = FindObjectOfType<UIController>().selectedBlock;
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

            var uvs2 = new NativeArray<float3>(vertexCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            mesh.GetUVs(1, uvs2.Reinterpret<Vector3>());

            var outputVertices = OutputMesh.GetVertexData<Vector3>(stream: 0);
            var outputNormals = OutputMesh.GetVertexData<Vector3>(stream: 1);
            var outputUVs = OutputMesh.GetVertexData<Vector3>(stream: 2);
            var outputUVs2 = OutputMesh.GetVertexData<Vector3>(stream: 3);

            for (int i = 0; i < vertexCount; i++)
            {
                outputVertices[vertexStart + i] = vertices[i];
                outputNormals[vertexStart + i] = normals[i];
                outputUVs[vertexStart + i] = uvs[i];
                outputUVs2[vertexStart + i] = uvs2[i];
            }

            vertices.Dispose();
            normals.Dispose();
            uvs.Dispose();
            uvs2.Dispose();

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
        public NativeArray<float> BlockHealths;
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
            for (int j = 0; j < World.Layers.Count; j++)
            {
                int y = (int) MeshUtils.FractalBrownianMotion(coordX, coordZ, World.Layers[j].layerParams);
                yList.Add(y);
            }

            float yCave = MeshUtils.FractalBrownianMotion3D(coordX, coordY, coordZ, World.CaveGrapher.layerParams);
            // Debug.LogError("HAYRIII " + yCave);

            BlockHealths[i] = 100;
            
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
                List<ProbabilityData> probabilityDatas = World.Layers.Last().layerParams.probabilityDatas;
                
                if(probabilityDatas == null || probabilityDatas.Count == 0)
                    BlockTypes[i] = World.Layers.Last().blockType;
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
                while (index < World.Layers.Count && coordY > yList[index])
                    index++;

                List<ProbabilityData> probabilityDatas = World.Layers[index - 1].layerParams.probabilityDatas;
                
                if(probabilityDatas == null || probabilityDatas.Count == 0)
                    BlockTypes[i] = World.Layers[index - 1].blockType;
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

            if (yCave < World.CaveGrapher.drawCutoff)
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
