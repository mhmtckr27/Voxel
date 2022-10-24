using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Block
{
    private BlockData _blockData;
    public Mesh Mesh;
    
    public Block(BlockData blockData, Vector3 position)
    {
        _blockData = blockData;
        MyQuad[] myQuads = new MyQuad[6];

        Array quadSideArray = System.Enum.GetValues(typeof(QuadSide));
        
        for (int i = 0; i < quadSideArray.Length; i++)
        {
            QuadSide quadSide = (QuadSide)quadSideArray.GetValue(i);
            myQuads[i] = new MyQuad(quadSide, _blockData.GetUV(quadSide), position);
        }

        var meshes = myQuads.Select(x => x.mesh).ToArray(); 
        
        Mesh = MeshUtils.CombineMeshes(meshes);
    }
}

public enum QuadSide
{
    Front,
    Back,
    Left,
    Right,
    Top,
    Bottom
}

public enum BlockType
{
    Grass,
    Dirt,
    Stone,
    Water,
    Sand
}