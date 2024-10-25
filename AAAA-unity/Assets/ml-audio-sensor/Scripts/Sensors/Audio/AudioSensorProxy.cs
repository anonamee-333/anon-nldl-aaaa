using Unity.Barracuda;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace MBaske.Sensors.Audio
{
    /// <summary>
    /// Sensor proxy that refers to an <see cref="AudioSensor"/>.
    /// </summary>
    public class AudioSensorProxy : ISensor
    {
        public SensorObservationShape Shape => m_AudioSensor.Shape;
        public SensorCompressionType CompressionType => m_AudioSensor.CompressionType;

        public readonly AudioSensor m_AudioSensor;

        /// <summary>
        /// Initializes the sensor.
        /// </summary>
        /// <param name="audioSensor">The <see cref="AudioSensor"/> to refer to.</param>
        public AudioSensorProxy(AudioSensor audioSensor)
        {
            m_AudioSensor = audioSensor;
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return m_AudioSensor.GetName() + "_Proxy";
        }

        /// <inheritdoc/>
        public ObservationSpec GetObservationSpec()
        {
            return m_AudioSensor.GetObservationSpec();
        }

        /// <inheritdoc/>
        public CompressionSpec GetCompressionSpec()
        {
            return m_AudioSensor.GetCompressionSpec();
        }

        /// <inheritdoc/>
        public byte[] GetCompressedObservation()
        {
            return m_AudioSensor.CachedCompressedObservation;
        }

        /// <inheritdoc/>
        public int Write(ObservationWriter writer)
        {
            return m_AudioSensor.Write(writer);
        }
        
        public int Write(Tensor tensor)
        {
            return m_AudioSensor.Write(tensor);
        }


        /// <inheritdoc/>
        public void Update() { }

        /// <inheritdoc/>
        public void Reset() { }
    }
}