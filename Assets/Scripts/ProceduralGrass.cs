using UnityEngine;

public class ProceduralGrass : MonoBehaviour
{
    [Header("Grass Params")]
    [SerializeField] private int resolution;
    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
    [SerializeField] private Vector2 bendFactor;
    [SerializeField] [Range(0.0f, 1.0f)] private float ambientOcclusion;
    [SerializeField][Range(0.0f, 1.0f)] private float smoothness;
    [SerializeField] private Color mainColour;
    [SerializeField] private Color tipColour;
    [SerializeField] private GameObject grass;

    [Header("Wind Params")]
    [SerializeField] private float windAmplitude;
    [SerializeField] private float windFrequency;
    [SerializeField] private Vector2 windDirection;

    [Header("Noise Params")]
    [SerializeField] private int pixSize;
    [SerializeField] private Vector2 noise_origin;
    [SerializeField] private Vector2 wind_origin;
    [SerializeField] private float scale;
    [SerializeField] private float amplitude;
    [SerializeField] private float frequency;

    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private ComputeShader mowUpdateShader;
    [SerializeField] private RenderTexture mowTex;
    private ComputeBuffer transformMatrixBuffer;

    private Collider groundCollider;
    private Mesh groundMesh;

    private Vector2 min;
    private Vector2 max;
    private float size;
    private MaterialPropertyBlock properties;
    private Material grassMaterial;
    private Mesh grassMesh;
    private float grassHeight;
    private float grassCenter;
    private Matrix4x4[] matrices;
    private Texture noiseTex;
    private Texture sizeTex;
    private Texture windTex;
    private RenderTexture accumMowTex;

    void Start()
    {
        grassMaterial = grass.GetComponent<MeshRenderer>().sharedMaterial;
        grassMesh = grass.GetComponent<MeshFilter>().sharedMesh;
        grassHeight = grassMesh.bounds.size.y;
        grassCenter = grassMesh.bounds.center.y;

        noiseTex = GetComponent<ProceduralMesh>().noiseTex;
        sizeTex = CalcNoise(pixSize, scale, amplitude, frequency, noise_origin);
        windTex = CalcNoise(pixSize, scale, amplitude, frequency, wind_origin);
        grassMaterial.SetTexture("_NoiseTex", noiseTex);
        grassMaterial.SetTexture("_WindTex", windTex);

        DispatchComputeShader();
    }

    void OnEnable()
    {
        if (grassMaterial == null) grassMaterial = grass.GetComponent<MeshRenderer>().sharedMaterial;

        if (mowTex != null)
        {
            accumMowTex = new RenderTexture(mowTex.width, mowTex.height, 0, RenderTextureFormat.ARGBFloat);
            accumMowTex.enableRandomWrite = true;
            mowTex.enableRandomWrite = true;
        }

        noiseTex = GetComponent<ProceduralMesh>().noiseTex;
        sizeTex = CalcNoise(pixSize, scale, amplitude, frequency, noise_origin);
        windTex = CalcNoise(pixSize, scale, amplitude, frequency, noise_origin);
        grassMaterial.SetTexture("_NoiseTex", noiseTex);
        grassMaterial.SetTexture("_WindTex", windTex);
        grassMaterial.SetColor("_MainColour", mainColour);
        grassMaterial.SetColor("_TipColour", tipColour);
        grassMaterial.SetVector("_BendFactor", new Vector4(bendFactor.x, bendFactor.y, 0.0f, 0.0f));
        grassMaterial.SetFloat("_AAFactor", ambientOcclusion);
        grassMaterial.SetFloat("_Smoothness", smoothness);
        grassMaterial.SetVector("_Min", new Vector4(min.x, min.y, 0, 0));
        grassMaterial.SetVector("_Max", new Vector4(max.x, max.y, 0, 0));
        grassMaterial.SetFloat("_WindAmplitude", windAmplitude);
        grassMaterial.SetFloat("_WindFrequency", windFrequency);
        grassMaterial.SetVector("_WindDirection", new Vector4(windDirection.x, windDirection.y, 0, 0));
    }

    void Update()
    {
        if (mowTex != null)
        {
            UpdateMowTexGPU();
            grassMaterial.SetTexture("_MowTex", accumMowTex);
        }
        GPUInstantiate_ComputeShader();
    }

    Texture2D RenderTextoTex2D(RenderTexture rendTex)
    {
        Texture2D resultTexture = new Texture2D(rendTex.width, rendTex.height);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rendTex;
        resultTexture.ReadPixels(new Rect(0, 0, rendTex.width, rendTex.height), 0, 0);
        resultTexture.Apply();
        RenderTexture.active = prev;
        return resultTexture;
    }

