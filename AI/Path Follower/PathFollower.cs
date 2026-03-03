using System;
using UnityEngine;

public class PathFollower : MonoBehaviour
{
    [SerializeField] float patrollingSpeed;
    [SerializeField] Path path;
    [SerializeField] CycleMode cycleMode = CycleMode.None;
    [SerializeField] bool reversePath = false;
    [SerializeField] float dwellingTime = 1f;
    [SerializeField] int startWaypoint = 1;
    [SerializeField] bool[] stopAtPoints;

    EnemyMovement movement;
    int currentWaypointNumber = 1;
    int endWaypointNumber;

    bool canFollowPath = true;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
    }

    private void Start()
    {
        if (path != null)
        {
            SetStartWaypoint(startWaypoint);
            currentWaypointNumber = startWaypoint;
            movement.SetDestination(GetCurrentWaypoint());
        }
    }


    void Update()
    {
        if (path != null && canFollowPath)
        {
            if (cycleMode == CycleMode.None && ReachedEndOfPath()) return;
            FollowPath();
        }
    }

    public void SetPath(Path path, int startWaypointNumber)
    {
        this.path = path;
        SetStartWaypoint(startWaypointNumber);
        currentWaypointNumber = startWaypointNumber;
    }

    public bool ReachedEndOfPath()
    {
        return currentWaypointNumber == endWaypointNumber;
    }

    public Path GetCurrentPath()
    {
        return path;
    }

    public void CanFollowPath(bool enable)
    {
        if (canFollowPath && !enable)
        {
            movement.SetDestination(transform.position);
        }
        else if (!canFollowPath && enable && path != null)
        {
            movement.SetDestination(GetCurrentWaypoint());
        }

        canFollowPath = enable;
    }

    private void SetStartWaypoint(int number)
    {
        startWaypoint = number;
        if (reversePath)
        {
            endWaypointNumber = (startWaypoint + 1) % path.GetNumberOfWaypoints();
            if (endWaypointNumber == 0) endWaypointNumber = path.GetNumberOfWaypoints();
        }
        else
        {
            if (startWaypoint == 1) endWaypointNumber = path.GetNumberOfWaypoints();
            else endWaypointNumber = startWaypoint - 1;
        }
    }

    private void FollowPath()
    {
        movement.SetSpeed(patrollingSpeed);
        if (movement.AtDestination())
        {
            if (IsStopPoint() && movement.GetTimeSinceReachedDestination() < dwellingTime)
            {
                return;
            }
            else
            {
                CycleWaypoint();
                movement.SetDestination(GetCurrentWaypoint());
            }
        }

    }

    private bool IsStopPoint()
    {
        if (stopAtPoints.Length < 1) return false;
        return stopAtPoints[currentWaypointNumber - 1];
    }

    private void CycleWaypoint()
    {
        currentWaypointNumber = reversePath ? currentWaypointNumber - 1 : currentWaypointNumber + 1;
        int totalNumberOfWaypoints = path.GetNumberOfWaypoints();

        switch (cycleMode)
        {
            case CycleMode.PingPong:
                if (currentWaypointNumber < 1)
                {
                    reversePath = !reversePath;
                    currentWaypointNumber = 2;
                }
                else if (totalNumberOfWaypoints < currentWaypointNumber)
                {
                    reversePath = !reversePath;
                    currentWaypointNumber = totalNumberOfWaypoints - 1;
                }
                break;
            case CycleMode.Loop:
                if (currentWaypointNumber < 1) currentWaypointNumber = path.GetNumberOfWaypoints();
                else if (totalNumberOfWaypoints < currentWaypointNumber) currentWaypointNumber = 1;
                break;

        }
    }

    private Vector3 GetCurrentWaypoint()
    {
        return path.GetWaypoint(currentWaypointNumber);
    }
}




public enum CycleMode
{
    None,
    Loop,
    PingPong
}





