using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class WorldCreator : MonoBehaviour
{
    [SerializeField] private Material atlasMat;
    [SerializeField] private Vector2Int chunkCount;
    [SerializeField] private Vector3Int chunkSize;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private GameObject mainWorldCamera;
    [SerializeField] private GameObject player;
    [ShowInInspector] public static Dictionary<BlockType, BlockData> BlockDatas;


    public static List<WorldLayer> Layers;

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
    private void GenerateWorld()
    {
        RefreshBlockDatas();
        Layers = GetComponentsInChildren<WorldLayer>().OrderBy(x => x.layerParams.yOffset).ToList();
        player.SetActive(false);
        mainWorldCamera.SetActive(true);
        loadingBar.maxValue = chunkCount.x * chunkCount.y;
        loadingBar.value = 0;
        StartCoroutine(GenerateWorldRoutine());
    }
    
    [Button]
    IEnumerator GenerateWorldRoutine()
    {
        for (int x = 0; x < chunkCount.x; x++)
        {
            for (int y = 0; y < chunkCount.y; y++)
            {
                GameObject chunkObj = new GameObject($"Chunk_{x}_{y}");
                chunkObj.transform.position = new Vector3(x * chunkSize.x, 0, y * chunkSize.z);
                chunkObj.transform.SetParent(transform);
                Chunk chunk = chunkObj.AddComponent<Chunk>();
                chunk.Init(chunkSize, atlasMat);
                chunk.GenerateChunk();
                loadingBar.value++;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.5f);
        mainWorldCamera.SetActive(false);
        player.SetActive(true);
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