    Texture2D BlackTexture(int width, int height)
    {
        Texture2D newTex = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = Color.black;
            }
        }
        newTex.SetPixels(pixels);
        newTex.Apply();
        return newTex;
    }

    /*
    void UpdateMowTex()
    {
        Color[] pixels = accumMowTex.GetPixels();
        Texture2D rendTex = RenderTextoTex2D(mowTex);

        for (int y = 0; y < rendTex.height; y++)
        {
            for (int x = 0; x < rendTex.width; x++)
            {
                if (rendTex.GetPixel(x, y).r == 1.0f)
                {
                    pixels[y * rendTex.width + x] = Color.white;
                }
            }
        }
        accumMowTex.SetPixels(pixels);
        accumMowTex.Apply();
    }
    */

    Texture2D CalcNoise(int pixSize, float scale, float amplitude, float frequency, Vector2 origin)
    {
        Texture2D tex = new Texture2D(pixSize, pixSize);
        Color[] pixels = new Color[noiseTex.width * noiseTex.height];

        for (float y = 0.0f; y < tex.height; y++)
        {
            for (float x = 0.0f; x < tex.width; x++)
            {
                float xCoord = (origin.x + x / tex.width) / scale * frequency;
                float yCoord = (origin.y + y / tex.height) / scale * frequency;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                sample *= amplitude;
                pixels[(int)y * tex.width + (int)x] = new Color(sample, sample, sample);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    Matrix4x4[] GetTransformationMatrices()
    {
        groundCollider = GetComponent<Collider>();
        min = new Vector2(groundCollider.bounds.min.x, groundCollider.bounds.min.z);
        max = new Vector2(groundCollider.bounds.max.x, groundCollider.bounds.max.z);

        if (max.x - min.x != max.y - min.y)
        {
            Debug.Log("Ground plane is not square");
        }

        size = max.x - min.x;

        float rotation;
        float offsetX;
        float offsetZ;

        Matrix4x4[] result = new Matrix4x4[(resolution + 1) * (resolution + 1)];
        for (float i = 0.0f; i <= (float)resolution; i++)
        {
            for (float j = 0.0f; j <= (float)resolution; j++)
            {
                rotation = Random.Range(-180.0f, 180.0f);
                offsetX = Random.Range(0.0f, (size / (float)resolution) * 0.5f);
                offsetZ = Random.Range(0.0f, (size / (float)resolution) * 0.5f);

                Vector3 position = Vector3.zero;
                position.x = min.x + offsetX + i * (size / (float)resolution);
                position.z = min.y + offsetZ + j * (size / (float)resolution);

                position.y = transform.position.y + grass.transform.localScale.z + 1.0f;

                result[(int)j + ((int)i * resolution)] = Matrix4x4.TRS(position, Quaternion.Euler(90, rotation, 0), grass.transform.localScale);
            }
        }
        return result;
    }

    void UpdateMowTexGPU()
    {
        mowUpdateShader.SetTexture(0, "_InputTex", mowTex);
        mowUpdateShader.SetTexture(0, "_ResultTex", accumMowTex);

        mowUpdateShader.SetInt("_TexWith", mowTex.width);
        mowUpdateShader.SetInt("_TexHeight", mowTex.height);

        uint threadGroupSizeX;
        uint threadGroupSizeY;
        mowUpdateShader.GetKernelThreadGroupSizes(0, out threadGroupSizeX, out threadGroupSizeY, out _);
        int threadGroupsX = Mathf.CeilToInt(mowTex.width / threadGroupSizeX);
        int threadGroupsY = Mathf.CeilToInt(mowTex.height / threadGroupSizeY);
        mowUpdateShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

    void DispatchComputeShader()
    {
        groundCollider = GetComponent<Collider>();

        min = new Vector2(groundCollider.bounds.min.x, groundCollider.bounds.min.z);
        max = new Vector2(groundCollider.bounds.max.x, groundCollider.bounds.max.z);

        int bufferSize = (resolution + 1) * (resolution + 1);
        transformMatrixBuffer = new ComputeBuffer(bufferSize, sizeof(float) * 16, ComputeBufferType.Structured);
        computeShader.SetBuffer(0, "_TransformMatrices", transformMatrixBuffer);

        computeShader.SetTexture(0, "_NoiseTex", noiseTex);
        computeShader.SetTexture(0, "_SizeTex", sizeTex);

        computeShader.SetVector("_Min", new Vector4(min.x, min.y, 0, 0));
        computeShader.SetVector("_Max", new Vector4(max.x, max.y, 0, 0));
        computeShader.SetFloat("_YPos", transform.position.y);
        computeShader.SetVector("_Scale", new Vector4(grass.transform.localScale.x, grass.transform.localScale.y, grass.transform.localScale.z, 0));
        computeShader.SetMatrix("_RotationMatrix", Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(90, 45, 0), new Vector3(1, 1, 1)));
        computeShader.SetInt("_Resolution", resolution);
        computeShader.SetFloat("_MinHeight", minHeight);
        computeShader.SetFloat("_MaxHeight", maxHeight);
        computeShader.SetFloat("_GrassHeight", grassHeight);
        computeShader.SetFloat("_GrassCenter", grassCenter);

        uint threadGroupSizeX;
        uint threadGroupSizeY;
        computeShader.GetKernelThreadGroupSizes(0, out threadGroupSizeX, out threadGroupSizeY, out _);
        int threadGroupsX = Mathf.CeilToInt((resolution + 1) / threadGroupSizeX);
        int threadGroupsY = Mathf.CeilToInt((resolution + 1) / threadGroupSizeY);
        computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

    void GPUInstantiate()
    {
        if (grassMesh != null && grassMaterial != null)
        {
            Graphics.DrawMeshInstanced(
                grassMesh,
                0,
                grassMaterial,
                matrices
            );
        }
        else
        {
            grassMaterial = grass.GetComponent<MeshRenderer>().sharedMaterial;
            grassMesh = grass.GetComponent<MeshFilter>().sharedMesh;
        }
    }

    void GPUInstantiate_ComputeShader()
    {
        Matrix4x4[] grassMatrices = new Matrix4x4[(resolution + 1) * (resolution + 1)];
        transformMatrixBuffer.GetData(grassMatrices);
        
        if (grassMesh != null && grassMaterial != null)
        {
            Graphics.DrawMeshInstanced(
                grassMesh,
                0,
                grassMaterial,
                grassMatrices
            );
        }
        else
        {
            grassMaterial = grass.GetComponent<MeshRenderer>().sharedMaterial;
            grassMesh = grass.GetComponent<MeshFilter>().sharedMesh;
        } 
    }
}
