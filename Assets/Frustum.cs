using UnityEngine;

public class Plane
{
    Vector3 p;
    Vector3 normal;

    public float SignedDistanceToPlane(Vector3 p)
    {
        return 0.0f;
    }
}

public class Frustum
{
    // bottom, top, left, right, near, far
    Plane[] planes;

    Frustum(Camera cam)
    {
        
    }

    public bool PointTest(Vector3 p)
    {
        return false;
    }

    public bool SphereTest(Vector3 center, float radius)
    {
        return false;
    }

    public bool AABBTest(AABB a)
    {
        return false;
    }
}
