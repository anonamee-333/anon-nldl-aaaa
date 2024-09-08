using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioTrigger : MonoBehaviour
{
    public float maxLingerTimer = 4f;
    private float _timer = 0;
    private AudioSource _audioSource;
    
    private void OnTriggerStay(Collider other)
    {
        _timer = maxLingerTimer;
    }

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_audioSource) return;
        if (_timer > 0)
        {
            if (!_audioSource.isPlaying) _audioSource.Play();
            _timer -= Time.deltaTime;
        }
        else
        {
            if (_audioSource.isPlaying) _audioSource.Stop();
        }
    }
}
