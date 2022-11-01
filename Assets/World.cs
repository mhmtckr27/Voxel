using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class World : MonoBehaviour
{
    [SerializeField] private Material atlasMat;
    [SerializeField] private Vector3Int chunkCount;
    [SerializeField] private Vector3Int chunkExtensionCount;
    [SerializeField] private Vector3Int chunkSize;
    [SerializeField] private int occlusionDistance;
    [SerializeField] private Transform chunksParent;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private GameObject mainWorldCamera;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private LayerMask playerLayer;
    [ShowInInspector] public static Dictionary<BlockType, BlockData> BlockDatas;

    [ShowInInspector] public static List<WorldLayer> Layers;
    // [ShowInInspector] public static WorldLayer CaveLayer;
    [ShowInInspector] public static PerlinGrapher3D CaveGrapher;

    [ShowInInspector] private HashSet<Vector3Int> _chunkCoordinates = new();
    [ShowInInspector] private HashSet<Vector2Int> _chunkColumns = new();
    [ShowInInspector] private Dictionary<Vector3Int, Chunk> _chunks = new();

    public HashSet<Vector3Int> ChunkCoordinates => _chunkCoordinates;
    public HashSet<Vector2Int> ChunkColumns => _chunkColumns;
    public Dictionary<Vector3Int, Chunk> Chunks => _chunks;
    public GameObject Player => player;
    public Vector3Int ChunkSize => chunkSize;
    public Vector3Int ChunkCount => chunkCount;

    private Vector3Int lastBuiltPosition;

    private Queue<IEnumerator> generateWorldQueue = new();

    public bool loadFromFile = true;
    
    private void Update()
    {
        if(drawBox)
            DrawBox(drawHitBlock, Quaternion.identity, new Vector3(1f, 1f, 1f), Color.black);
    }

    public void Dig()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out hit, 10))
        {
            Vector3Int hitBlock = Vector3Int.RoundToInt(hit.point - hit.normal / 2f);
            // Debug.LogError(hitBlock);
            // Debug.LogError(hitBlock);
            Chunk chunk = hit.collider.GetComponent<Chunk>();
            chunk.Dig(new Vector3Int(hitBlock.x % chunkSize.x, hitBlock.y % chunkSize.y, hitBlock.z % chunkSize.z));
        }
    }

    private bool drawBox;
    private Vector3Int drawHitBlock;
    
    public void Build()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out hit, 10))
        {
            Vector3Int hitBlock = Vector3Int.RoundToInt(hit.point + hit.normal / 2f);
            
            // Debug.LogError("HAYRI HIT POINT " + hitBlock);

            Collider[] colliders = new Collider[1];
            
            int overlapCount = Physics.OverlapBoxNonAlloc(hitBlock, new Vector3(0.5f, 0.5f, 0.5f), colliders, Quaternion.identity, playerLayer);
            drawBox = true;
            drawHitBlock = hitBlock;
            
            if (overlapCount > 0)
            {
                Debug.LogError("THERE IS PLAYER, CANT BUILD!!");
                return;
            }

            // Chunk chunk = hit.collider.GetComponent<Chunk>();

            int chunkX;
            int chunkY;
            int chunkZ;

            if (hitBlock.x < 0 && hitBlock.x % 10 != 0)
                chunkX = (hitBlock.x - 10) / chunkSize.x * chunkSize.x;
            else
                chunkX = hitBlock.x / chunkSize.x * chunkSize.x;

            if (hitBlock.y < 0 && hitBlock.y % 10 != 0)
                chunkY = (hitBlock.y - 10) / chunkSize.y * chunkSize.y;
            else
                chunkY = hitBlock.y / chunkSize.y * chunkSize.y;

            if (hitBlock.z < 0 && hitBlock.z % 10 != 0)
                chunkZ = (hitBlock.z - 10) / chunkSize.z * chunkSize.z;
            else
                chunkZ = hitBlock.z / chunkSize.z * chunkSize.z;
            
            Vector3Int chunkCoords = new Vector3Int(chunkX, chunkY, chunkZ);
            
            // Debug.LogError("HAYRI CHUNK : " + chunkCoords);
            Chunk chunk;
            if (!_chunkCoordinates.Contains(chunkCoords))
            {
                _chunkCoordinates.Add(chunkCoords);
                GameObject chunkObj = new GameObject($"Chunk_{chunkCoords.x}_{chunkCoords.y}_{chunkCoords.z}");
                chunkObj.transform.position = chunkCoords;
                chunkObj.transform.SetParent(chunksParent);
                chunk = chunkObj.AddComponent<Chunk>();
                BlockType[] blockTypes = new BlockType[chunkSize.x * chunkSize.y * chunkSize.z];
                for (int i = 0; i < blockTypes.Length; i++)
                {
                    blockTypes[i] = BlockType.Air;
                }
                chunk.Init(chunkSize, atlasMat, blockTypes);
                
                chunk.GenerateChunk(false);
                _chunks.TryAdd(chunkCoords, chunk);
                _chunks[chunkCoords].ShowChunk(true);
            }
            chunk = _chunks[chunkCoords];

            hitBlock.x %= chunkSize.x;
            hitBlock.y %= chunkSize.y;
            hitBlock.z %= chunkSize.z;
            
            if (hitBlock.x < 0)
                hitBlock.x += chunkSize.x;
            if (hitBlock.y < 0)
                hitBlock.y += chunkSize.y;
            if (hitBlock.z < 0)
                hitBlock.z += chunkSize.z;
            
            
            // Debug.LogError("HAYRI HIT POINT RELATIVE" + hitBlock);
            
            chunk.Build(hitBlock);
        }
    }

    public void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
    {
        // create matrix
        Matrix4x4 m = new Matrix4x4();
        m.SetTRS(pos, rot, scale);
 
        var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
        var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
        var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
        var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));
 
        var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
        var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
        var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
        var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));
 
        Debug.DrawLine(point1, point2, c);
        Debug.DrawLine(point2, point3, c);
        Debug.DrawLine(point3, point4, c);
        Debug.DrawLine(point4, point1, c);
 
        Debug.DrawLine(point5, point6, c);
        Debug.DrawLine(point6, point7, c);
        Debug.DrawLine(point7, point8, c);
        Debug.DrawLine(point8, point5, c);
 
        Debug.DrawLine(point1, point5, c);
        Debug.DrawLine(point2, point6, c);
        Debug.DrawLine(point3, point7, c);
        Debug.DrawLine(point4, point8, c);
 
    }
    
    private void RefreshBlockDatas()
    {
        BlockDatas = new Dictionary<BlockType, BlockData>();

        var blockDataAssets = Resources.LoadAll("Blocks", typeof(BlockData)).Cast<BlockData>();

        foreach (BlockData blockDataAsset in blockDataAssets)
        {
            BlockDatas.Add(blockDataAsset.blockType, blockDataAsset);
        }
    }
    
    [Button]
    private void DeleteExistingChunks()
    {
        int childCount = chunksParent.transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            DestroyImmediate(chunksParent.transform.GetChild(0).gameObject);
        }
        
        _chunks.Clear();
        _chunkColumns.Clear();
        _chunkCoordinates.Clear();
    }
    
    [Button]
    public void GenerateWorld(string saveFileName = "")
    {
        FindObjectOfType<UIController>(true).cursor.SetActive(false);
        DeleteExistingChunks();
        RefreshBlockDatas();
        WorldLayer[] layers = GetComponentsInChildren<WorldLayer>();
        Layers = layers.Where(x => !x.layerParams.worldLayerTag.Contains("Cave")).OrderByDescending(x => x.transform.GetSiblingIndex()).ToList();
        CaveGrapher = FindObjectOfType<PerlinGrapher3D>();
        // CaveLayer = layers.First(x => x.layerParams.worldLayerTag.Contains("Cave"));
        player.SetActive(false);
        mainWorldCamera.SetActive(true);
        playerCamera.SetActive(false);
        loadingBar.maxValue = chunkCount.x * chunkCount.z;
        loadingBar.value = 0;
        if(loadFromFile)
            StartCoroutine(LoadWorld(saveFileName));
        else
            StartCoroutine(GenerateWorldRoutine());

        WorldSaver.GetAllSaveFiles();
    }

    IEnumerator GenerateWorldExtensionRoutine()
    {
        int xStart = chunkCount.x;
        int zStart = chunkCount.z;
        int xEnd = chunkCount.x + chunkExtensionCount.x;
        int zEnd = chunkCount.z + chunkExtensionCount.z;
        
        for (int x = xStart; x < xEnd; x++)
        {
            for (int z = 0; z < zEnd; z++)
            {
                GenerateWorldColumn(x, z, false);
                yield return null;
            }
        }
        
        for (int x = 0; x < xEnd; x++)
        {
            for (int z = zStart; z < zEnd; z++)
            {
                GenerateWorldColumn(x, z, false);
                yield return null;
            }
        }
    }

    [Button]
    IEnumerator GenerateWorldRoutine()
    {
        for (int x = 0; x < chunkCount.x; x++)
        {
            for (int z = 0; z < chunkCount.z; z++)
            {
                GenerateWorldColumn(x, z);
                loadingBar.value++;
                yield return null;
            }
        }

        int playerPosX = (chunkCount.x * chunkSize.x) / 2;
        int playerPosZ = (chunkCount.z * chunkSize.z) / 2;
        int playerPosY = (int) MeshUtils.FractalBrownianMotion(playerPosX, playerPosZ, Layers.Last().layerParams);
        playerPosY += 5;

        player.transform.position = new Vector3(playerPosX, playerPosY, playerPosZ);
        
        lastBuiltPosition = Vector3Int.CeilToInt(player.transform.position);
        
        if(Application.isPlaying)
        {
            mainWorldCamera.SetActive(false);
            playerCamera.SetActive(true);
            player.SetActive(true);
            FindObjectOfType<UIController>(true).cursor.SetActive(true);
            FindObjectOfType<UIController>(true).SelectBlock(1);
        }

        StartCoroutine(UpdateWorldIterator());
        StartCoroutine(UpdateWorld());
        StartCoroutine(GenerateWorldExtensionRoutine());
    }

    private void GenerateWorldColumn(int x, int z, bool showChunk = true)
    {
        for (int y = 0; y < chunkCount.y; y++)
        {
            Vector3Int chunkCoords = new Vector3Int(x * chunkSize.x, y * chunkSize.y, z * chunkSize.z);

            if (_chunkCoordinates.Contains(chunkCoords))
            {
                _chunks[chunkCoords].ShowChunk(showChunk);
                continue;
            }
            
            GameObject chunkObj = new GameObject($"Chunk_{x}_{y}_{z}");
            chunkObj.transform.position = chunkCoords;
            chunkObj.transform.SetParent(chunksParent);
            Chunk chunk = chunkObj.AddComponent<Chunk>();
            chunk.Init(chunkSize, atlasMat, null);
            chunk.GenerateChunk();
            _chunkCoordinates.Add(chunkCoords);
            _chunks.TryAdd(chunkCoords, chunk);
            _chunks[chunkCoords].ShowChunk(showChunk);
        }

        _chunkColumns.Add(new Vector2Int(x, z));
    }

    private IEnumerator UpdateWorldIterator()
    {
        while (true)
        {
            while (generateWorldQueue.Count > 0)
            {
                yield return StartCoroutine(generateWorldQueue.Dequeue());
            }

            yield return null;
        }
    }

    private IEnumerator GenerateWorldRecursive(int x, int z, int radius)
    {
        radius--;
        if(radius <= 0)
            yield break;

        
        GenerateWorldColumn(x, z + 1);
        generateWorldQueue.Enqueue(GenerateWorldRecursive(x, z + 1, radius));
        yield return null;
        
        GenerateWorldColumn(x, z - 1);
        generateWorldQueue.Enqueue(GenerateWorldRecursive(x, z - 1, radius));
        yield return null;
        
        GenerateWorldColumn(x + 1, z);
        generateWorldQueue.Enqueue(GenerateWorldRecursive(x + 1, z, radius));
        yield return null;
        
        GenerateWorldColumn(x - 1, z);
        generateWorldQueue.Enqueue(GenerateWorldRecursive(x - 1, z, radius));
        yield return null;
    }

    private IEnumerator UpdateWorld()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(0.5f);
        while (true)
        {
            if (Vector3.Distance(player.transform.position, lastBuiltPosition) > chunkSize.x)
            {
                Vector3Int playerChunkPosition = Vector3Int.RoundToInt(player.transform.position);
                lastBuiltPosition = playerChunkPosition;

                int playerChunkPositionX = playerChunkPosition.x / chunkSize.x;
                int playerChunkPositionZ = playerChunkPosition.z / chunkSize.z;

                generateWorldQueue.Enqueue(GenerateWorldRecursive(playerChunkPositionX, playerChunkPositionZ, occlusionDistance));
                generateWorldQueue.Enqueue(HideColumnsOutOfSight(playerChunkPositionX, playerChunkPositionZ));
            }

            yield return waitForSeconds;
        }
    }

    private IEnumerator HideColumnsOutOfSight(int x, int z)
    {
        Vector2Int fpcPos = new Vector2Int(x, z);

        foreach (Vector2Int chunkColumn in _chunkColumns)
        {
            if(Vector2Int.Distance(fpcPos, chunkColumn) > occlusionDistance)
                HideWorldColumn(chunkColumn.x, chunkColumn.y);
        }

        yield return null;
    }
    
    private void HideWorldColumn(int x, int z)
    {
        for (int y = 0; y < chunkCount.y; y++)
        {
            Vector3Int chunkCoordToHide = new Vector3Int(x * chunkSize.x, y * chunkSize.y, z * chunkSize.z);
            if(_chunkCoordinates.Contains(chunkCoordToHide))
                _chunks[chunkCoordToHide].ShowChunk(false);
        }
    }

    public IEnumerator LoadWorld(string saveFileName)
    {
        WorldSaveData worldSaveData = WorldSaver.Load($"{saveFileName}.dat");

        if (worldSaveData == null)
            yield break;

        _chunkCoordinates = new HashSet<Vector3Int>();
        foreach (SerializableVector3Int chunkCoordinate in worldSaveData.chunkCoordinates)
        {
            _chunkCoordinates.Add(chunkCoordinate);
        }

        _chunkColumns = new HashSet<Vector2Int>();
        foreach (SerializableVector2Int chunkColumn in worldSaveData.chunkColumns)
        {
            _chunkColumns.Add(chunkColumn);
        }

        loadingBar.maxValue = _chunkCoordinates.Count;
        loadingBar.value = 0;
        
        _chunks = new Dictionary<Vector3Int, Chunk>();
        for (int i = 0; i < _chunkCoordinates.Count; i++)
        {
            Vector3Int chunkCoords = _chunkCoordinates.ElementAt(i);
            GameObject chunkObj = new GameObject($"Chunk_{chunkCoords.x}_{chunkCoords.y}_{chunkCoords.z}");
            chunkObj.transform.position = chunkCoords;
            chunkObj.transform.SetParent(chunksParent);
            Chunk chunk = chunkObj.AddComponent<Chunk>();
            chunk.Init(chunkSize, atlasMat, worldSaveData.chunkSaveDatas[i].blockTypes);
            chunk.GenerateChunk(false);
            _chunks.TryAdd(chunkCoords, chunk);
            _chunks[chunkCoords].ShowChunk(true);
            loadingBar.value++;
            yield return null;
        }

        player.transform.position = worldSaveData.playerPosition;
        
        if(Application.isPlaying)
        {
            mainWorldCamera.SetActive(false);
            playerCamera.SetActive(true);
            player.SetActive(true);
            FindObjectOfType<UIController>(true).cursor.SetActive(true);
            FindObjectOfType<UIController>(true).SelectBlock(1);
        }
    }

    // public static BlockType GetBlockTypeFrom(int blockCoordY, int perlinY)
    // {
    //     BlockType toReturn;
    //     
    //     if(blockCoordY == perlinY)
    //         toReturn = BlockType.Grass;
    //     else if (blockCoordY < perlinY)
    //         toReturn = BlockType.Dirt;
    //     else
    //         toReturn = BlockType.Air;
    //
    //     return toReturn;
    // }

}
