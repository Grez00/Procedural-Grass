using UnityEngine;
using System.Collections.Generic;

public class ProceduralGrass : MonoBehaviour
{
    [Header("Grass Params")]
    [SerializeField] private int resolution; // number of grass blades per chunk is resolution squared
    [SerializeField] private float minHeight;
    [SerializeField] private float maxHeight;
    [SerializeField] private Vector2 bendFactor;
    [SerializeField] [Range(0.0f, 1.0f)] private float ambientOcclusion;
    [SerializeField][Range(0.0f, 1.0f)] private float smoothness;
    [SerializeField] private Color mainColour;
    [SerializeField] private Color tipColour;
    [SerializeField] private GameObject grass; // Actual grass prefab is used to get scale/height 
    [SerializeField] private Mesh grassMesh_HighLOD;
    [SerializeField] private Mesh grassMesh_LowLOD;
    [SerializeField] private Material grassMaterial;

    [Header("Wind Params")]
    [SerializeField] private float windAmplitude;
    [SerializeField] private float windFrequency;
    [SerializeField] private float windScale;
    [SerializeField] private Vector2 wind_origin;
    [SerializeField] private Vector2 windDirection;

    // Used for calculating size tex
    [Header("Size Params")]
    [SerializeField] private int pixSize;
    [SerializeField] private Vector2 noise_origin;
    [SerializeField] private float scale;
    [SerializeField] private float amplitude;
    [SerializeField] private float frequency;

    [Header("Shaders/Textures")]
    [SerializeField] private ComputeShader computeShader; // Calculates grass positions
    [SerializeField] private ComputeShader mowUpdateShader; // Adds mowTex to accumMowTex
    [SerializeField] private RenderTexture mowTex; // Texture which stores current position of mower

    [Header("Chunk Params")]
    [SerializeField] private Vector2Int chunkDim; // Number of chunks per axis
    [SerializeField] private bool visualizeChunks;

    [Header("LOD Params")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private float lodThreshold;
    [SerializeField] private bool frustumCulling;

    private ComputeBuffer[] matrixBuffers; // stores buffers which contain grass positions of each chunk

    private Collider groundCollider;
    private Mesh groundMesh;

    // world space min and max of terrain
    private Vector2 min;
    private Vector2 max;
    private Vector2 chunkSize; // Local size of chunks
    private int numChunks; // Total number of chunks
    private AABB chunkAABB; // AABB enclosing chunks, used for frustum culling
    private Frustum mainCamFrustum;

    private MaterialPropertyBlock properties;
    private Mesh grassMesh;
    private float grassHeight;
    private float grassCenter;

    private float heightMapAmplitude;

    private Matrix4x4[] matrices;
    private Texture noiseTex; // heightmap texture
    private Texture sizeTex; // texture used to modulate scale of grass blades
    private Texture windTex;
    private RenderTexture accumMowTex; // Texture showing which grass blades have been mowed

    void Start()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            mainCamFrustum = new Frustum(mainCam);
        }

        // Get grass material and meshes
        if (grassMaterial == null) grassMaterial = grass.GetComponent<MeshRenderer>().sharedMaterial;
        if (grassMesh_HighLOD == null) grassMesh_HighLOD = grass.GetComponent<MeshFilter>().sharedMesh;
        if (grassMesh_LowLOD == null) grassMesh_LowLOD = grass.GetComponent<MeshFilter>().sharedMesh;
        SetGrassMesh(grassMesh_HighLOD);

        // Calculate wind and size textures
        noiseTex = GetComponent<ProceduralMesh>().noiseTex;
        sizeTex = CalcNoise(pixSize, scale, amplitude, frequency, noise_origin);
        windTex = CalcNoise(pixSize, scale, amplitude, frequency, wind_origin);
        grassMaterial.SetTexture("_NoiseTex", noiseTex);
        grassMaterial.SetTexture("_WindTex", windTex);

        // Get min and max of terrain
        groundCollider = GetComponent<Collider>();
        min = new Vector2(groundCollider.bounds.min.x, groundCollider.bounds.min.z);
        max = new Vector2(groundCollider.bounds.max.x, groundCollider.bounds.max.z);

