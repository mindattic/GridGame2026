using UnityEngine;

/// <summary>
/// Builds a grid mesh covering NDC-like space [-1,1] x [-1,1], with UV 0..1 and per-cell id in uv2.x
/// </summary>
public static class ShardMeshBuilder
{
    /// <summary>
    /// cols x rows grid. 40 x 22 works well for 16:9 at 1080p.
    /// </summary>
    public static Mesh BuildGrid(int cols, int rows)
    {
        int vertCount = (cols + 1) * (rows + 1);
        int quadCount = cols * rows;

        var verts = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var uv2 = new Vector2[vertCount];
        var tris = new int[quadCount * 6];

        int i = 0;
        for (int y = 0; y <= rows; y++)
        {
            float vf = (float)y / rows;
            for (int x = 0; x <= cols; x++)
            {
                float uf = (float)x / cols;
                verts[i] = new Vector3(uf * 2f - 1f, vf * 2f - 1f, 0f);
                uvs[i] = new Vector2(uf, vf);
                uv2[i] = Vector2.zero; // will be set per-cell below
                i++;
            }
        }

        int t = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int v00 = y * (cols + 1) + x;
                int v10 = v00 + 1;
                int v01 = v00 + (cols + 1);
                int v11 = v01 + 1;

                int shardId = y * cols + x;

                tris[t++] = v00; tris[t++] = v11; tris[t++] = v10;
                tris[t++] = v00; tris[t++] = v01; tris[t++] = v11;

                float idf = shardId + 0.5f;
                uv2[v00].x = idf;
                uv2[v10].x = idf;
                uv2[v01].x = idf;
                uv2[v11].x = idf;
            }
        }

        var m = new Mesh();
        if (vertCount > 65000)
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = verts;
        m.uv = uvs;
        m.uv2 = uv2;
        m.triangles = tris;
        // Prevent CPU frustum culling since we output in clip-space in the shader
        m.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        return m;
    }
}
