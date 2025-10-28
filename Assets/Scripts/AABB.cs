using UnityEngine;

public class AABB
{
    public Vector3 center;
    public Vector3 extents;

    public AABB(Vector3 pCenter, Vector3 pExtents)
    {
        center = pCenter;
        extents = pExtents;
    }

    public Vector3 ClosestPoint(Vector3 p)
    {
        Vector3 d = center - p;

        for (int i = 0; i < 3; i++)
        {
            if (d[i] > extents[i]) p[i] = center[i] + extents[i];
            if (d[i] < -extents[i]) p[i] = center[i] - extents[i];
        }

        return p;
    }

    public bool IsColliding(Vector3 p)
    {
        Vector3 d = center - p;

        for (int i = 0; i < 3; i++)
        {
            if (d[i] > extents[i] || d[i] < -extents[i]) return false;
        }
        return true;
    }

    public void Draw()
    {
        Gizmos.DrawWireCube(center, extents * 2.0f);
    }

    public Vector3 GetExtents()
    {
        return new Vector3(extents.x, extents.y, extents.z);
    }

    public void SetCenter(Vector3 pCenter)
    {
        center = pCenter;
    }

}