        // Amplitude of terrain heightmap
        heightMapAmplitude = GetComponent<ProceduralMesh>().meshAmplitude;

        // Calculate chunk values
        numChunks = chunkDim.x * chunkDim.y;
        chunkSize = new Vector2(groundCollider.bounds.size.x / (float)chunkDim.x, groundCollider.bounds.size.z / (float)chunkDim.y);

        Vector3 center = Vector3.zero;
        Vector3 extents = new Vector3(chunkSize.x/2.0f, (grassHeight + Mathf.Sqrt(3))/2.0f, chunkSize.y/2.0f);
        chunkAABB = new AABB(center, extents);

        // Calculate grass positions
        // Each chunk has its own buffer which is calculated seperately
        // This only happens once
        matrixBuffers = new ComputeBuffer[numChunks];
        int bufferSize = (resolution + 1) * (resolution + 1);
        for (int x = 0; x < chunkDim.x; x++)
        {
            for (int y = 0; y < chunkDim.y; y++)
            {
                int currentIndex = x * chunkDim.y + y;
                matrixBuffers[currentIndex] = new ComputeBuffer(bufferSize, sizeof(float) * 16, ComputeBufferType.Structured);
                Vector2 localMin = new Vector2(min.x + chunkSize.x * x, min.y + chunkSize.y * y);
                Vector2 localMax = localMin + chunkSize;
                DispatchComputeShader(localMin, localMax, matrixBuffers[currentIndex], bufferSize);
            }
        }
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

        // Get wind, size, and heightmap textures
        noiseTex = GetComponent<ProceduralMesh>().noiseTex;
        sizeTex = CalcNoise(pixSize, scale, amplitude, frequency, noise_origin);
        windTex = CalcNoise(pixSize, windScale, windAmplitude, windFrequency, wind_origin);

        // Amplitude of terrain heightmap
        heightMapAmplitude = GetComponent<ProceduralMesh>().meshAmplitude;

        // Set all grass material parameters
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
        // Update mow texture
        if (mowTex != null)
        {
            UpdateMowTexGPU();
            grassMaterial.SetTexture("_MowTex", accumMowTex);
        }

        // Update camera frustum
        mainCamFrustum = new Frustum(mainCam);

