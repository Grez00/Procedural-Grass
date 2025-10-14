using UnityEngine;

public class MowController : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    private Vector2 inputAxis;

    void Start()
    {
        inputAxis = Vector2.zero;
    }

    void Update()
    {
        inputAxis = Vector2.zero;

        if (Input.GetKey(KeyCode.W))
        {
            inputAxis.y += 1.0f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            inputAxis.x -= 1.0f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            inputAxis.y -= 1.0f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            inputAxis.x += 1.0f;
        }

        transform.position += new Vector3(inputAxis.x, 0.0f, inputAxis.y) * moveSpeed * Time.deltaTime;
    }
}
