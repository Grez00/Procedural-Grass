using UnityEngine;
using System.Collections.Generic;

// Purpose of this script is to create the terrain which the grass will sit on top of
public class ProceduralMesh : MonoBehaviour
{
    // These parameters control the heightmap of the terrain
    [Header("Noise Params")]
    [SerializeField] private int pixSize;
    [SerializeField] private Vector2 noise_origin;
    [SerializeField] private float scale;
    [SerializeField] private float amplitude;
    [SerializeField] private float frequency;

    [Header("Mesh Params")]
    // World units width and height of terrain
    [SerializeField] public int width;
    [SerializeField] public int height;
    [SerializeField] int resolution; // Density of vertices in plane
    [SerializeField] public float meshAmplitude;

    // Terrain colour changes with height
    [Header("Material Params")]
    [SerializeField] Color highColour = Color.white;
    [SerializeField] Color lowColour = Color.white;

    // If you want the most simple terrain possible
    // (No height offset, 2 tris)
    [Header("Simplify Mesh")]
    [SerializeField] private bool simpleMesh;

    private Material groundMaterial;
    [HideInInspector] public Texture2D noiseTex;
    private Color[] pixels;

    void OnEnable()
    {
        noiseTex = new Texture2D(pixSize, pixSize);
        pixels = new Color[noiseTex.width * noiseTex.height];

        CalcNoise();

        MaterialPropertyBlock matProp = new MaterialPropertyBlock();
        matProp.SetTexture("_NoiseTex", noiseTex);
        matProp.SetColor("_Tint", highColour);
        matProp.SetColor("_LowTint", lowColour);
        matProp.SetFloat("_Amplitude", meshAmplitude);
        GetComponent<Renderer>().SetPropertyBlock(matProp);

        CreateMesh();
    }

    public void CalcNoise()
    {
        for (float y = 0.0f; y < noiseTex.height; y++)
        {
            for (float x = 0.0f; x < noiseTex.width; x++)
            {
                if (simpleMesh)
                {
                    pixels[(int)y * noiseTex.width + (int)x] = new Color(0, 0, 0);
                }
                else
                {
                    float xCoord = (noise_origin.x + x / noiseTex.width) / scale * frequency;
                    float yCoord = (noise_origin.y + y / noiseTex.height) / scale * frequency;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    sample *= amplitude;
                    pixels[(int)y * noiseTex.width + (int)x] = new Color(sample, sample, sample);
                }
            }
        }

        noiseTex.SetPixels(pixels);
        noiseTex.Apply();
    }

    public void SetOrigin(Vector2 new_origin)
    {
        noise_origin = new Vector2(new_origin.x, new_origin.y);
    }

    void CreateMesh()
    {
        if (simpleMesh) resolution = 1;

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
