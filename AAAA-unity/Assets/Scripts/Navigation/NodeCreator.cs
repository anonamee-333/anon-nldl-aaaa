using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class NodeCreator : MonoBehaviour
{
    public Vector2 minExtents;
    public Vector2 maxExtents;
    public int numNodes = 10;
    public GameObject prefab;

    private void Awake()
    {
        // Get distance between nodes to figure out how far the nodes should connect to each other
        float distBetweenNodes = (maxExtents.x - minExtents.x) / (numNodes-1);
        float diagDistance = distBetweenNodes * 1.5f;  // diagonal is roughly 1.414 longer than distance to adjacent nodes
        for (int x = 0; x < numNodes; x++)
        {
            for (int y = 0; y < numNodes; y++)
            {
                float posX = minExtents.x + (maxExtents.x - minExtents.x) * x / (numNodes - 1);
                float posY = minExtents.y + (maxExtents.y - minExtents.y) * y / (numNodes - 1);
                if (!prefab) return;
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(new Vector3(posX, 2.5f, posY), out hit, 5f, NavMesh.AllAreas))
                {
                    posX = hit.position.x;
                    posY = hit.position.z;
                }
                else
                {
                    Debug.LogError($"Invalid pos: {posX}, {posY}");
                }
                var obj = Instantiate(prefab);
                // var obj = new GameObject().AddComponent<NodeScript>();
                obj.transform.position = new Vector3(posX, 2.5f, posY);
                obj.transform.parent = transform;
                NodeScript nodeScript = obj.GetComponent<NodeScript>();
                if (nodeScript)
                {
                    nodeScript.MaxNeighbourDistance = diagDistance;
                }
                obj.SetActive(true);
            }
        }
    }
}
