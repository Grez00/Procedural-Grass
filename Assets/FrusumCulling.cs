using UnityEngine;

public class FrusumCulling : MonoBehaviour
{
    [SerializeField] bool drawAxes;
    [SerializeField] bool drawNormals;
    [SerializeField] bool drawFrustum;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    Vector3[] GetRectangle(Vector3 center, Vector3 u, Vector3 v)
    {
        Vector3[] points = { center - u + v, center + u + v, center - u - v, center + u - v };
        return points;
    }

    void DrawRectangle(Vector3 center, Vector3 u, Vector3 v)
    {
        Vector3[] points = GetRectangle(center, u, v);

        Gizmos.DrawLine(points[0], points[1]);
        Gizmos.DrawLine(points[2], points[3]);
        Gizmos.DrawLine(points[0], points[2]);
        Gizmos.DrawLine(points[1], points[3]);
    }

    void DrawRectangle(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(a, c);
        Gizmos.DrawLine(b, d);
    }

    void DrawFrustum(Vector3 near_center, Vector3 near_right, Vector3 near_up, Vector3 far_center, Vector3 far_right, Vector3 far_up)
    {
        Vector3[] near = GetRectangle(near_center, near_right, near_up);
        Vector3[] far = GetRectangle(far_center, far_right, far_up);

        DrawRectangle(near[0], near[1], near[2], near[3]);
        DrawRectangle(far[0], far[1], far[2], far[3]);

        Gizmos.DrawLine(near[0], far[0]);
        Gizmos.DrawLine(near[1], far[1]);
        Gizmos.DrawLine(near[2], far[2]);
        Gizmos.DrawLine(near[3], far[3]);
    }

    void OnDrawGizmos()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        Matrix4x4 viewMatrix = cam.cameraToWorldMatrix;

        Vector3 up = Vector3.Normalize(viewMatrix * new Vector3(0, 1, 0));
        Vector3 forward = Vector3.Normalize(viewMatrix * new Vector3(0, 0, -1));
        Vector3 right = Vector3.Cross(up, forward);

        float nearHeight = Mathf.Tan(cam.fieldOfView / 2.0f * Mathf.Deg2Rad) * cam.nearClipPlane * 2.0f;
        float nearWidth = nearHeight * cam.aspect;
        Vector3 nearCenter = cam.transform.position + forward * cam.nearClipPlane;
        Vector3 nearNormal = forward;

        float farHeight = Mathf.Tan(cam.fieldOfView / 2.0f * Mathf.Deg2Rad) * cam.farClipPlane * 2.0f;
        float farWidth = farHeight * cam.aspect;
        Vector3 farCenter = cam.transform.position + forward * cam.farClipPlane;
        Vector3 farNormal = -forward;

        Vector3 farForwardVector = forward * cam.farClipPlane;
        Vector3 leftNormal = Vector3.Normalize(Vector3.Cross(up, farForwardVector + right * farHeight/2.0f));
        Vector3 rightNormal = Vector3.Normalize(Vector3.Cross(farForwardVector - right * farHeight/2.0f, up));
        Vector3 topNormal = Vector3.Normalize(Vector3.Cross(right, farForwardVector - up * farWidth/2.0f));
        Vector3 bottomNormal = Vector3.Normalize(Vector3.Cross(farForwardVector + up * farWidth / 2.0f, right));
        
        if (drawFrustum)
        {
            Gizmos.color = Color.magenta;
            DrawFrustum(
                nearCenter,
                right * nearWidth / 2.0f,
                up * nearHeight / 2.0f,

                farCenter,
                right * farWidth / 2.0f,
                up * farHeight / 2.0f
            );
        }

        if (drawAxes)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + up);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + forward);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + right);
        }
        
        if (drawNormals)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + topNormal);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + rightNormal);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + leftNormal);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + bottomNormal);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + nearNormal);

            Gizmos.color = Color.black;
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + farNormal);
        }
    }
}
