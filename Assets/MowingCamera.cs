using UnityEngine;

public class MowingCamera : MonoBehaviour
{
    [SerializeField] private GameObject ground;
    private Camera mowCam;
    private Bounds groundBounds;

    void Start()
    {
        if (ground == null)
        {
            ground = GameObject.FindGameObjectsWithTag("Ground")[0];
        }
        groundBounds = ground.GetComponent<Collider>().bounds;

        mowCam = GetComponent<Camera>();
        Matrix4x4 viewMatrix = mowCam.worldToCameraMatrix;

        transform.position = new Vector3(groundBounds.center.x, transform.position.y, groundBounds.center.z);

        Vector4 min = new Vector4(groundBounds.min.x, groundBounds.min.y, groundBounds.min.z, 1.0f);
        Vector4 max = new Vector4(groundBounds.max.x, groundBounds.max.y, groundBounds.max.z, 1.0f);
        mowCam.farClipPlane = -(viewMatrix * min).z;
        mowCam.nearClipPlane = -(viewMatrix * (max + new Vector4(0.0f, 5.0f, 0.0f, 0.0f))).z;
    }
}
