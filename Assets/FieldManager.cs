using UnityEngine;
using System.Collections.Generic;

public class FieldManager : MonoBehaviour
{
    [SerializeField] private Vector2 chunkSize;
    [SerializeField] private Vector2 chunkOrigin;
    [SerializeField] private int drawDistance;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject ground;

    private List<GameObject> currentChunks;
    private ProceduralMesh groundPlane;
    private Vector2 prevChunkPos;

    void Start()
    {
        prevChunkPos = Vector2.zero;
        currentChunks = new List<GameObject>();
        groundPlane = GetComponentInChildren<ProceduralMesh>();
        if (player == null) player = GameObject.FindGameObjectsWithTag("Player")[0];

        groundPlane.width = (int)chunkSize.x * (drawDistance * 2 + 1);
        groundPlane.height = (int)chunkSize.y * (drawDistance * 2 + 1);

        SetGroundPosToPlayer();

        groundPlane.Init();
    }

    void Update()
    {
        Vector2 playerChunkPos = WorldToChunk(player.transform.position);
        if (Vector2.Distance(playerChunkPos, prevChunkPos) != 0.0f)
        {
            SetGroundPosToPlayer();
            Vector2 newOrigin = WorldToChunk(player.transform.position);
            newOrigin -= new Vector2(drawDistance, drawDistance);
            groundPlane.SetOrigin(newOrigin);
            groundPlane.CalcNoise();
        }
    }

    private void SetGroundPosToPlayer()
    {
        Vector3 groundPos = ChunkToWorld(WorldToChunk(player.transform.position));
        groundPos -= new Vector3(chunkSize.x * drawDistance, 0.0f, chunkSize.y * drawDistance);
        groundPos.y = groundPlane.transform.position.y;
        groundPlane.transform.position = groundPos;
    }

    private Vector2 WorldToChunk(Vector3 worldPos)
    {
        Vector2 chunkPos = new Vector2(worldPos.x, worldPos.z);
        chunkPos -= chunkOrigin;

        if (chunkPos.x < 0.0f) chunkPos.x -= chunkSize.x;
        if (chunkPos.y < 0.0f) chunkPos.y -= chunkSize.y;

        chunkPos = new Vector2((int)(chunkPos.x / chunkSize.x), (int)(chunkPos.y / chunkSize.y));

        return chunkPos;
    }

    private Vector3 ChunkToWorld(Vector2 chunkPos)
    {
        Vector3 worldPos = new Vector3(chunkPos.x * chunkSize.x, transform.position.y, chunkPos.y * chunkSize.y);
        worldPos.x += chunkOrigin.x;
        worldPos.z += chunkOrigin.y;
        return worldPos;
    }

    private void DrawChunk(Vector2 chunkPos)
    {
        Vector3 spawnPos = ChunkToWorld(chunkPos);
        GameObject newChunk = Instantiate(ground, spawnPos, Quaternion.identity);
        newChunk.GetComponent<ProceduralMesh>().SetOrigin(chunkPos);
        currentChunks.Add(newChunk);
    }

    private void ClearChunks() {
        foreach (GameObject chunk in currentChunks)
        {
            GameObject.Destroy(chunk);
        }
        currentChunks.Clear();
    }

    private void UpdateChunks()
    {
        ClearChunks();

        Vector2 playerChunkPos = WorldToChunk(player.transform.position);
        for (int x = -drawDistance; x < drawDistance + 1; x++)
        {
            for (int y = -drawDistance; y < drawDistance + 1; y++)
            {
                DrawChunk(new Vector2(x, y) + playerChunkPos);
            }
        }
    }
}
