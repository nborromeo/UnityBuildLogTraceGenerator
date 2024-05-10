using UnityEditor;
using UnityEngine;

namespace Unity.Profiling.BuildLogAnalyzer.Editor
{
    public class BuildLogAnalyzerWindow : EditorWindow
    {
        private float _minDuration = 0.2f;
        private string _buildLogPath;

        [MenuItem("Window/Analysis/Build Log Analyzer")]
        private static void OpenWindow()
        {
            CreateWindow<BuildLogAnalyzerWindow>().Show();
        }

        private void OnGUI()
        {
            _minDuration = EditorGUILayout.Slider("Min Duration", _minDuration, 0, 1);
            if (GUILayout.Button("Select log"))
            {
                _buildLogPath = EditorUtility.OpenFilePanel("", "", "*.*");
            }

            if (string.IsNullOrEmpty(_buildLogPath))
            {
                return;
            }

            EditorGUILayout.LabelField(_buildLogPath);
            if (GUILayout.Button("Analyze"))
            {
                var minDurationUs = (long) (_minDuration * 1000000);
                var outputPath = Application.dataPath + "/../buildLogTrace.json";
                new BuildLogParser(minDurationUs).Analyze(_buildLogPath, outputPath);
                EditorUtility.DisplayDialog("Build Log Analyzer", "Analysis complete", "Ok");
            }
        }
    }
}