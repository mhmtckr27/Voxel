using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Block : MonoBehaviour
{
    [SerializeField] private BlockData _blockData;
    [SerializeField] private Material _atlasMat;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    
    void Start()
    {
        
        Debug.Log(_blockData.GetUV(QuadSide.Front));
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = _atlasMat;

        MyQuad[] myQuads = new MyQuad[6];

        Array quadSideArray = System.Enum.GetValues(typeof(QuadSide));
        
        for (int i = 0; i < quadSideArray.Length; i++)
        {
            QuadSide quadSide = (QuadSide)quadSideArray.GetValue(i);
            myQuads[i] = new MyQuad(quadSide, _blockData.GetUV(quadSide));
        }

        var meshes = myQuads.Select(x => x.mesh).ToArray(); 
        
        Mesh combinedMesh = MeshUtils.CombineMeshes(meshes);

        _meshFilter.mesh = combinedMesh;
    }

    // Update is called once per frame
    void Update()
    {
        
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