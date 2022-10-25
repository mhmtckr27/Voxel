using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class PerlinGrapher : MonoBehaviour
{
    [SerializeField] public float heightMultiplier = 2f;
    [SerializeField] public float xzMultiplier = 0.5f;
    [SerializeField] public int octaveCount = 1;
    [SerializeField] public float yOffset;
    
    public LineRenderer lineRenderer;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 100;
        Graph();
    }

    private void Graph()
    {
        int z = 65;
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        for (int x = 0; x < positions.Length; x++)
        {
            float y = MeshUtils.FractalBrownianMotion(x, z, octaveCount, xzMultiplier, heightMultiplier, yOffset);
            positions[x] = new Vector3(x, y, z);
        }
        lineRenderer.SetPositions(positions);
    }

    private void OnValidate()
    {
        Graph();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
