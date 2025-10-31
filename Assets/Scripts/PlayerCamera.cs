using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private Vector3 offset = new Vector3(0.0f, 20.0f, -20.0f);
    [SerializeField] private float zoomScale;
    private Camera cam;
    private float distToPlayer;
    private Vector3 playerToCamDir;

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectsWithTag("Player")[0];

        cam = GetComponent<Camera>();

        distToPlayer = (transform.position - player.transform.position).magnitude;
        playerToCamDir = Vector3.Normalize(transform.position - player.transform.position);
        if (playerToCamDir == Vector3.zero) playerToCamDir = Vector3.Normalize(offset);
    }

    void Update()
    {
        distToPlayer += Input.mouseScrollDelta.y * zoomScale;

        transform.position = player.transform.position + (playerToCamDir * distToPlayer);
        transform.LookAt(player.transform.position);
    }
}
