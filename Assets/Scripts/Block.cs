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
    private Chunk _parentChunk;
    
    public Block(BlockData blockData, Vector3Int position, Chunk parentChunk)
    {
        _blockData = blockData;
        _parentChunk = parentChunk;
        List<MyQuad> myQuads = new List<MyQuad>();

        if(blockData.blockType == BlockType.Air)
            return;
        
        Array quadSideArray = System.Enum.GetValues(typeof(QuadSide));
        
        for (int i = 0; i < quadSideArray.Length; i++)
        {
            QuadSide quadSide = (QuadSide)quadSideArray.GetValue(i);
            if(!_parentChunk.HasNeighbour(position, quadSide))
                myQuads.Add(new MyQuad(quadSide, _blockData.GetUV(quadSide), position));
        }
        
        if(myQuads.Count == 0)
            return;
        
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
    Sand,
    Air,
    WorldBottom,
    Diamond
}