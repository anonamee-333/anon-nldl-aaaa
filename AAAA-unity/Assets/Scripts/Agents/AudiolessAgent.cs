
using System.Collections.Generic;

using MBaske.Sensors.Audio;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;


public class AudiolessAgent : NewAgent
{
    
    private DecisionRequester decisionRequester;
    
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
        values.Add(decisionRequester.DecisionPeriod.ToString());
        return values;
    }
}
