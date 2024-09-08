using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class NavMeshSensor : MonoBehaviour, IMeasurable
{
    // private GameObject _child;
    // private NavMeshAgent _navMeshAgent;
    private NavMeshPath _path;
    
    // Start is called before the first frame update
    void Start()
    {
        _path = new NavMeshPath();
    }
    
    public Vector3 GetRandomPoint(Vector3 center, float maxDistance) {
        // Get Random Point inside Sphere which position is center, radius is maxDistance
        Vector3 randomPos = Random.insideUnitSphere * maxDistance + center;

        NavMeshHit hit; // NavMesh Sampling Info Container

        // from randomPos find a nearest point on NavMesh surface in range of maxDistance
        NavMesh.SamplePosition(randomPos, out hit, maxDistance, NavMesh.AllAreas);

        return hit.position;
    }
    
    public float GetRemainingDistance(Vector3 targetPos)
    {
        NavMesh.CalculatePath(transform.position, targetPos, NavMesh.AllAreas, _path);
        float distance = GetPathRemainingDistance(_path);
        return distance;
    }
    
    public Vector3 GetNextDirection(Vector3 targetPos)
    {
        NavMesh.CalculatePath(transform.position, targetPos, NavMesh.AllAreas, _path);
        if (_path.corners.Length < 2)
            return new Vector3(0,100,0);
        var direction = _path.corners[1] - _path.corners[0]; // - transform.position;
        return direction;
    }

    private float GetPathRemainingDistance(NavMeshPath path)
    {
        // Code adapted from Kim / Stackoverflow
        if (path.corners.Length == 0)
            return -1f;
        float distance = 0.0f;
        for (int i = 0; i < path.corners.Length - 1; ++i)
        {
            distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return distance;
    }

    public List<string> GetColumnNames()
    {
        return new List<string>{"PathLength", "PathCorners"};
    }

    public List<string> GetValues()
    {
        var path = _path;
        return new List<string>{GetPathRemainingDistance(path).ToString(), path.corners.Length.ToString()};
    }
}
