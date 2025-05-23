using UnityEngine;

public class Path : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(GetWaypoint(i + 1), .25f);
            int j = GetNextWaypointIndex(i);
            Gizmos.DrawLine(GetWaypoint(i + 1), GetWaypoint(j + 1));
        }
    }

    private int GetNextWaypointIndex(int i)
    {
        if (i == transform.childCount - 1)
        {
            return 0;
        }
        else
        {
            return i + 1;
        }
    }

    public Vector3 GetWaypoint(int i)
    {
        return transform.GetChild(i - 1).position;
    }

    public int GetNumberOfWaypoints()
    {
        return transform.childCount;
    }
}
