#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FMODLineProvider))]
public class FMODLineProviderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        var provider = (FMODLineProvider)target;

        if (GUILayout.Button("Validate / Sync Line IDs", GUILayout.Height(28)))
        {
            Undo.RecordObject(provider, "Validate / Sync Line IDs");
            provider.ValidateAndSyncLineEvents();
            EditorUtility.SetDirty(provider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(provider);
        }

        if (GUILayout.Button("Toggle Voice Language (EN/ALT)", GUILayout.Height(22)))
        {
            Undo.RecordObject(provider, "Toggle Voice Language");
            provider.ToggleVoiceLanguage();
            EditorUtility.SetDirty(provider);
            PrefabUtility.RecordPrefabInstancePropertyModifications(provider);
        }
    }
}
#endif
