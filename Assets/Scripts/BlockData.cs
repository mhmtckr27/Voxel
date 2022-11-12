using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(menuName = "SO/BlockDataSO", fileName = "BlockDataSO")]
public class BlockData : ScriptableObject
{
    public BlockType blockType;
    public bool canDrop;

    public Vector2Int frontUV;
    public Vector2Int backUV;
    public Vector2Int leftUV;
    public Vector2Int rightUV;
    public Vector2Int topUV;
    public Vector2Int bottomUV;

    /// <summary>
    /// How much damage does this block takes from a single hit is determined by durability.
    /// When durability is higher, more hits required to dig this block. Different from block health which is
    /// 100 by default for all blocks.
    /// </summary>
    [Tooltip("How much damage does this block takes from a single hit is determined by durability. " +
             "When durability is lower, more hits required to dig this block. (damageDoneToBlock = damagePerSecond * blockDurability)  " +
             "Different from block health which is 100 by default for all blocks.")] 
    [Range(0, 1f)]
    public float blockDurability;

    public Vector2Int GetUV(QuadSide quadSide)
    {
        Vector2Int uvToReturn = Vector2Int.zero;
        switch (quadSide)
        {
            case QuadSide.Front:
                uvToReturn = frontUV;
                break;
            case QuadSide.Back:
                uvToReturn = backUV;
                break;
            case QuadSide.Left:
                uvToReturn = leftUV;
                break;
            case QuadSide.Right:
                uvToReturn = rightUV;
                break;
            case QuadSide.Top:
                uvToReturn = topUV;
                break;
            case QuadSide.Bottom:
                uvToReturn = bottomUV;
                break;
            default:
                break;
        }
        return uvToReturn;
    }
}