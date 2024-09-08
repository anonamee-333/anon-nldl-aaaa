using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Audio
{
    /// <summary>
    /// A sensor implementation for vector observations.
    /// </summary>
    public class PositionSensor : ISensor
    {
        // TODO use float[] instead
        // TODO allow setting float[]
        List<float> m_Observations;
        ObservationSpec m_ObservationSpec;
        string m_Name;
        private GameObject m_parent;

        /// <summary>
        /// Initializes the sensor.
        /// </summary>
        /// <param name="observationSize">Number of vector observations.</param>
        /// <param name="name">Name of the sensor.</param>
        /// <param name="observationType"></param>
        public PositionSensor(int observationSize, GameObject parent, string name = null, ObservationType observationType = ObservationType.Default)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = $"PositionSensor_size{observationSize}";
                if (observationType != ObservationType.Default)
                {
                    name += $"_{observationType.ToString()}";
                }
            }

            m_parent = parent;
            m_Observations = new List<float>(observationSize);
            m_Name = name;
            m_ObservationSpec = ObservationSpec.Vector(observationSize, observationType);
        }

        /// <inheritdoc/>
        public int Write(ObservationWriter writer)
        {
            writer.Add(m_parent.transform.localPosition / 100f);  // Divide to get closer to [-1,1] range 
            return 3;
        }

        /// <inheritdoc/>
        public void Update()
        {
            Clear();
        }

        /// <inheritdoc/>
        public void Reset()
        {
            Clear();
        }

        /// <inheritdoc/>
        public ObservationSpec GetObservationSpec()
        {
            return m_ObservationSpec;
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return m_Name;
        }

        /// <inheritdoc/>
        public virtual byte[] GetCompressedObservation()
        {
            return null;
        }

        /// <inheritdoc/>
        public CompressionSpec GetCompressionSpec()
        {
            return CompressionSpec.Default();
        }

        void Clear()
        {
            m_Observations.Clear();
        }


        
    }
}