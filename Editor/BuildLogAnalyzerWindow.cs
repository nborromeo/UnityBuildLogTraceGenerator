using UnityEditor;
using UnityEngine;

namespace Unity.Profiling.BuildLogAnalyzer.Editor
{
    public class BuildLogAnalyzerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private BuildLogParser _parser;
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
                _parser = new BuildLogParser(minDurationUs);
                _parser.Analyze(_buildLogPath, outputPath);
            }
            
            if (_parser != null)
            {
                EditorGUIUtility.labelWidth = this.position.width * 0.33f;
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                var biggestMarkerDuration = _parser.TopMarkers[0].DurationTime;
                foreach (var marker in _parser.TopMarkers)
                {
                    var minValue = 0f;
                    var markerDuration = marker.DurationTime;

                    EditorGUILayout.MinMaxSlider(
                        $"[{marker.Pid}] ({marker.InitLine}:{marker.EndLine}) {marker.Type.name} {markerDuration}s",
                        ref minValue, ref markerDuration, 
                        0, biggestMarkerDuration);
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }
}