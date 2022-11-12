using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinGrapher3D : MonoBehaviour
{
    [SerializeField] public Vector3Int size;
    [SerializeField] public LayerParams layerParams;
    [SerializeField] public float drawCutoff;

    [SerializeField] private List<GameObject> cubes;
    
    private void CreateCubes()
    {
        cubes = new List<GameObject>();
        GameObject childObj = new GameObject("Cubes");
        childObj.transform.SetParent(transform);
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(childObj.transform);
                    cube.transform.position = new Vector3(x, y, z);
                    cubes.Add(cube);
                }
            }
        }
    }

    private void Graph()
    {
        if(cubes.Count == 0)
            CreateCubes();
        
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    float yP3D = MeshUtils.FractalBrownianMotion3D(x, y, z, layerParams);
                    cubes[x + size.x * (y + size.y * z)].SetActive(yP3D >= drawCutoff);
                }
            }
        }
    }

    private void OnValidate()
    {
        Graph();
    }
}
