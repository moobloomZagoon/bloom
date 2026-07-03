#nullable enable

using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using FMODUnity;
using FMOD.Studio;
using Debug = UnityEngine.Debug;

public class YarnFmodTrigger : MonoBehaviour
{
    [SerializeField] private DialogueRunner? dialogueRunner;

    // Persistent instances by event path
    private readonly Dictionary<string, EventInstance> instances = new();

    private void Awake()
    {
        if (dialogueRunner == null)
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();

        if (dialogueRunner == null)
        {
            Debug.LogError("[YarnFmodTrigger] No DialogueRunner found in scene.");
            return;
        }

        // Explicit string/float bindings (prevents the “correct component” conversion issue)
        dialogueRunner.AddCommandHandler<string>("play_fmod_oneshot", PlayFmodOneShot);
        dialogueRunner.AddCommandHandler<string>("play_fmod_instance", PlayFmodInstance);
        dialogueRunner.AddCommandHandler<string>("stop_fmod_instance", StopFmodInstance);
        dialogueRunner.AddCommandHandler<string, string, float>("set_fmod_parameter", SetFmodParameter);
    }

    private void PlayFmodOneShot(string eventPath)
    {
        RuntimeManager.PlayOneShot(eventPath);
    }

    private void PlayFmodInstance(string eventPath)
    {
        // Reuse if already playing
        if (instances.TryGetValue(eventPath, out var existing) && existing.isValid())
        {
            existing.getPlaybackState(out var st);
            if (st == PLAYBACK_STATE.PLAYING || st == PLAYBACK_STATE.STARTING)
                return;
        }

        var inst = RuntimeManager.CreateInstance(eventPath);
        if (!inst.isValid())
        {
            Debug.LogWarning($"[YarnFmodTrigger] Invalid FMOD event: {eventPath}");
            return;
        }

        var result = inst.start();
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogWarning($"[YarnFmodTrigger] start() failed: {result} ({eventPath})");
            inst.release();
            inst.clearHandle();
            return;
        }

        // Keep alive (don't release)
        instances[eventPath] = inst;
    }

    private void StopFmodInstance(string eventPath)
    {
        if (!instances.TryGetValue(eventPath, out var inst) || !inst.isValid())
            return;

        inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        inst.release();
        inst.clearHandle();

        instances.Remove(eventPath);
    }

    private void SetFmodParameter(string eventPath, string parameterName, float value)
    {
        // 1) Try GLOBAL parameter first
        var globalResult = RuntimeManager.StudioSystem.setParameterByName(parameterName, value);
        if (globalResult == FMOD.RESULT.OK)
            return;

        // 2) Otherwise set on the persistent event instance
        if (!instances.TryGetValue(eventPath, out var inst) || !inst.isValid())
        {
            // Ensure music is started so the parameter matters
            PlayFmodInstance(eventPath);

            if (!instances.TryGetValue(eventPath, out inst) || !inst.isValid())
            {
                Debug.LogWarning($"[YarnFmodTrigger] Can't set '{parameterName}': no instance for {eventPath}");
                return;
            }
        }

        var r = inst.setParameterByName(parameterName, value);
        if (r != FMOD.RESULT.OK)
            Debug.LogWarning($"[YarnFmodTrigger] Event param set failed: {r} ({eventPath}/{parameterName})");
    }

    private void OnDisable() => StopAll(immediate: true);
    private void OnDestroy() => StopAll(immediate: true);

    private void StopAll(bool immediate)
    {
        foreach (var kv in instances)
        {
            var inst = kv.Value;
            if (!inst.isValid())
                continue;

            inst.stop(immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            inst.release();
            inst.clearHandle();
        }
        instances.Clear();
    }
}
