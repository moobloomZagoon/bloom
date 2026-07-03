#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using FMODUnity;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class LineToFmodEvent
{
    public string lineID = "";
    public string description = "";
    public EventReference fmodEventEN;
    public EventReference fmodEventALT;
}

public class FMODLineProvider : MonoBehaviour
{
    [Header("Yarn Script (Drag Here)")]
    public TextAsset? yarnScript;

    [Header("FMOD Event Assignments")]
    [SerializeField] public List<LineToFmodEvent> lineEventMappings = new();

    public enum VoiceLanguage { EN, ALT }

    [Header("Voice Over Language")]
    [SerializeField] private VoiceLanguage voiceLanguage = VoiceLanguage.EN;

    /// <summary>Read-only accessor if other systems need it.</summary>
    public VoiceLanguage CurrentVoiceLanguage => voiceLanguage;

    /// <summary>Optional event if you want listeners to react.</summary>
    public event Action<VoiceLanguage>? OnVoiceLanguageChanged;

    private readonly Dictionary<string, (EventReference en, EventReference alt)> lineToFmodEvents = new();

    private void Awake()
    {
        RebuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep lookup current while editing (handy when you assign events)
        RebuildLookup();
    }
#endif

    public void SetVoiceLanguage(VoiceLanguage lang)
    {
        if (voiceLanguage == lang) return;
        voiceLanguage = lang;
        OnVoiceLanguageChanged?.Invoke(voiceLanguage);
    }

    public void ToggleVoiceLanguage()
    {
        SetVoiceLanguage(voiceLanguage == VoiceLanguage.EN ? VoiceLanguage.ALT : VoiceLanguage.EN);
    }

    private void RebuildLookup()
    {
        lineToFmodEvents.Clear();
        foreach (var m in lineEventMappings)
        {
            if (!string.IsNullOrEmpty(m.lineID))
                lineToFmodEvents[m.lineID] = (m.fmodEventEN, m.fmodEventALT);
        }
    }

    public bool TryGetFmodEvent(string lineID, out EventReference fmodEvent)
    {
        if (lineToFmodEvents.TryGetValue(lineID, out var ev))
        {
            fmodEvent = (voiceLanguage == VoiceLanguage.EN) ? ev.en : ev.alt;

            // Optional fallback: if selected language missing, try the other
            if (fmodEvent.IsNull)
            {
                var fallback = (voiceLanguage == VoiceLanguage.EN) ? ev.alt : ev.en;
                if (!fallback.IsNull)
                {
                    fmodEvent = fallback;
                    return true;
                }
            }

            return !fmodEvent.IsNull;
        }

        Debug.LogWarning($"⚠️ No FMOD event assigned for Yarn Line ID: {lineID}");
        fmodEvent = new EventReference();
        return false;
    }

    // This is your validator/sync method. Keep your existing logic if you prefer.
    public void ValidateAndSyncLineEvents()
    {
        if (yarnScript == null)
        {
            Debug.LogWarning("⚠️ No Yarn script assigned.");
            return;
        }

        var existing = lineEventMappings
            .Where(e => !string.IsNullOrEmpty(e.lineID))
            .ToDictionary(e => e.lineID, e => (e.fmodEventEN, e.fmodEventALT, e.description));

        string[] lines = yarnScript.text.Split('\n');

        var newList = new List<LineToFmodEvent>();
        var seen = new HashSet<string>();

        int added = 0;

        foreach (var raw in lines)
        {
            var idMatch = Regex.Match(raw, @"#line:([a-fA-F0-9]+)");
            if (!idMatch.Success) continue;

            var id = $"line:{idMatch.Groups[1].Value}";
            if (!seen.Add(id)) continue;

            var tags = Regex.Matches(raw, @"#(\w+)")
                .Select(m => m.Groups[1].Value)
                .Where(t => t != "line")
                .ToList();

            var desc = tags.Count > 0 ? tags[0] : "";

            (EventReference en, EventReference alt, string prevDesc) =
                existing.TryGetValue(id, out var prev)
                    ? prev
                    : (new EventReference(), new EventReference(), "");

            newList.Add(new LineToFmodEvent
            {
                lineID = id,
                description = !string.IsNullOrEmpty(desc) ? desc : prevDesc,
                fmodEventEN = en,
                fmodEventALT = alt
            });

            added++;
        }

        lineEventMappings = newList;
        RebuildLookup();

        Debug.Log($"✅ Synced {added} line IDs into FMOD mappings.");

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif
    }
}
