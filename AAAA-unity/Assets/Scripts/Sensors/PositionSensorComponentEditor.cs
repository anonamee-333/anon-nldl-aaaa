#if (UNITY_EDITOR)
using UnityEditor;
using UnityEngine;

namespace MBaske.Sensors.Audio
{
    [CustomEditor(typeof(PositionSensorComponent), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    internal class PositionSensorComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            // Drawing the PositionSensorComponent
            
            EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
            //EditorGUILayout.PropertyField(so.FindProperty("m_ObservationSize"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_ObservationType"), true);
            //EditorGUILayout.PropertyField(so.FindProperty("m_ObservationStacks"), true);


            so.ApplyModifiedProperties();
        }
    }
}
#endif