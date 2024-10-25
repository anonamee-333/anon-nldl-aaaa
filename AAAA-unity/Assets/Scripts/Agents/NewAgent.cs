using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MBaske.Sensors.Audio;
using TMPro;
using Unity.Barracuda;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class NewAgent : Agent, IMeasurable
{
    public bool normalizeActionMagnitude = true;
    public bool trainingMode;
    public bool evalMode;
    public bool forceRandom = false;  // Forces action to always be random values
    public bool forceDirectLine = false;  // Forces action to always point at the target
    public bool useMoveDirect = false;
    public float moveDirectDistance = 0f;  // If distance is less than this and LoS==true, move directly towards target

    public delegate void TargetReached();
    public TargetReached OnTargetReached;
    
    public GameObject proxyAgent;
    public bool isProxy;
    public GameObject target;

    public bool randomizeRotation;
    public bool randomizePosition;
    public float randomPosRadius = 25;
    public float maxTargetDistance = 100;
    public float targetDistanceThreshold = 4f;
    
    public float directionScale = 1f;
    public float losScale = 0.5f;

    private int _stepsSinceAction = 0;
    private float _prevAngleCheck = 0;
    private bool _prevLosStatus;
    
    

    protected float[] _actions = {0,0};
    protected float _actionLoS = 0;
    protected Vector3 _actionDirection = Vector3.zero;  // Direction from actions given by policy
    protected Rigidbody rbody;
    
    public NavMeshSensor navMeshSensor;
    // protected BehaviorParameters _behaviorParameters;
    protected float _lastDistance = 0;

    private GridNavigatorComponent _gridNavigatorComponent;

    // protected NavMeshAgent _navMeshAgent;

    
    
    /* For debugging */

    public bool forceLoS = false;
    public bool forceHeuristicAction = false;
    public List<float> heuristicAction = new List<float>();
    private float _previousCumulativeReward = 0;
    private float _previousEpisodeReward = 0;
    private TextMeshProUGUI _debugTextBox;
    protected List<float> _previousObservation = new();
    private static bool _debugDisplayLock;
    private BehaviorParameters _behaviorParameters;

    private void Start()
    {
        _behaviorParameters = GetComponent<BehaviorParameters>();
    }

    public override void Initialize()
    {
        base.Initialize();
        rbody = GetComponent<Rigidbody>();

        if (!target)
        {
            // Automatically find a suitable target, if none selected (just a hackish QOL feature)
            foreach (var tar in FindObjectsOfType<MonoBehaviour>().OfType<ITarget>())
            {
                var obj = tar.GetGameObject();
                if (obj.activeInHierarchy)
                {
                    target = obj;
                }
            }
        }
        
        navMeshSensor = GetComponentInChildren<NavMeshSensor>();
        if (!navMeshSensor) navMeshSensor = gameObject.AddComponent<NavMeshSensor>();

        _behaviorParameters = GetComponentInChildren<BehaviorParameters>();
        if (!_debugDisplayLock)
        {
            _debugDisplayLock = true;
            _debugTextBox = FindObjectOfType<TextMeshProUGUI>();
        }
        _gridNavigatorComponent = gameObject.AddComponent<GridNavigatorComponent>();
        _gridNavigatorComponent.SetTarget(target);
    }

    public float GetDegreesToTarget()
    {
        var observations = GetDirectionAndDistanceToTarget();
        Vector3 direction = observations;
        var angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        angle /= 180;  // Scale to [-1,1]
        return angle;
    }
    
    public float GetCurrentActionDegrees()
    {
        Vector3 direction = _actionDirection;
        var angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        angle /= 180;  // Scale to [-1,1]
        return angle;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        for (int i = 0; i < _actions.Length; i++)
        {
            actionsOut.ContinuousActions.Array[i] = _actions[i];
        }
    }

    void Update()
    {
        DrawDebugRays();
    }

    void FixedUpdate()
    {
        _stepsSinceAction++;
        _previousCumulativeReward = GetCumulativeReward();
        if (isProxy && !navMeshSensor) navMeshSensor = FindObjectOfType<NavMeshSensor>();
        if (rbody)
        {
            rbody.velocity = Vector3.zero;
            rbody.angularVelocity = Vector3.zero;
        }
        Move();
        ComputeReward();
        if (isProxy)
        {
            transform.position = proxyAgent.transform.position;
            transform.rotation = proxyAgent.transform.rotation;
        }

        if (_debugTextBox)
        {
            var obsFormatted = string.Concat(_previousObservation.Select(i => string.Format("{0,4:F4}; ", i)));
            _debugTextBox.text = $"EpisodeReward: {_previousEpisodeReward}, \n" +
                                 $"Reward: {GetCumulativeReward() - _previousCumulativeReward}, \n" +
                                 $"RewardSum: {GetCumulativeReward()}, \n" +
                                 $"Observation: [{obsFormatted}], \n" +
                                 $"Action: {String.Join("; ", _actions)}";
        }

        if (target)
        {
            // var distance = Vector3.Distance(transform.position, target.transform.position);
            var distance = navMeshSensor.GetRemainingDistance(target.transform.position);
            if (distance < targetDistanceThreshold)
            {
                AddReward(2f);
                if (evalMode)
                {
                    // Let other classes handle resetting
                    OnTargetReached.Invoke();
                    _gridNavigatorComponent.ResetNodes();
                }
                else
                {
                    // Target reached -> end episode
                    EndEpisode();
                    _gridNavigatorComponent.ResetNodes();
                }
            }
        }
    }
    

    protected void DrawDebugRays()
    {
        var obs = GetDirectionAndDistanceToTarget();
        Vector3 targetDir = new Vector3(obs[0], obs[1], obs[2]);
        float distanceNormalized = obs.w;
        Debug.DrawRay(transform.position, _actionDirection * 50, Color.red, 0.2f);
        Debug.DrawRay(transform.position, targetDir * 50, Color.green, 0.2f);
    }

    protected void ComputeReward()
    {
        
        
        Vector3 targetDirNavmesh = navMeshSensor.GetNextDirection(target.transform.position);
        
        // Basic direction-based reward
        var directionReward = Vector3.Dot(_actionDirection, targetDirNavmesh.normalized);
        directionReward = Mathf.Clamp(directionReward, -1, 1) - 1; // Change to penalty [-2,0] 
        float reward = directionReward * directionScale;  //and change scale
        

        // Line-of-sight reward to support custom grid-based navigation
        // Negative action means no LOS
        // Positive action means yes LOS
        float losReward = (_actionLoS - 0.5f) * 2f;  // Scale to [-1, 1]
        // Debug.Log($"LosReward1: {losReward}");
        losReward = TargetInLineOfSight() ? -losReward : losReward;  // Invert reward depending on LoS
        // Debug.Log($"LosReward2: {losReward}");
        losReward = Mathf.Clamp(losReward, -1, 1) - 1;  // Change to penalty [-2,0]
        // Debug.Log($"LosReward3: {losReward}");
        reward += losReward * losScale;  // Scale with multiplier
        
        // Add rewards
        AddReward(reward * Time.fixedDeltaTime);
        //Debug.Log($"DirectionReward: {directionReward}, LosReward: {losReward}, (los: {_prevLosStatus})");
    }

    bool TargetInLineOfSight()
    {
        return _gridNavigatorComponent.NavMeshLineOfSight(target.transform.position);
    }
    
    bool TargetInLineOfSight2()
    {
        bool spotted = false;
        Vector3 direction = target.transform.position - transform.position;
        float distance = direction.magnitude;
        var layer = _gridNavigatorComponent._currentNode.obstacleLayer;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, distance, layer))
        {
            // Collision, there is probably a wall between us and this node
            // Debug.DrawRay(transform.position, direction, Color.yellow, 3f);
            spotted = false;
        }
        else
        {
            spotted = true;
            Debug.DrawRay(transform.position, direction, Color.blue, 3f);
        }
        return spotted;
    }

    public void Move()
    {
        
        bool lineofsight;
        bool isInference = _behaviorParameters.BehaviorType == BehaviorType.InferenceOnly;
        // bool isInference = false;
        if (forceLoS)
        {
            lineofsight = true;
        }
        else if (isInference)
            // During inference, use the LoS information from the agent
            lineofsight = _actionLoS > 0.5f;
        else
        {
            // During training, use the ground truth LoS information
            // to avoid agent getting stuck in back-and-forth movement
            bool navMeshLos = TargetInLineOfSight();
            bool raycastLos = TargetInLineOfSight2();
            if (navMeshLos != raycastLos)
            {
                // We are in the gray area - lets keep the previous observation
                // This is done to avoid rapid switching between two states in edge cases
                lineofsight = _prevLosStatus;
            }
            else
            {
                // Both heuristics agree on LoS - it is safe to switch the los status
                lineofsight = navMeshLos;
                _prevLosStatus = lineofsight;  // save current status for future reference
            }
        }
        
        
        if (forceRandom) {
            _gridNavigatorComponent.MoveRandom(lineofsight);
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (useMoveDirect && lineofsight && distance < moveDirectDistance)
        {
            // For simulating audio-visual agent, which gets perfect information when it sees the target from close
            _gridNavigatorComponent.MoveDirect();
        }
        else
        {
            _gridNavigatorComponent.Move(_actionDirection, lineofsight);
        }
        
    }
    
    public Vector3 SampleValidPoint(Vector3 center, Vector3 direction, float maxDistance) {
        Vector3 randomPos = direction.normalized + center;
        UnityEngine.AI.NavMeshHit hit;
        bool success = UnityEngine.AI.NavMesh.SamplePosition(randomPos, out hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas);
        if (success)
        {
            return hit.position;
        }
        else
        {
            return transform.position;
        }
    }




    public void SetActions(float[] actions)
    {
        // Debug.Log("SET ACTIONSSSS");
        _actions = actions;
        
        if (forceRandom)
        {
            for(int i = 0; i < _actions.Length; i++)
            {
                _actions[i] = Random.Range(-1f, 1f);
            }
            _actions[2] = 1;  // LoS always at 1 for the random agent (should not matter too much if this is random or not)
        }
        if (forceHeuristicAction)
        {
            _actions = heuristicAction.ToArray();
        }

        _actionDirection = new Vector3(_actions[0], 0, _actions[1]);
        _actionDirection = transform.rotation * _actionDirection;  // Rotate action to match agent rotation
        _actionLoS = _actions[2];

        if (forceDirectLine)
        {
            _actionDirection = target.transform.position - transform.position;
            _actionDirection.y = 0;
            _actionDirection.Normalize();
            _actionLoS = actions[2] = 1;
        }
        
        if (normalizeActionMagnitude || _actionDirection.magnitude > 1)
        {
            // Normalize or clip action lenght to 1
            _actionDirection.Normalize();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        base.OnActionReceived(actions);
        float[] a =  actions.ContinuousActions.Array;
        SetActions(a);
    }

    public void TeleportToRandomPosition()
    {
        if (randomizePosition)
        {
            Vector3 pos = new Vector3(Random.Range(-randomPosRadius, randomPosRadius), 1.5f, Random.Range(-randomPosRadius, randomPosRadius));
            transform.position = pos;
        }

        if (randomizeRotation)
        {
            transform.Rotate(0, Random.Range(-180, 180), 0);
        }
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        _previousEpisodeReward = _previousCumulativeReward;
        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            source.Stop();
        }

        foreach (var obj in FindObjectsOfType<MonoBehaviour>().OfType<ITarget>())
        {
            obj.ResetTarget();
        }
        SetReward(0f);
        _lastDistance = 0;
        TeleportToRandomPosition();
        if (trainingMode)
        {
            target.transform.position = navMeshSensor.GetRandomPoint(transform.position, maxTargetDistance);
        }
    }

    protected Vector4 GetDirectionAndDistanceToTarget()
    {
        var pos = target.transform.position;
        float distance = navMeshSensor.GetRemainingDistance(pos);
        var targetDir = navMeshSensor.GetNextDirection(pos);
        distance /= 1000f;  // Normalize to roughly [0,1] - need to account for long mazes
        distance = Mathf.Clamp(distance, 0, 1);
        targetDir = targetDir.normalized;
        return new Vector4(targetDir.x, targetDir.y, targetDir.z, distance);
    }

    public virtual List<string> GetColumnNames()
    {
        return new List<string>{"AgentPositionX", "AgentPositionY", "AgentPositionZ", 
            "StepsSinceAction",  "AngleToTarget",
            "ActionAngle", "ActionMagnitude",
            "RewardSum",
            "DistanceToTargetNormalized", "ActionLos", "TargetLos"
        };
    }

    public virtual List<string> GetValues()
    {
        if (_prevAngleCheck != GetCurrentActionDegrees())
        {
            _stepsSinceAction = 0;
        }
        _prevAngleCheck = GetCurrentActionDegrees();
        float distanceNormalized = GetDirectionAndDistanceToTarget().w;
        return new List<string>
        {
            transform.position.x.ToString(), transform.position.y.ToString(), transform.position.z.ToString(),
            _stepsSinceAction.ToString(),  GetDegreesToTarget().ToString(),
            GetCurrentActionDegrees().ToString(), _actionDirection.sqrMagnitude.ToString(),
            GetCumulativeReward().ToString(),
            distanceNormalized.ToString(),
            _actionLoS.ToString(),
            TargetInLineOfSight().ToString(),
            
        };
    }
}
