using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Prevents Unity's built-in Inspector from trying to create editors for destroyed selection targets.
/// This avoids SerializedObjectNotCreatableException when a selected runtime UI object is destroyed
/// during play mode transitions or singleton cleanup.
/// </summary>
[InitializeOnLoad]
internal static class InspectorNullSelectionGuard
{
    private static bool _sanitizeScheduled;
    private static double _sanitizeUntil;

    static InspectorNullSelectionGuard()
    {
        Selection.selectionChanged += ScheduleSanitize;
        EditorApplication.hierarchyChanged += ScheduleSanitize;
        EditorApplication.playModeStateChanged += _ => ArmSanitizeWindow();

        ArmSanitizeWindow();
    }

    private static void ScheduleSanitize()
    {
        if (_sanitizeScheduled)
            return;

        _sanitizeScheduled = true;
        EditorApplication.delayCall += SanitizeSelection;
    }

    private static void ArmSanitizeWindow()
    {
        _sanitizeUntil = EditorApplication.timeSinceStartup + 2.0;
        EditorApplication.update -= SanitizeDuringWindow;
        EditorApplication.update += SanitizeDuringWindow;
        ScheduleSanitize();
    }

    private static void SanitizeDuringWindow()
    {
        SanitizeSelection();

        if (EditorApplication.timeSinceStartup >= _sanitizeUntil)
            EditorApplication.update -= SanitizeDuringWindow;
    }

    private static void SanitizeSelection()
    {
        _sanitizeScheduled = false;

        if (EditorApplication.isCompiling)
            return;

        int[] selectedIds = Selection.instanceIDs;
        if (selectedIds == null || selectedIds.Length == 0)
            return;

        List<int> validIds = null;
        for (int i = 0; i < selectedIds.Length; i++)
        {
            int id = selectedIds[i];
            if (id != 0 && EditorUtility.InstanceIDToObject(id) != null)
            {
                validIds?.Add(id);
                continue;
            }

            validIds ??= CopySelectionPrefix(selectedIds, i);
        }

        if (validIds != null)
            Selection.instanceIDs = validIds.ToArray();
    }

    private static List<int> CopySelectionPrefix(int[] selectedIds, int count)
    {
        var validIds = new List<int>(selectedIds.Length);
        for (int i = 0; i < count; i++)
            validIds.Add(selectedIds[i]);

        return validIds;
    }
}
