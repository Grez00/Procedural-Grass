using UnityEngine;

public class MowingCamera : MonoBehaviour
{
    [SerializeField] private GameObject ground;
    private Camera mowCam;
    private Bounds groundBounds;

    void OnEnable()
    {
        if (ground == null)
        {
            ground = GameObject.FindGameObjectsWithTag("Ground")[0];
        }
        groundBounds = ground.GetComponent<Collider>().bounds;

        mowCam = GetComponent<Camera>();

        if (mowCam.orthographic)
        {
            mowCam.orthographicSize = groundBounds.extents.x;
            transform.position = new Vector3(groundBounds.center.x, groundBounds.center.y + 5.0f, groundBounds.center.z);  
        }
        else
        {
            float distance = groundBounds.extents.x / Mathf.Tan((mowCam.fieldOfView / 2.0f) * Mathf.Deg2Rad);
            transform.position = new Vector3(groundBounds.center.x, groundBounds.center.y + distance, groundBounds.center.z);  
        }

        Matrix4x4 viewMatrix = mowCam.worldToCameraMatrix;

        Vector4 min = new Vector4(groundBounds.min.x, groundBounds.min.y, groundBounds.min.z, 1.0f);
        Vector4 max = new Vector4(groundBounds.max.x, groundBounds.max.y, groundBounds.max.z, 1.0f);
        mowCam.farClipPlane = -(viewMatrix * min).z;
        mowCam.nearClipPlane = -(viewMatrix * (max + new Vector4(0.0f, 5.0f, 0.0f, 0.0f))).z;
    }
}
