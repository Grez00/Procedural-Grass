using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private GameObject player;

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectsWithTag("Player")[0];
    }

    void Update()
    {
        transform.position = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);
    }
}
