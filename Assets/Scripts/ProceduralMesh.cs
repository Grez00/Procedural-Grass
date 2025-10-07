using UnityEngine;
using System.Collections.Generic;

public class ProceduralMesh : MonoBehaviour
{
    [Header("Noise Params")]
    [SerializeField] private int pixSize;
    [SerializeField] private Vector2 noise_origin;
    [SerializeField] private float scale;
    [SerializeField] private float amplitude;
    [SerializeField] private float frequency;

    [Header("Mesh Params")]
    [SerializeField] int width;
    [SerializeField] int height;
    [SerializeField] int resolution;

    [Header("Material Params")]
    [SerializeField] Color highColour = Color.white;
    [SerializeField] Color lowColour = Color.white;

    private Material groundMaterial;
    [HideInInspector] public Texture2D noiseTex;
    private Color[] pixels;

    void OnEnable()
    {
        noiseTex = new Texture2D(pixSize, pixSize);
        pixels = new Color[noiseTex.width * noiseTex.height];

        CalcNoise();

        groundMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        groundMaterial.SetTexture("_NoiseTex", noiseTex);
        groundMaterial.SetColor("_Tint", highColour);
        groundMaterial.SetColor("_LowTint", lowColour);

        CreateMesh();
    }

    void CalcNoise()
    {
        for (float y = 0.0f; y < noiseTex.height; y++)
        {
            for (float x = 0.0f; x < noiseTex.width; x++)
            {
                float xCoord = (noise_origin.x + x / noiseTex.width) / scale * frequency;
                float yCoord = (noise_origin.y + y / noiseTex.height) / scale * frequency;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                sample *= amplitude;
                pixels[(int)y * noiseTex.width + (int)x] = new Color(sample, sample, sample);
            }
        }

        noiseTex.SetPixels(pixels);
        noiseTex.Apply();
    }

    void CreateMesh()
    {
        var mesh = new Mesh
        {
            name = "Procedural Mesh"
        };

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector4> tangents = new List<Vector4>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        float widthStep = (float)width / (float)resolution;
        float heightStep = (float)height / (float)resolution;

        for (float i = 0.0f; i <= height; i += heightStep)
        {
            for (float j = 0.0f; j <= width; j += widthStep)
            {
                vertices.Add(new Vector3(j, 0.0f, i));
                normals.Add(new Vector3(0.0f, 1.0f, 0.0f));
                tangents.Add(new Vector4(1f, 0f, 0f, -1f));
                uvs.Add(new Vector2(j / width, i / height));
            }
        }

        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                int index = j + ((resolution + 1) * i);

                indices.Add(index);
                indices.Add(index + (resolution + 1));
                indices.Add(index + 1);

                indices.Add(index + 1);
                indices.Add(index + (resolution + 1));
                indices.Add(index + (resolution + 1) + 1);
            }
        }

        mesh.vertices = vertices.ToArray();

        mesh.normals = normals.ToArray();

        mesh.tangents = tangents.ToArray();

        mesh.uv = uvs.ToArray();

        mesh.triangles = indices.ToArray();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;  
    }
}