        // Draw grass
        GPUInstantiate_Chunked();
    }

    private void SetGrassMesh(Mesh newGrassMesh)
    {
        grassMesh = newGrassMesh;

        grassHeight = grassMesh.bounds.size.y;
        grassCenter = grassMesh.bounds.center.y;
    }

    // Converts rendertexture to identical texture2D
    // This is unused as textures are updated by compute shader
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

    // Generates blank (all black) texture 2D
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

    // Calculates perlin noise texture
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

    // CPU implementation of getting transformation matrices
    // Unused as matrices are calculated using compute shader
    Matrix4x4[] GetTransformationMatrices()
    {
        groundCollider = GetComponent<Collider>();
        min = new Vector2(groundCollider.bounds.min.x, groundCollider.bounds.min.z);
        max = new Vector2(groundCollider.bounds.max.x, groundCollider.bounds.max.z);

        if (max.x - min.x != max.y - min.y)
        {
            Debug.Log("Ground plane is not square");
        }

        float size = max.x - min.x;

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

    // Uses compute shader to add mowtext to accumMowTex
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

    // Calculates a single buffer of grass positions
    void DispatchComputeShader(Vector2 localMin, Vector2 localMax, ComputeBuffer transformMatrixBuffer, int bufferSize)
    {
        if (transformMatrixBuffer.count != bufferSize)
        {
            transformMatrixBuffer = new ComputeBuffer(bufferSize, sizeof(float) * 16, ComputeBufferType.Structured);
        }
        
        // Set all compute shader parameters
        computeShader.SetBuffer(0, "_TransformMatrices", transformMatrixBuffer);

        computeShader.SetTexture(0, "_NoiseTex", noiseTex);
        computeShader.SetTexture(0, "_SizeTex", sizeTex);

        computeShader.SetVector("_Min", new Vector4(localMin.x, localMin.y, 0, 0));
        computeShader.SetVector("_Max", new Vector4(localMax.x, localMax.y, 0, 0));
        computeShader.SetVector("_GlobalMin", new Vector4(min.x, min.y, 0, 0));
        computeShader.SetVector("_GlobalMax", new Vector4(max.x, max.y, 0, 0));
        computeShader.SetFloat("_YPos", transform.position.y);
        computeShader.SetVector("_Scale", new Vector4(grass.transform.localScale.x, grass.transform.localScale.y, grass.transform.localScale.z, 0));
        computeShader.SetMatrix("_RotationMatrix", Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(90, 45, 0), new Vector3(1, 1, 1)));
        computeShader.SetInt("_Resolution", resolution);
        computeShader.SetFloat("_MinHeight", minHeight);
        computeShader.SetFloat("_MaxHeight", maxHeight);
        computeShader.SetFloat("_GrassHeight", grassHeight);
        computeShader.SetFloat("_GrassCenter", grassCenter);
        computeShader.SetFloat("_HeightMapAmplitude", heightMapAmplitude);

        // Dispatch the shader
        uint threadGroupSizeX;
        uint threadGroupSizeY;
        computeShader.GetKernelThreadGroupSizes(0, out threadGroupSizeX, out threadGroupSizeY, out _);
        int threadGroupsX = Mathf.CeilToInt((resolution + 1) / threadGroupSizeX);
        int threadGroupsY = Mathf.CeilToInt((resolution + 1) / threadGroupSizeY);
        computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

    void GPUInstantiate_Chunked()
    {
        if (grassMesh == null && grassMaterial == null)
        {
            grassMaterial = grass.GetComponent<MeshRenderer>().sharedMaterial;
            grassMesh = grass.GetComponent<MeshFilter>().sharedMesh;
        }

        Matrix4x4[] grassMatrices;
        // One batch is rendered for each chunk
        for (int x = 0; x < chunkDim.x; x++)
        {
            for (int y = 0; y < chunkDim.y; y++)
            {
                Vector2 chunkPos = new Vector2(x, y);
                float distanceToCamera = (mainCam.transform.position - ChunkToWorld(chunkPos)).magnitude;

                // Should the camera be a far enough distance away, a lower poly mesh is used for a chunk's grass
                if (distanceToCamera > lodThreshold)
                {
                    SetGrassMesh(grassMesh_LowLOD);
                }
                else
                {
                    SetGrassMesh(grassMesh_HighLOD);
                }

                chunkAABB.SetCenter(ChunkToWorld(new Vector2(x, y)) + new Vector3(0.0f, chunkAABB.GetExtents().y, 0.0f));

                // Should the chunk fall outside of the camera's view, it will not be rendered
                if (frustumCulling && !mainCamFrustum.AABBTest(chunkAABB)) continue;

                grassMatrices = new Matrix4x4[(resolution + 1) * (resolution + 1)];
                matrixBuffers[x * chunkDim.y + y].GetData(grassMatrices);

                Graphics.DrawMeshInstanced(
                    grassMesh,
                    0,
                    grassMaterial,
                    grassMatrices
                );
            }
        }
    }
    
    private Vector2 WorldToChunk(Vector3 worldPos)
    {
        Vector2 chunkPos = new Vector2(worldPos.x, worldPos.z);
        chunkPos -= new Vector2(transform.position.x, transform.position.z);

        if (chunkPos.x < 0.0f) chunkPos.x -= chunkSize.x;
        if (chunkPos.y < 0.0f) chunkPos.y -= chunkSize.y;

        chunkPos = new Vector2((int)(chunkPos.x / chunkSize.x), (int)(chunkPos.y / chunkSize.y));

        return chunkPos;
    }

    private Vector3 ChunkToWorld(Vector2 chunkPos)
    {
        Vector3 worldPos = new Vector3(chunkPos.x * chunkSize.x, transform.position.y, chunkPos.y * chunkSize.y);
        worldPos.x += transform.position.x + chunkSize.x / 2.0f;
        worldPos.z += transform.position.z + chunkSize.y / 2.0f;
        return worldPos;
    }
}
