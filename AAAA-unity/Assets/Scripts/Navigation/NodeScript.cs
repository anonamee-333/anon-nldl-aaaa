using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class NodeScript : MonoBehaviour
{
    public float Weight = 20;
    public float MaxNeighbourDistance = 30;

    private float _decaySpeed = 0.1f;
    private float _redistributeFactor = 0.5f;

    // Neighbors (connected nodes)
    public List<NodeScript> neighbors;

    // Layer mask for raycasting (adjust as needed)
    public LayerMask obstacleLayer;

    void Start()
    {
        Weight = 0f; // Initialize weight

        // Find neighbors during initialization
        FindNeighbors();
    }

    public void DistributeWeight(float val, List<NodeScript> excluded)
    {
        // Add weight to this node and neighboring nodes
        // (i.e., the noise is coming from this node or its neighbours)
        IncreaseWeight(val);
        foreach (NodeScript node in neighbors)
        {
            if (!excluded.Contains(node))
                node.IncreaseWeight(_redistributeFactor * val);
        }
    }

    public void IncreaseWeight(float val)
    {
        if (Weight < 100)
        Weight += val;
    }

    public void DecreaseWeight(float val)
    {
        if (Weight > 0)
            Weight -= val;
    }

    private void Update()
    {
        // Decay weight
        if (Weight < 20)
        {
            IncreaseWeight(_decaySpeed);
        }
        else
        {
            DecreaseWeight(_decaySpeed);
        }
    }

    void FindNeighbors()
    {
        foreach (var node in FindObjectsOfType<NodeScript>())
        {
            // Raycast from this node to the node
            Vector3 direction = node.transform.position - transform.position;
            float distance = direction.magnitude;

            if (distance > MaxNeighbourDistance)
                continue;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, distance, obstacleLayer))
            {
                // Occlusions, node is not visible from here
                Debug.DrawRay(transform.position, direction, Color.red, 1f);
            }
            else
            {
                // No occlusions
                AddNeighbor(node);
                Debug.DrawRay(transform.position, direction, Color.green, 5f);
            }
        }
    }

    // Example method to add a neighbor
    public void AddNeighbor(NodeScript neighbor)
    {
        // Ensure neighbor is not null and not already in the list
        if (neighbor != null && !neighbors.Contains(neighbor) && neighbor != this)
        {
            // Add neighbor to the list
            neighbors.Add(neighbor);
        }
    }

    // Example method to find the neighbor with the highest weight
    public NodeScript GetHighestWeightNeighbor()
    {
        NodeScript highestWeightNeighbor = null;
        float highestWeight = float.MinValue;

        foreach (var neighbor in neighbors)
        {
            if (neighbor.Weight > highestWeight)
            {
                highestWeight = neighbor.Weight;
                highestWeightNeighbor = neighbor;
            }
        }

        return highestWeightNeighbor;
    }
    
    public NodeScript GetHighestWeightNeighbor2(int maxDepth=2)
    {
        // Get highest weight neighbour, but also consider neighbours of neighbours
        NodeScript highestWeightNeighbor = null;
        float highestWeight = float.MinValue;

        foreach (var neighbor in neighbors)
        {
            float cumulativeWeight = neighbor.Weight;
            foreach (var neighbor2 in neighbor.neighbors)
            {
                if (!neighbors.Contains(neighbor2))  // Avoid circular additions
                {
                    cumulativeWeight += neighbor2.Weight;
                }
            }
            if (cumulativeWeight > highestWeight)
            {
                highestWeight = cumulativeWeight;
                highestWeightNeighbor = neighbor;
            }
        }

        return highestWeightNeighbor;
    }
    
    public (NodeScript, float) GetClosestNeighborToDirection(Vector3 direction, Vector3 position)
    {
        // Returns the best matching neighbour and its dot product (wrt given direction)
        
        NodeScript closestNeighbor = null;
        float maxDotProduct = float.MinValue;
        direction = direction.normalized;

        foreach (var neighbor in neighbors)
        {
            Vector3 neighborToNode = neighbor.transform.position - position;
            neighborToNode.Normalize();

            // Calculate dot product
            float dotProduct = Vector3.Dot(direction, neighborToNode);

            if (dotProduct > maxDotProduct)
            {
                maxDotProduct = dotProduct;
                closestNeighbor = neighbor;
            }
        }

        return (closestNeighbor, maxDotProduct);
    }
    
    public List<Tuple<NodeScript, float>> GetAllNeighbourDotProducts(Vector3 direction, Vector3 position)
    {
        // Returns dot products to all neighbours (wrt given direction)
        
        List<Tuple<NodeScript, float>> results = new List<Tuple<NodeScript, float>>();
        direction = direction.normalized;

        foreach (var neighbor in neighbors)
        {
            Vector3 neighborToNode = neighbor.transform.position - position;
            neighborToNode.Normalize();

            // Calculate dot product
            float dotProduct = Vector3.Dot(direction, neighborToNode);
            
            // Add to results
            results.Add(new Tuple<NodeScript, float>(neighbor, dotProduct));
        }

        return results;
    }
    
    public static List<NodeScript> GreedySearch(NodeScript startNode, int maxDepth)
    {
        var openList = new List<(NodeScript node, double cumulativeWeight)> { (startNode, 0) };
        var closedList = new HashSet<NodeScript>();

        while (openList.Any())
        {
            var (currentNode, cumulativeWeight) = openList.OrderByDescending(n => n.cumulativeWeight).First();
            openList.Remove((currentNode, cumulativeWeight));

            if (cumulativeWeight >= 100) // Adjust the threshold as needed
            {
                // Found a path with sufficient cumulative weight
                return closedList.ToList();
            }

            if (closedList.Count < maxDepth)
            {
                closedList.Add(currentNode);

                foreach (var neighbor in currentNode.neighbors)
                {
                    if (!closedList.Contains(neighbor))
                    {
                        var neighborCumulativeWeight = cumulativeWeight + neighbor.Weight;
                        openList.Add((neighbor, neighborCumulativeWeight));
                    }
                }
            }
        }

        // No path found
        return null;
    }

    // Replace with your own goal condition
    private static bool IsGoalNode(NodeScript node)
    {
        // Example: Check if the node meets your goal criteria
        return node.Weight >= 100; // Adjust as needed
    }
}
