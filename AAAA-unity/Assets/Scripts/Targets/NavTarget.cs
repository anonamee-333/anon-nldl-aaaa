using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavTarget : MonoBehaviour, ITarget
{
    public GameObject spawnArea;
    public float maxX = 0.95f;
    public float maxZ = 0.95f;
    public float maxDistance = 100f;
    public float changeTargetThreshold = 1f;
    private float originalY;
    private Rigidbody rbody;
    private bool initialized = false;
    
    private UnityEngine.AI.NavMeshAgent agent;
    public Vector3 GetRandomPoint(Vector3 center, float maxDistance) {
        Vector3 randomPos = Random.insideUnitSphere * maxDistance + center;
        UnityEngine.AI.NavMeshHit hit;
        UnityEngine.AI.NavMesh.SamplePosition(randomPos, out hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas);
        return hit.position;
    }

    public void ResetTarget()
    {
        RandomizePosition();
        
        // Make the agent move towards a random destination
        RandomizeDestination();
    }

    void RandomizeDestination()
    {
        if (initialized && agent && agent.isActiveAndEnabled)
        {
            agent.destination = GetRandomPoint(transform.position, maxDistance);
        }
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    void Init()
    {
        if (initialized) return;
        initialized = true;
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        RandomizeDestination();
        originalY = transform.position.y;
        rbody = GetComponent<Rigidbody>();
    }

    void Start() {
        Init();
    }

    void Update() {
        
        if (initialized && agent.isActiveAndEnabled && !agent.pathPending && agent.remainingDistance < changeTargetThreshold)
        {
            RandomizeDestination();
        }
        

        if (rbody)
        {
            rbody.velocity = Vector3.zero;
            rbody.angularVelocity = Vector3.zero;
        }
    }
    
    void RandomizePosition()
    {
        // Compute random position within bounds
        float x = Random.Range(-maxX, maxX);
        float z = Random.Range(-maxZ, maxZ);
        var scale = spawnArea.transform.lossyScale * 5;  // TODO: Get scale in more generalizable way
        Vector3 offset = new Vector3(x, 0f, z);
        offset.Scale(scale);
        Vector3 newPos = spawnArea.transform.position + offset;  // TODO: Account for rotation
        newPos.y = originalY;
        transform.position = newPos;
        
        // Use NavMesh to make sure the new position is within bounds and not inside obstacles
        UnityEngine.AI.NavMeshHit hit;
        UnityEngine.AI.NavMesh.SamplePosition(newPos, out hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas);
        transform.position = hit.position;
    }

    public List<string> GetColumnNames()
    {
        return new List<string>{"TargetPositionX", "TargetPositionY", "TargetPositionZ"};
    }

    public List<string> GetValues()
    {
        return new List<string>{transform.position.x.ToString(), transform.position.y.ToString(), transform.position.z.ToString()};
    }
}
