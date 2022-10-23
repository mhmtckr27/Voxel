using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(menuName = "SO/BlockDataSO", fileName = "BlockDataSO")]
public class BlockData : ScriptableObject
{
    public BlockType blockType;

    public Vector2Int frontUV;
    public Vector2Int backUV;
    public Vector2Int leftUV;
    public Vector2Int rightUV;
    public Vector2Int topUV;
    public Vector2Int bottomUV;

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