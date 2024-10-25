using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsFixer : MonoBehaviour
{
    public int fps = 50;
    
    // Start is called before the first frame update
    void Start()
    {
        /*
        Debug.Log(Application.targetFrameRate);
        Debug.Log(Time.captureFramerate);
        */
        Application.targetFrameRate = fps;
        Time.captureFramerate = fps;
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.targetFrameRate != fps)
        {
            Debug.Log("Wrong target framerate!");
        }
        if (Time.captureFramerate != fps)
        {
            Debug.Log("Wrong capture framerate!");
        }
    }
}
