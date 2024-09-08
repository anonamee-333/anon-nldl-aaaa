using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;

namespace MBaske.Sensors.Audio
{
    /// <summary>
    /// A SensorComponent that creates a <see cref="PositionSensor"/>.
    /// </summary>
    //[AddComponentMenu("ML Agents/Vector Sensor", (int)MenuGroup.Sensors)]
    public class PositionSensorComponent : SensorComponent
    {
        /// <summary>
        /// Name of the generated <see cref="PositionSensor"/> object.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }
        [HideInInspector, SerializeField]
        private string m_SensorName = "PositionSensor";

        /// <summary>
        /// The number of float observations in the PositionSensor
        /// </summary>
        public int ObservationSize
        {
            get { return m_ObservationSize; }
            set { m_ObservationSize = value; }
        }

        [HideInInspector, SerializeField]
        int m_ObservationSize = 3;

        [HideInInspector, SerializeField]
        ObservationType m_ObservationType;

        PositionSensor m_Sensor;

        /// <summary>
        /// The type of the observation.
        /// </summary>
        public ObservationType ObservationType
        {
            get { return m_ObservationType; }
            set { m_ObservationType = value; }
        }

        [HideInInspector, SerializeField]
        [Range(1, 50)]
        [Tooltip("Number of camera frames that will be stacked before being fed to the neural network.")]
        int m_ObservationStacks = 1;

        /// <summary>
        /// Whether to stack previous observations. Using 1 means no previous observations.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int ObservationStacks
        {
            get { return m_ObservationStacks; }
            set { m_ObservationStacks = value; }
        }

        /// <summary>
        /// Creates a PositionSensor.
        /// </summary>
        /// <returns></returns>
        public override ISensor[] CreateSensors()
        {
            m_Sensor = new PositionSensor(m_ObservationSize, gameObject, m_SensorName, m_ObservationType);
            if (ObservationStacks != 1)
            {
                return new ISensor[] { new StackingSensor(m_Sensor, ObservationStacks) };
            }
            return new ISensor[] { m_Sensor };
        }

        /// <summary>
        /// Returns the underlying PositionSensor
        /// </summary>
        /// <returns></returns>
        public PositionSensor GetSensor()
        {
            return m_Sensor;
        }
    }
}