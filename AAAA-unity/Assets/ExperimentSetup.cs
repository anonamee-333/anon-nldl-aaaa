using System;
using System.Collections.Generic;
using System.IO;
using Unity.Barracuda;
using Unity.Barracuda.ONNX;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.AI; // Ensure to include this for NavMeshAgent
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class ExperimentSetup : MonoBehaviour
{
    public enum AgentType
    {
        Hanning,
        Random,
        Rect,
        HanningAO,  // AO = Audio sensor only (no other sensors)
        RectAO  // AO = Audio sensor only (no other sensors)
    }
    
    public AgentType agent = AgentType.Hanning;
    public GameObject hanningAgent;
    public GameObject randomAgent;
    public GameObject rectAgent;
    public GameObject hanningAOAgent;
    public GameObject rectAOAgent;
    public bool enableBenchmark = false;  // Run benchmark instead of training
    public bool enableSmoketest = false;  // Run a very short version of the benchmark for debugging
    public bool forceSaveArgs = false;  // For quick debugging in editor
    public int decisionPeriod = 1;  // How often should the agent make decisions? Should be a fraction of the audio buffer length
    public float losRewardScale = 0.5f;  // Used to be 0.5 on pre-rebuttal experiments, set 0 to disable
    public string benchmarkName = "unnamed";  // Affects the name of the benchmark output csv
    public string modelPath = string.Empty;  // Path to any onnx model that will be used to override the agent model
    public float targetSpeed = 0.0f; // Movement speed of the target. Set 0 to disable movement.
    public int numAudioSources = 1;  // If above 1, duplicates the target to create multiple audio sources for performance testing
    
    
    private GameObject activeAgent;
    
    void Awake()
    {
        if (forceSaveArgs) SaveArgs();
        // Call the ParseArgs method to initialize configurations
        ParseArgs();
        // Instantiate the selected agent based on the agent type
        Debug.Log($"Creating agent of type: {agent}");
        switch (agent)
        {
            case AgentType.Hanning:
                activeAgent = Instantiate(hanningAgent);
                break;
            case AgentType.Random:
                activeAgent = Instantiate(randomAgent);
                break;
            case AgentType.Rect:
                activeAgent = Instantiate(rectAgent);
                break;
            case AgentType.HanningAO:
                activeAgent = Instantiate(hanningAOAgent);
                break;
            case AgentType.RectAO:
                activeAgent = Instantiate(rectAOAgent);
                break;
        }
        // Change reward scale
        Debug.Log($"Setting line-of-sight reward scale to {losRewardScale}.");
        activeAgent.GetComponent<NewAgent>().losScale = losRewardScale;
        
        // Change audio agent decision period
        NewAudioAgent audioAgent = activeAgent.GetComponent<NewAudioAgent>();
        if (audioAgent != null)
        {
            Debug.Log($"Setting decision period to {decisionPeriod}.");
            audioAgent.DecisionInterval = decisionPeriod;
            //audioAgent.ValidateIntervals();  // Prints errors if decision period is not within bounds
        }
        
        // Update NavMeshAgent speed for any NavTarget objects
        NavTarget target = FindObjectOfType<NavTarget>();
        if (target != null)
        {
            NavMeshAgent navAgent = target.GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.speed = targetSpeed;
                Debug.Log($"Target speed set to: {targetSpeed}");
            }
        }
        
        // Make sure the agent is activated
        activeAgent.SetActive(true); 
        
        // Switch model if we are not training
        if (enableBenchmark && modelPath != string.Empty)
        {
            ChangeModel(modelPath);
            
            // Attempt to force agent to update its model
            activeAgent.SetActive(false); 
            activeAgent.SetActive(true); 
        }

        if (numAudioSources > 1)
        {
            for (int i = 1; i < numAudioSources; i++)
            {
                Instantiate(target);
            }
        }
    }
    void Start()
    {
        if (enableBenchmark)
        {
            // Setup benchmark
            var benchmark = FindObjectOfType<Benchmark>();
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            benchmark.csvName = $"{agent}_{benchmarkName}_{sceneName}.csv";
            if (enableSmoketest)
            {
                benchmark.episodes = 5;
                benchmark.maxSteps = 10;
            }
            
            // Start benchmark (Do this in Start() instead of Awake() to make sure other scripts have chance to initialize)
            benchmark.StartBenchmark();
        }
    }
    void ChangeModel(string modelPath)
    {
        // Load new model from disk. 
        //
        // A bit hackish, but ML-agents does not seem to have a native way for doing this.
        Debug.Log($"Attempting to load a new model from {modelPath}...");
        
        // Get current agent components
        BehaviorParameters agent_params = activeAgent.GetComponent<BehaviorParameters>();
        Agent root_agent = activeAgent.GetComponent<Agent>();
        var behavior_name = agent_params.BehaviorName;
        
        // Read file at path and convert to byte array
        Debug.Log($"Converting ONNX to correct format...");
        var converter = new ONNXModelConverter(optimizeModel: true); // requires the Unity.Barracuda.ONNX assembly
        Model model = converter.Convert(modelPath);
        NNModelData modelData = ScriptableObject.CreateInstance<NNModelData>();
        
        // Copy data from the Model to the NNModel
        Debug.Log($"Copying data from the converted ONNX to the agent...");
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            ModelWriter.Save(writer, model);
            modelData.Value = memoryStream.ToArray();
        }
        modelData.name = "Data";
        modelData.hideFlags = HideFlags.HideInHierarchy;
        NNModel nnModel = ScriptableObject.CreateInstance<NNModel>();
        nnModel.modelData = modelData;
        nnModel.name = "ModelFromDisk";
        
        // Change the model (One of these seems to work, but which?)
        // agent_params.Model = model;  // TODO: Does this line have any effect?
        root_agent.SetModel("from_disk", nnModel);  // TODO: Does this line have any effect?
        
        Debug.Log($"Loaded new model from {modelPath}");
    }
    
    void ChangeModel2(string modelPath)
    {
        // Load new model from disk. 
        //
        // A bit hackish, but ML-agents does not seem to have a native way for doing this.
        Debug.Log($"Attempting to load a new model from {this.modelPath}...");
        
        // Get current agent components
        BehaviorParameters agent_params = activeAgent.GetComponent<BehaviorParameters>();
        Agent root_agent = activeAgent.GetComponent<Agent>();
        var behavior_name = agent_params.BehaviorName;
        
        // Get any NNModel (here we use the current model from the agent)
        NNModel model = agent_params.Model;
        // Read file at path and convert to byte array
        Debug.Log($"Converting ONNX to correct format...");
        var converter = new ONNXModelConverter(optimizeModel: true); // requires the Unity.Barracuda.ONNX assembly
        byte[] modelData = File.ReadAllBytes(modelPath);
        Model model2 = converter.Convert(modelData); // type is Unity.Barracuda.Model
        
        // Copy data from the Model to the NNModel
        Debug.Log($"Copying data from the converted ONNX to the agent...");
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                ModelWriter.Save(writer, model2);
                byte[] bbb = memoryStream.ToArray();
                model.modelData.Value = bbb;
            }
        }
        
        // Change the model (One of these seems to work, but which?)
        agent_params.Model = model;  // TODO: Does this line have any effect?
        root_agent.SetModel("from_disk", model);  // TODO: Does this line have any effect?
        
        Debug.Log($"Loaded new model from {this.modelPath}");
    }
    
    void ParseArgs()
    {
        #if UNITY_EDITOR
        // Fetch terminal arguments from environment variable when using the editor
        string[] args = (Environment.GetEnvironmentVariable("UNITY_CMD_ARGS") ?? string.Empty).Split(' ');
        #else
        // Fetch terminal arguments when using the stand-alone executable
        string[] args = Environment.GetCommandLineArgs();
        #endif
        
        Debug.Log($"Running with args: {string.Join(", ", args)}");
        
        // Parse the arguments
        for (int i = 0; i < args.Length; i++)
        {
            Debug.Log($"Parsing arg: '{args[i].ToLower()}'");
            switch (args[i].ToLower())
            {
                case "--help":
                    Debug.Log("Executing --help case");
                    Debug.Log("Available commands:");
                    Debug.Log("-agent (Choose from one of the following options: 'hanning', 'rect', 'random')");
                    Debug.Log("-benchmark (Enable benchmark mode. Should not be used while training.)");
                    Debug.Log("-model (Path to model, if loading an external ONNX model for benchmark)");
                    Debug.Log("-name (Prefix for the output csv name for the benchmark results)");
                    Debug.Log("-smoketest (Changes benchmark settings to a very short benchmark)");
                    Debug.Log("-decisionPeriod (Integer, usually 1 or 10. Should be a fraction of the audio buffer length)");
                    Debug.Log("-losReward (Float value to set the line-of-sight reward scale)");
                    Debug.Log("-targetSpeed (Float value to set the speed of NavMeshAgent)");
                    Debug.Log("-audioSources (Integer, if above 1 creates additional audio sources for performance testing)");
                    Debug.Log("Example benchmark: ./build.x86_64 -agent hanning -model models/hanning-1.onnx -benchmark -name hanning_1 -decisionPeriod 10 -losReward 0.5 -targetSpeed 3.5");
                    Debug.Log("Example training: ./build.x86_64 -agent hanning");
                    Debug.Log("Example smoketest: ./build.x86_64 -agent hanning -benchmark -smoketest");
                    Application.Quit();
                    break;
                case "-agent":
                    Debug.Log("Executing -agent case");
                    // Which agent to enable? Should work with both training and benchmark
                    if (i + 1 < args.Length && Enum.TryParse<AgentType>(args[i + 1], true, out var parsedAgent))
                    {
                        Debug.Log($"Setting agent to: {args[i + 1]}");
                        agent = parsedAgent;
                    }
                    else
                    {
                        Debug.LogError($"ERROR: Invalid agent type in args {string.Join(", ", args)}");
                        Application.Quit();
                    }
                    break;
                case "-benchmark":
                    Debug.Log("Executing -benchmark case");
                    // Run benchmark instead of training
                    enableBenchmark = true;
                    break;
                case "-model":
                    Debug.Log("Executing -model case");
                    // Path to model, if loading an external ONNX model for benchmark
                    if (i + 1 < args.Length)
                    {
                        Debug.Log($"Got model path: {args[i + 1]}");
                        modelPath = args[i + 1];
                    }
                    break;
                case "-name":
                    Debug.Log("Executing -name case");
                    // Prefix for the output csv name
                    if (i + 1 < args.Length)
                    {
                        Debug.Log($"Got benchmark name: {args[i + 1]}");
                        benchmarkName = args[i + 1];
                    }
                    break;
                case "-smoketest":
                    Debug.Log("Executing -smoketest case");
                    // Run a very short benchmark to make sure everything works
                    enableSmoketest = true;
                    break;
                case "-decisionperiod":
                    Debug.Log("Executing -decisionPeriod case");
                    // Specify the decision period
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var period))
                    {
                        Debug.Log($"Got decision period: {period}");
                        decisionPeriod = period;
                    }
                    break;
                case "-losreward":
                    Debug.Log("Executing -losReward case");
                    // Specify the line-of-sight reward scale
                    if (i + 1 < args.Length && float.TryParse(args[i + 1], out var scale))
                    {
                        Debug.Log($"Got line-of-sight reward scale: {scale}");
                        losRewardScale = scale;
                    }
                    break;
                case "-audiosources": // New case
                    Debug.Log("Executing -audioSources case");

                    // Specify the number of audio sources
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var numSources))
                    {
                        Debug.Log($"Number of audio sources set to: {numSources}");
                        numAudioSources = numSources;
                    }
                    break;
                case "-targetspeed":
                    Debug.Log("Executing -targetSpeed case");
                    // Specify the target speed for NavMeshAgent
                    if (i + 1 < args.Length && float.TryParse(args[i + 1], out var speed))
                    {
                        Debug.Log($"Got target speed: {speed}");
                        targetSpeed = speed;
                    }
                    break;
            }
        }
        #if UNITY_EDITOR
            SaveArgs();
        #endif
        
        // Output the parsed values
        Debug.Log($"Agent: {agent}, benchmark: {enableBenchmark}, ModelPath: {modelPath}, CSV_name: {benchmarkName}, TargetSpeed: {targetSpeed}");
    }
    void SaveArgs()
    {
        #if UNITY_EDITOR
            // Simulate arguments as if they were coming from the command line.
            // This is mostly for debugging the args in editor.
            var argsList = new List<string>
            {
                "UnityEditor",
            };
            if (enableBenchmark)
            {
                argsList.Add("-benchmark");
            }
            if (enableSmoketest)
            {
                argsList.Add("-smoketest");
            }
            argsList.Add("-name");
            argsList.Add(benchmarkName);
            
            argsList.Add("-losreward");
            argsList.Add(losRewardScale.ToString());
                
            argsList.Add("-agent");
            argsList.Add(agent.ToString().ToLower());
            
            argsList.Add("-decisionPeriod");
            argsList.Add(decisionPeriod.ToString());
            
            argsList.Add("-audioSources");
            argsList.Add(numAudioSources.ToString());

            // Add target speed argument
            argsList.Add("-targetSpeed");
            argsList.Add(targetSpeed.ToString());

            if (modelPath != string.Empty && File.Exists(modelPath))
            {
                argsList.Add("-model");
                argsList.Add(modelPath);
            }
                
            string[] simulatedArgs = argsList.ToArray();
            
            Debug.Log($"Command-line arguments: {string.Join(" ", simulatedArgs)}");
            Environment.SetEnvironmentVariable("UNITY_CMD_ARGS", string.Join(" ", simulatedArgs));
        #endif
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}