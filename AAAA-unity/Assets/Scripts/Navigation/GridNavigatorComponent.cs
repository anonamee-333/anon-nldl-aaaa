using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GridNavigatorComponent : MonoBehaviour
{
    public NodeScript _currentNode;
    public NavMeshAgent _navMeshAgent;
    private GameObject _target;

    private bool _pursuitMode = true;
    
    // Start is called before the first frame update
    void Start()
    {
        _currentNode = FindClosestNode();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (!_navMeshAgent)
            _navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
    }

    void Reset()
    {
        _currentNode = FindClosestNode();
    }

    public void SetTarget(GameObject obj)
    {
        _target = obj;
    }

    NodeScript FindClosestNode()
    {
        List<NodeScript> allNodes = new List<NodeScript>(FindObjectsOfType<NodeScript>());
        allNodes = allNodes.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).ToList();
        foreach (NodeScript node in allNodes)
        {
            Vector3 direction = node.transform.position - transform.position;
            float distance = direction.magnitude;
            var layer = node.obstacleLayer;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, distance, layer))
            {
                // Collision, there is probably a wall between us and this node
                Debug.DrawRay(transform.position, direction, Color.yellow, 3f);
            }
            else
            {
                _currentNode = node;
                Debug.DrawRay(transform.position, direction, Color.blue, 3f);
                return node;
            }
        }
        Debug.LogWarning("Navigator could not find any non-occluded nodes! Setting to closest one that is occluded");
        return allNodes[0];
    }

    public void ResetNodes()
    {
        foreach (var node in FindObjectsOfType<NodeScript>())
        {
            node.Weight = 20;
        } 
    }

    public bool NavMeshLineOfSight(Vector3 targetPos)
    {
        NavMeshHit hit;
        bool lineOfSight = !_navMeshAgent.Raycast(targetPos, out hit);
        return lineOfSight;
    }

    private bool randomDirInitialized = false;
    public void MoveRandom(bool lineOfSight)
    {
        // Moves to some randompoint, except if it has lineofsight
        // With lineofsight, we assume that the agent visually sees the target and navigates towards it
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (lineOfSight && distance < 15f)
        {
            MoveDirect();
            return;
        }
        if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
        {
            randomDirInitialized = false;
        }

        if (!randomDirInitialized)
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * 50;
            randomDirection.y = 0;
            randomDirection += transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, 10, 1))
            {
                Vector3 finalPosition = hit.position;
                _navMeshAgent.SetDestination(finalPosition);
                randomDirInitialized = true;
            }
        }
    }

    public void MoveDirect()
    {
        _navMeshAgent.SetDestination(_target.transform.position);
    }
    
    

    // Update is called once per frame
    public void Move(Vector3 beliefDirection, bool lineOfSight)
    {
        // Debug.Log("MOOOOVE");
        // Update weights based on belief
        // Vector3 direction = _target.transform.position - transform.position;
        var direction = beliefDirection;
        List<NodeScript> excludedNodes = new List<NodeScript>();
        excludedNodes.Add(_currentNode);
        excludedNodes.AddRange(_currentNode.neighbors);
        
        // var (nodeInDirection, dotProductValue) = _currentNode.GetClosestNeighborToDirection(direction, transform.position);
        foreach (Tuple<NodeScript,float> neighbourDotProduct in _currentNode.GetAllNeighbourDotProducts(direction, transform.position))
        {
            // Distribute positive and negative weights depending on the dot product
            NodeScript neighbor = neighbourDotProduct.Item1;
            float dotProduct = neighbourDotProduct.Item2;
            neighbor.DistributeWeight(dotProduct, excludedNodes);
        }
        // nodeInDirection.DistributeWeight(dotProductValue, excludedNodes);
        // Debug.Log(dotProductValue);
        
        // Check line of sight
        // lineOfSight = DebugLineOfSight();
        if (_pursuitMode != lineOfSight)
        {
            // Switch movement mode, as LOS status changed
            // Also, find closest node again, as we may have travelled far from previous node
            _currentNode = FindClosestNode();
            _pursuitMode = lineOfSight;
        }
        if (!_pursuitMode)
        {
            // Agent patrols without line of sight
            
            // Reduce weight for current target, as the target is likely not here
            _currentNode.DecreaseWeight(1f);
            
            // Set nav target
            var distance = Vector3.Distance(transform.position, _currentNode.transform.position);
            if (distance > 2)
            {
                _navMeshAgent.SetDestination(_currentNode.transform.position);
            }
            else
            {
                // _currentNode = _currentNode.GetHighestWeightNeighbor();
                _currentNode = _currentNode.GetHighestWeightNeighbor2();
                //Debug.Log("Switching to next node");
            }
        }
        else
        {
            // Agent has spotted the target and is in pursuit
            _navMeshAgent.SetDestination(transform.position + direction);
            _currentNode = FindClosestNode();
        }
        
    }

    public void SetSpeed(float speed)
    {
        _navMeshAgent.speed = speed;
    }
}
