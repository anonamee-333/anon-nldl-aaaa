using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class NodeVisualizer : MonoBehaviour
{
    public int textureSize = 11; // Adjust as needed
    public NodeScript[] nodes; // Reference to your NodeScript objects

    public Texture2D weightTexture;
    // public MeshRenderer displayPlane;
    
    Vector3 minCoords = Vector3.positiveInfinity;
    Vector3 maxCoords = Vector3.negativeInfinity;

    void Start()
    {
        var vis = FindObjectOfType<NodeCreator>();
        if (vis)
            textureSize = vis.numNodes;
        nodes = FindObjectsOfType<NodeScript>();
        
        foreach (var node in nodes)
        {
            var pos = node.transform.position;
            minCoords = Vector3.Min(minCoords, pos);
            maxCoords = Vector3.Max(maxCoords, pos);
        }
        
        // Create a new texture
        weightTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBAFloat, false);
        weightTexture.filterMode = FilterMode.Point;
        weightTexture.wrapMode = TextureWrapMode.Clamp;
        
        // if (!displayPlane)
        //     displayPlane = GetComponentInChildren<MeshRenderer>();
        // if (displayPlane)
        //     displayPlane.material.mainTexture = weightTexture;
        
        
        // Initialize the texture with weights
        UpdateWeightTexture();

        // Apply the texture to a material or UI element
        GetComponent<Renderer>().material.mainTexture = weightTexture;
    }

    private void Update()
    {
        UpdateWeightTexture();
        GetComponent<Renderer>().material.mainTexture = weightTexture;
    }

    void UpdateWeightTexture()
    {
        foreach (var node in nodes)
        {
            // Map weight to grayscale value (0 to 1)
            float normalizedWeight = Mathf.Clamp01(node.Weight / 100);
            // normalizedWeight = Random.Range(0, 100);

            // Set pixel color based on weight
            // Color pixelColor = new Color(normalizedWeight, Random.Range(0, 100), Random.Range(0, 100), 1f);
            Color pixelColor = new Color(normalizedWeight, 0.1f, 0.1f, 1f);

            // Calculate texture coordinates
            var pos = node.transform.position;
            pos -= minCoords;
            Vector3 scaleCoords = maxCoords - minCoords;
            pos.x /= scaleCoords.x;
            pos.z /= scaleCoords.z;
            pos.y /= scaleCoords.y;
            int x = Mathf.FloorToInt(pos.x * textureSize);
            int y = Mathf.FloorToInt(pos.z * textureSize);

            // Set pixel color in the texture
            weightTexture.SetPixel(x, y, pixelColor);
            // weightTexture.SetPixel(0, 0, Color.red);
            // weightTexture.SetPixel(1, 1, Color.blue);
            // weightTexture.SetPixel(4, 4, Color.green);
        }

        // Apply changes to the texture
        weightTexture.Apply();
    }
}