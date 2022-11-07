using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyQuad
{
    public Mesh mesh;
    public MyQuad(QuadSide quadSide, Vector2Int uv, Vector3 position, float blockHealth)
    {
        mesh = new Mesh
        {
            name = "MyQuad"
        };

        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];
        Vector2[] uvs = new Vector2[4];
        Vector2[] uvs2 = new Vector2[4];
        int[] triangles = new int[6];

        int uvX0 = uv.x;
        int uvX1 = uv.x + 1;
        int uvY0 = uv.y;
        int uvY1 = uv.y + 1;
        
        Vector2 uv00 = new Vector2(uvX0 * 0.0625f, uvY0 * 0.0625f);
        Vector2 uv10 = new Vector2(uvX1 * 0.0625f, uvY0 * 0.0625f);
        Vector2 uv01 = new Vector2(uvX0 * 0.0625f, uvY1 * 0.0625f);
        Vector2 uv11 = new Vector2(uvX1 * 0.0625f, uvY1 * 0.0625f);

        uvs = new[]
        {
            uv11,
            uv01,
            uv00,
            uv10
        };

        int damageTextureIndex = 1;

        if (Math.Abs(blockHealth - 100) < 0.001f)
            damageTextureIndex = 12;
        else if (blockHealth > 87.5f)
            damageTextureIndex = 1;
        else if (blockHealth > 75f)
            damageTextureIndex = 2;
        else if (blockHealth > 62.5f)
            damageTextureIndex = 3;
        else if (blockHealth > 50f)
            damageTextureIndex = 4;
        else if (blockHealth > 37.5f)
            damageTextureIndex = 5;
        else if (blockHealth > 25f)
            damageTextureIndex = 6;
        else if (blockHealth > 12.5f)
            damageTextureIndex = 7;
        else
            damageTextureIndex = 8;
        

        uvs2 = new[]
        {
            new Vector2(damageTextureIndex * 0.0625f, 0.0625f),
            new Vector2((damageTextureIndex - 1) * 0.0625f, 0.0625f),
            new Vector2((damageTextureIndex - 1)  * 0.0625f, 0),
            new Vector2(damageTextureIndex * 0.0625f, 0),
        };

        Vector3 v0 = new Vector3(-0.5f, -0.5f, 0.5f) + position;
        Vector3 v1 = new Vector3(0.5f, -0.5f, 0.5f) + position;
        Vector3 v2 = new Vector3(0.5f, -0.5f, -0.5f) + position;
        Vector3 v3 = new Vector3(-0.5f, -0.5f, -0.5f) + position;
        Vector3 v4 = new Vector3(-0.5f, 0.5f, 0.5f) + position;
        Vector3 v5 = new Vector3(0.5f, 0.5f, 0.5f) + position;
        Vector3 v6 = new Vector3(0.5f, 0.5f, -0.5f) + position;
        Vector3 v7 = new Vector3(-0.5f, 0.5f, -0.5f) + position;


        switch (quadSide)
        {
            case QuadSide.Front:
                normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,  };
                vertices = new[] { v4, v5, v1, v0 };
                triangles = new[] { 3, 1, 0, 3, 2, 1 };
                break;
            case QuadSide.Back:
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back,  };
                vertices = new[] { v6, v7, v3, v2 };
                triangles = new[] { 3, 1, 0, 3, 2, 1 };
                break;
            case QuadSide.Left:
                normals = new[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left,  };
                vertices = new[] { v5, v6, v2, v1 };
                triangles = new[] { 3, 1, 0, 3, 2, 1 };
                break;
            case QuadSide.Right:
                normals = new[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right,  };
                vertices = new[] { v7, v4, v0, v3 };
                triangles = new[] { 3, 1, 0, 3, 2, 1 };
                break;
            case QuadSide.Top:
                normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up,  };
                vertices = new[] { v7, v6, v5, v4 };
                triangles = new[] { 3, 1, 0, 3, 2, 1 };
                break;
            case QuadSide.Bottom:
                normals = new[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down,  };
                vertices = new[] { v0, v1, v2, v3 };
                triangles = new[] { 3, 1, 0, 3, 2, 1 };
                break;
            default:
                break;
        }
        




        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.SetUVs(1, uvs2);
        mesh.normals = normals;
        mesh.triangles = triangles;
        
        mesh.RecalculateBounds();
    }
    
}
