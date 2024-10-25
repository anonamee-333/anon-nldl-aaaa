using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Benchmark : MonoBehaviour, IMeasurable
{
    public bool running = false;
    
    public string csvName = "test.csv";
    public int episodes = 20;
    public int seedOffset = 100;
        
    public bool rewriteCSV = false;
    public bool resetAgent = true; // Use false for debugging only!
    public int maxSteps = 1000;
    
    private NewAgent _agent;
    private GameObject _target;
    private ITarget _targetController;

    private float _initialDistance;
    private float _travelledDistance;
    private Vector3 _prevPosition;

    private int _episodeNum = 0;
    private int _episodeSteps = 0;

    private StreamWriter _csvWriter;
    public Scene nextScene;

    private List<IMeasurable> _measurables = new List<IMeasurable>();
    public string LogPath = "logs/";
    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void StartBenchmark()
    {
        
        Debug.Log("Starting Benchmark");
        
        Debug.Log($"Max Steps: {maxSteps}, episodes: {episodes}, seedOffset: {seedOffset}");
        
        Debug.Log($"Logging to path: {LogPath}/{csvName}");
        running = true;
        _agent = FindObjectOfType<NewAgent>();  // This should only find active agents, and there should only be one agent active
        _target = _agent.target;
        _targetController = _target.GetComponent<ITarget>();
        NavMeshAgent navMeshAgent = _target.GetComponentInChildren<NavMeshAgent>();
        if (navMeshAgent) navMeshAgent.speed = 0.01f;  // Easier to compute metrics for stationary target
        _agent.MaxStep = -1;
        _agent.evalMode = true;
        _agent.OnTargetReached += FinishEpisode;
        _episodeNum = -1;
        // _agent.speed /= 10f;
        NextEpisode();
        _measurables = new List<IMeasurable>(FindObjectsOfType<MonoBehaviour>().OfType<IMeasurable>());
        /*
        foreach (var meas in _measurables)
        {
            Debug.Log(meas.GetColumnNames()[0]);
        }
        Debug.Log(_measurables);
        */
        
        if (!rewriteCSV && File.Exists($"{LogPath}/{csvName}"))
        {
            Debug.Log("CSV file already exists. Moving to next scene.");
            LoadNextOrExit();
            return;
        }
        CreateCSV($"{LogPath}/{csvName}");
        
        WriteAllColumns();
        
    }

    void LoadNextOrExit()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        Debug.Log($"Benchmark for scene {currentSceneIndex} completed.");

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"Loading next scene: {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No more scenes to load. Exiting application.");
            #if UNITY_EDITOR
            Debug.Log("Exiting Playmode (Editor)");
            EditorApplication.ExitPlaymode();
            #else
            Debug.Log("Exiting Application");
            Application.Quit();
            #endif
        }
    }
    void NextEpisode()
    {
        _episodeNum++;
        if (episodes == _episodeNum)
        {
            LoadNextOrExit();
        }
        Random.InitState(_episodeNum + seedOffset);
        _episodeSteps = 0;
        _travelledDistance = 0;
        _initialDistance = -1;
        _prevPosition = _agent.transform.position;
        if (resetAgent) _agent.TeleportToRandomPosition();
        _targetController.ResetTarget();
    }

    void FinishEpisode()
    {
        Debug.Log($"Episode: {_episodeNum+1}/{episodes}, Travelled: {_travelledDistance}, Shortest: {_initialDistance}, " +
                  $"reward: {_agent.GetCumulativeReward()}," +
                  $"Distance ratio: {_initialDistance/_travelledDistance}, Steps: {_episodeSteps}");
        _agent.SetReward(0f);
        _agent.EndEpisode();
        NextEpisode();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!running) return;
        if (_initialDistance < 0)
        {
            _initialDistance = _agent.navMeshSensor.GetRemainingDistance(_target.transform.position);
            // Debug.Log("Updated episode initial distance to " + _initialDistance);
        }

        Vector3 curPosition = _agent.transform.position;
        _travelledDistance += Vector3.Distance(_prevPosition, curPosition);
        _prevPosition = curPosition;
        _episodeSteps++;
        WriteAllValues();
        // Debug.Log(_agent.GetCumulativeReward());
        if (_episodeSteps > maxSteps) FinishEpisode();
    }

    void CreateCSV(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        _csvWriter = new StreamWriter(path, false);
    }

    void CloseCSV()
    {
        if (_csvWriter != null) _csvWriter.Close();
    }
    
    void WriteToCSV(string data)
    {
        if (_csvWriter != null) _csvWriter.Write(data + ";");
    }

    void AddNewlineToCSV()
    {
        if (_csvWriter != null) _csvWriter.Write(_csvWriter.NewLine);
    }

    void WriteAllColumns()
    {
        foreach (var meas in _measurables)
        {
            foreach (var columnName in meas.GetColumnNames())
            {
                // Debug.Log(columnName);
                WriteToCSV(columnName);
            }
        }

        AddNewlineToCSV();
    }
    
    void WriteAllValues()
    {
        foreach (var meas in _measurables)
        {
            foreach (var columnValue in meas.GetValues())
            {
                // Debug.Log(columnValue);
                WriteToCSV(columnValue);
            }
        }
        AddNewlineToCSV();
    }

    private void OnDestroy()
    {
        CloseCSV();
    }

    public List<string> GetColumnNames()
    {
        return new List<string>{"Episode", "Frame", "Timestamp", 
            "OutOfTime", "SceneName", "MaxSteps"};
    }

    public List<string> GetValues()
    {
        int terminated = _episodeSteps >= maxSteps ? 1 : 0;
        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        return new List<string>{_episodeNum.ToString(), _episodeSteps.ToString(), Time.time.ToString(), 
            terminated.ToString(), sceneName, maxSteps.ToString()};
    }

}
