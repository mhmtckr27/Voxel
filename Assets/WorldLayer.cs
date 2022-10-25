using System;
using Sirenix.OdinInspector;
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
        int z = 65;
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
        UnityEditor.Handles.Label(lineRenderer.GetPosition(0) + Vector3.up * 2, name);
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
    [SerializeField] public float probability;
    [SerializeField] public string worldLayerTag;
}