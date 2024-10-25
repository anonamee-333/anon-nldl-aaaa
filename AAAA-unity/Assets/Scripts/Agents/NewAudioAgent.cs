
using System.Collections.Generic;

using MBaske.Sensors.Audio;

using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;


public class NewAudioAgent : NewAgent
{
    
    public enum ObservationType
    {
        Audio,
        Coordinates,
        RelativeAngle,
    }
    
    public ObservationType observationType;
    public AudioSensorProxy audioProxy = null;
    
    [FormerlySerializedAs("m_DecisionInterval")] [SerializeField]
    public int DecisionInterval;
    private int m_BufferLength;
    
    // Assuming there's only one audio sensor per agent.
    protected IAudioSampler m_Sampler;
    
    
    
    public override void Initialize()
    {
        base.Initialize();
        if (isProxy) return;
        
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 50;
        m_Sampler = GetAudioSampler();
        m_Sampler.SamplingUpdateEvent += OnSamplingUpdate;

        if (observationType == ObservationType.Audio)
        {
            m_Sampler.SamplingEnabled = true;
            AudioSensorComponent comp = GetComponentInChildren<AudioSensorComponent>();
            if (comp.Sensor == null)
            {
                comp.CreateSensors();
            }
            //Debug.Log(comp.Shape);
            audioProxy = new AudioSensorProxy(comp.Sensor);
        }
    }
    
    protected IAudioSampler GetAudioSampler()
    {
        if (m_Sampler == null)
        {
            var components = GetComponentsInChildren<SensorComponent>();
            foreach (var comp in components)
            {
                if (comp is IAudioSampler)
                {
                    return (IAudioSampler)comp;
                }
            }
            throw new MissingComponentException("Audio sensor component not found.");
        }

        return m_Sampler;
    }
    
    protected void OnSamplingUpdate(int samplingStepCount, bool bufferLengthReached)
    {
        if (samplingStepCount % DecisionInterval == 0)
        {
            // Will also request action.
            RequestDecision();
        }
        else
        {
            // Act in between decisions.
            RequestAction();
        }
    }

    private void OnDestroy()
    {
        if (m_Sampler != null)
        {
            m_Sampler.SamplingUpdateEvent -= OnSamplingUpdate;
        }
    }
    
    
    private void OnValidate()
    {
        var component = GetComponentInChildren<AudioSensorComponent>();
        if (!component) return;
        // https://stackoverflow.com/a/7065771
        component.SettingsUpdateEvent -= OnSensorSettingsUpdate;
        component.SettingsUpdateEvent += OnSensorSettingsUpdate;
        m_BufferLength = component.BufferLength;
        ValidateIntervals();
    }

    private void OnSensorSettingsUpdate(AudioSensorComponent component)
    {
        m_BufferLength = component.BufferLength;
        ValidateIntervals();
    }

    private void ValidateIntervals()
    {
        DecisionInterval = Mathf.Clamp(DecisionInterval, 1, m_BufferLength);

        if (m_BufferLength % DecisionInterval != 0)
        {
            var divisors = new List<int>();
            for (int i = 1; i <= m_BufferLength; i++)
            {
                if (m_BufferLength % i == 0)
                {
                    divisors.Add(i);
                }
            }
            Debug.LogWarning($"Decision interval should be a fraction of the audio buffer's length {m_BufferLength}: "
                             + string.Join(", ", divisors.ToArray()));
        }

        if (MaxStep % m_BufferLength != 0)
        {
            Debug.LogWarning($"Max Step should be a multiple of the audio buffer's length {m_BufferLength}.");
        }
    }
    
    public override List<string> GetColumnNames()
    {
        // Get the base list of column names
        var columns = base.GetColumnNames();
        // Add new column
        columns.Add("DecisionPeriod");  // ML-agents calls this DecisionPeriod instead of DecisionInterval
        return columns;
    }

    public override List<string> GetValues()
    {
        // Get the base list of values
        var values = base.GetValues();
        // Add new value, assuming GetAudioLevel() is a method that returns the audio level as a float
        values.Add(DecisionInterval.ToString());
        return values;
    }
}
