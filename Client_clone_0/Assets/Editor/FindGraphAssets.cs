using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// EditorWindow utility to find candidate "Graph" assets (Visual Scripting, Animator state machines, etc.)
// Use: Window -> Tools -> Debug -> Find Graph Assets. Click "Scan for Graph assets in project".
// It lists assets whose type name or path contains common Graph keywords.
public class FindGraphAssets : EditorWindow
{
    private Vector2 _scroll;
    private List<string> _results = new List<string>();

    [MenuItem("Tools/Debug/Find Graph Assets")]
    public static void ShowWindow() => GetWindow<FindGraphAssets>("Find Graph Assets");

    private void OnGUI()
    {
        GUILayout.Space(6);
        if (GUILayout.Button("Scan for Graph assets in project", GUILayout.Height(28)))
        {
            Scan();
        }

        GUILayout.Space(6);
        if (_results.Count > 0)
        {
            GUILayout.Label($"Found {_results.Count} candidate assets:", EditorStyles.boldLabel);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(300));
            foreach (var r in _results)
            {
                GUILayout.Label(r);
            }
            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("No candidates listed yet. Click Scan to search.", EditorStyles.helpBox);
        }
    }

    private static readonly string[] _keywords = new[]
    {
        "Graph",
        "StateGraph",
        "ScriptGraph",
        "XEventGraph",
        "AnimationStateMachine",
        "AnimationBlendTree",
        "VisualScripting",
        "Bolt",
    };

    private void Scan()
    {
        _results.Clear();
        var guids = AssetDatabase.FindAssets(string.Empty);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (path.StartsWith("Packages/") || path.EndsWith(".meta")) continue;

            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj == null) continue;
            var typeName = obj.GetType().Name ?? "";

            foreach (var k in _keywords)
            {
                if (typeName.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _results.Add($"{typeName} : {path}");
                    break;
                }
            }
        }

        Debug.Log($"[FindGraphAssets] Scan complete, found {_results.Count} candidate assets.");
    }
}
