using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


public class Footsteps : MonoBehaviour, IMeasurable
{

    public bool alwaysOn;
    private Vector3 prevPos;
    private AudioSource audioSource;
    private int clipIndex = 0;
    public List<AudioClip> walkingClips = new List<AudioClip>();
    public List<AudioClip> runningClips = new List<AudioClip>();
    
    
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
        string[] results = AssetDatabase.FindAssets("Footsteps_Tile_Walk");
        foreach (var result in results)
        {
            string path = AssetDatabase.GUIDToAssetPath(result);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip) walkingClips.Add(clip);
        }
        results = AssetDatabase.FindAssets("Footsteps_Tile_Run");
        foreach (var result in results)
        {
            string path = AssetDatabase.GUIDToAssetPath(result);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip) runningClips.Add(clip);
        }
#endif
    }

    void ChooseClip()
    {
        List<AudioClip> clips;
        clips = walkingClips;
        
        int i = Random.Range(0, clips.Count);
        clipIndex = i;
        audioSource.clip = clips[i];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 currentPos = transform.position;
        float movementDelta = Mathf.Abs(Vector3.Distance(currentPos, prevPos));
        
        if (alwaysOn || movementDelta > 0.1f)
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
    
    public List<string> GetColumnNames()
    {
        return new List<string>{"ClipName", "ClipIndex", "ClipDuration", "ClipPosition"};
    }

    public List<string> GetValues()
    {
        var clip = audioSource.clip;
        return new List<string>{clip.name, clipIndex.ToString(), clip.length.ToString(), audioSource.time.ToString()};
    }

}
