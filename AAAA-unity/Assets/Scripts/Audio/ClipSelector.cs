using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


public class ClipSelector : MonoBehaviour
{
    public float minMovement = 0.1f;
    public bool alwaysOn;
    private Vector3 prevPos;
    private AudioSource audioSource;
    public List<AudioClip> clips = new List<AudioClip>();


    // Start is called before the first frame update
    void Start()
    {
        prevPos = transform.position;
        audioSource = GetComponent<AudioSource>();
    }

    private void Reset()
    {
        FindAudioClips();
    }

    void FindAudioClips()
    {
#if UNITY_EDITOR
        Debug.Log("Searching for clips");
        string[] results = AssetDatabase.FindAssets("push_object");
        foreach (var result in results)
        {
            string path = AssetDatabase.GUIDToAssetPath(result);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip) clips.Add(clip);
        }
#endif
    }

    void ChooseClip()
    {
        int i = Random.Range(0, clips.Count);
        audioSource.clip = clips[i];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 currentPos = transform.position;
        float movementDelta = Mathf.Abs(Vector3.Distance(currentPos, prevPos));
        
        if (alwaysOn || movementDelta > minMovement)
        {
            //Debug.Log(movementDelta);
            prevPos = currentPos;
            if (!audioSource.isPlaying)
            {
                ChooseClip();
                audioSource.Play();
            }
        }

        
    }

}
