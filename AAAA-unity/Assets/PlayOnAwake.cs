using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;

public class PlayOnAwake : MonoBehaviour
{
    // Start is called before the first frame update
    private int delay = 2;
    public bool alwaysPlay = false;

    private void Awake()
    {
        foreach (AudioSource audioSource in GetComponents<AudioSource>())
        {
            audioSource.enabled = false;
        }
    }

    void Start()
    {
        foreach (AudioSource audioSource in GetComponents<AudioSource>())
        {
            audioSource.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (alwaysPlay)
        {
            foreach (AudioSource audioSource in GetComponents<AudioSource>())
            {
                
                if (!audioSource.isPlaying)
                {
                    audioSource.enabled = true;
                    audioSource.Play(); // Somehow simply calling play without toggling enabled off/on does not work
                }
                
            }
        }
        if (!alwaysPlay)
        {
            delay--;
            if (delay < 0)
            {
                foreach (AudioSource audioSource in GetComponents<AudioSource>())
                {
                    audioSource.enabled = true;
                    audioSource.Play(); // Somehow simply calling play without toggling enabled off/on does not work
                }

                // Disable, as this script is now useless
                enabled = false;
            }
        }
    }
}
