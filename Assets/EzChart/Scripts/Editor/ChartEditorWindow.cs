#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChartUtil.ChartEditor
{
    public class EzChartWindow : EditorWindow
    {
        public ChartPresetLoader.Preset[] presets;
        Vector2 scrollPos;

        [MenuItem("Window/EzChart")]
        static void Init()
        {
            EzChartWindow window = (EzChartWindow)GetWindow(typeof(EzChartWindow), false, "EzChart");
            window.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

            EditorGUILayout.LabelField("Chart Previews");

            if (GUILayout.Button("Update Preview For All Charts"))
            {
                Chart[] charts = Resources.FindObjectsOfTypeAll<Chart>();
                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    try
                    {
                        chart.UpdateChart();
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            if (GUILayout.Button("Update Preview For Active Charts"))
            {
                Chart[] charts = FindObjectsOfType<Chart>();
                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    try
                    {
                        chart.UpdateChart();
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            if (GUILayout.Button("Clear Preview For All Charts"))
            {
                Chart[] charts = Resources.FindObjectsOfTypeAll<Chart>();
                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    try
                    {
                        chart.Clear();
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            EditorGUILayout.LabelField("Chart Presets");

            ScriptableObject target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty presetsProperty = so.FindProperty("presets");
            EditorGUILayout.PropertyField(presetsProperty, true);
            so.ApplyModifiedProperties();

            if (GUILayout.Button("Load Preset For Active Charts") && presets.Length > 0)
            {
                Chart[] charts = FindObjectsOfType<Chart>();
                ChartOptions[] chartOptions = new ChartOptions[charts.Length];
                for (int i = 0; i < charts.Length; ++i) chartOptions[i] = charts[i].chartOptions;
                Undo.RecordObjects(chartOptions, "Load chart presets");

                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    if (chart.chartOptions == null) continue;
                    try
                    {
                        for (int i = 0; i < presets.Length; ++i)
                        {
                            ChartOptionsProfile profile = presets[i].profile;
                            ChartOptionsPreset preset = presets[i].preset;
                            if (preset == null || profile == null) continue;
                            profile.LoadPreset(preset, ref chart.chartOptions);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(chart.chartOptions);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            if (GUILayout.Button("Load Preset For All Charts") && presets.Length > 0)
            {
                Chart[] charts = Resources.FindObjectsOfTypeAll<Chart>();
                ChartOptions[] chartOptions = new ChartOptions[charts.Length];
                for (int i = 0; i < charts.Length; ++i) chartOptions[i] = charts[i].chartOptions;
                Undo.RecordObjects(chartOptions, "Load chart presets");

                foreach (Chart chart in charts)
                {
                    if (chart.gameObject.scene.name == null) continue;
                    if (chart.chartOptions == null) continue;
                    try
                    {
                        for (int i = 0; i < presets.Length; ++i)
                        {
                            ChartOptionsProfile profile = presets[i].profile;
                            ChartOptionsPreset preset = presets[i].preset;
                            if (preset == null || profile == null) continue;
                            profile.LoadPreset(preset, ref chart.chartOptions);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(chart.chartOptions);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif