using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class WorldLayer : MonoBehaviour
{
    [SerializeField] public LayerParams layerParams;
    [SerializeField] public BlockType blockType;
    
    public LineRenderer lineRenderer;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 100;
        Graph();
    }

    private void Graph()
    {
        int z = -1;
        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        for (int x = 0; x < positions.Length; x++)
        {
            float y = MeshUtils.FractalBrownianMotion(x, z, layerParams);
            positions[x] = new Vector3(x, y, z);
        }
        lineRenderer.SetPositions(positions);
    }

    private void OnValidate()
    {
        name = $"Layer_{layerParams.worldLayerTag}";
        Graph();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        UnityEditor.Handles.color = Color.black;
        UnityEditor.Handles.Label(lineRenderer.GetPosition(0) + Vector3.up * 4 + Vector3.left * 3, name, new GUIStyle(){fontSize = 16});
    }
#endif
}

[Serializable]
public class LayerParams
{
    [SerializeField] public float heightMultiplier = 2f;
    [SerializeField] public float xzMultiplier = 0.5f;
    [SerializeField] public int octaveCount = 1;
    [SerializeField] public float yOffset;
    [SerializeField] public List<ProbabilityData> probabilityDatas;
    [SerializeField] public string worldLayerTag;
}

[Serializable]
public struct ProbabilityData
{
    public BlockType blockType;
    public float probability;
}