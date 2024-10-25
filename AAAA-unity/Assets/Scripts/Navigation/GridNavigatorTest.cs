using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GridNavigatorTest : MonoBehaviour
{
    private NodeScript _currentNode;
    private NavMeshAgent _navMeshAgent;
    private GameObject _debugTarget;

    private bool _pursuitMode = true;
    
    // Start is called before the first frame update
    void Start()
    {
        _currentNode = FindClosestNode();
        _debugTarget = FindObjectOfType<NavTarget>().gameObject;
        _navMeshAgent = GetComponent<NavMeshAgent>();
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

    bool DebugLineOfSight()
    {
        bool spotted = false;
        Vector3 direction = _debugTarget.transform.position - transform.position;
        float distance = direction.magnitude;
        var layer = _currentNode.obstacleLayer;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, distance, layer))
        {
            // Collision, there is probably a wall between us and this node
            // Debug.DrawRay(transform.position, direction, Color.yellow, 3f);
            spotted = false;
        }
        else
        {
            spotted = true;
            Debug.DrawRay(transform.position, direction, Color.blue, 3f);
        }
        return spotted;
    }

    private void ResetEpisode()
    {
        foreach (var node in FindObjectsOfType<NodeScript>())
        {
            node.Weight = 20;
        } 
        _debugTarget.GetComponent<ITarget>().ResetTarget();
    }

    // Update is called once per frame
    void Update()
    {
        // Update weights based on belief
        Vector3 direction = _debugTarget.transform.position - transform.position;
        var (nodeInDirection, dotProductValue) = _currentNode.GetClosestNeighborToDirection(direction, transform.position);
        List<NodeScript> excludedNodes = new List<NodeScript>();
        excludedNodes.Add(_currentNode);
        excludedNodes.AddRange(_currentNode.neighbors);
        nodeInDirection.DistributeWeight(dotProductValue, excludedNodes);
        Debug.Log(dotProductValue);
        
        //
        bool lineOfSight = DebugLineOfSight();
        if (_pursuitMode != lineOfSight)
        {
            // Find closest node again, as we may have travelled far from previous node
            _currentNode = FindClosestNode();
            _pursuitMode = lineOfSight;
        }
        if (!_pursuitMode)
        {
            // Agent patrols the are without knowledge where the target is
            
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
                _currentNode = _currentNode.GetHighestWeightNeighbor();
                Debug.Log("Switching to next node");
            }
        }
        else
        {
            // Agent has spotted the target and is in pursuit
            _navMeshAgent.SetDestination(_debugTarget.transform.position);
            _currentNode = FindClosestNode();
            if (Vector3.Distance(transform.position, _debugTarget.transform.position) < 2f)
                ResetEpisode();
        }
        
    }
}
