#nullable enable

using UnityEngine;
using Yarn.Unity;
using FMODUnity;
using FMOD.Studio;
using System.Collections;
using System.Threading;
using Debug = UnityEngine.Debug;

public class FMODDialogueView : DialoguePresenterBase
{
    [Header("References")]
    [SerializeField] private FMODLineProvider? fmodLineProvider;

    [Header("Audio Anchor")]
    [Tooltip("If VO events are 3D, set this to your player/camera/audio-listener anchor. If empty, falls back to this GameObject.")]
    [SerializeField] private GameObject? audioAnchor;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private EventInstance instance;
    private Coroutine? waitCoroutine;

    private void Awake()
    {
        if (fmodLineProvider == null)
            fmodLineProvider = FindFirstObjectByType<FMODLineProvider>();

        if (audioAnchor == null && Camera.main != null)
            audioAnchor = Camera.main.gameObject;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        StopPlayback(allowFadeOut: true);
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        StopPlayback(allowFadeOut: true);
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine dialogueLine, LineCancellationToken cancellationToken)
    {
        if (log) Debug.Log($"[FMODDialogueView] Line {dialogueLine.TextID}");

        StopPlayback(allowFadeOut: true);

        if (fmodLineProvider == null)
        {
            Debug.LogError("[FMODDialogueView] Missing FMODLineProvider.");
            return YarnTask.CompletedTask;
        }

        if (!fmodLineProvider.TryGetFmodEvent(dialogueLine.TextID, out var fmodEvent) || fmodEvent.IsNull)
        {
            if (log) Debug.LogWarning($"[FMODDialogueView] No event for {dialogueLine.TextID}");
            return YarnTask.CompletedTask;
        }

        instance = RuntimeManager.CreateInstance(fmodEvent);

        if (!instance.isValid())
        {
            Debug.LogError("[FMODDialogueView] Invalid FMOD instance.");
            return YarnTask.CompletedTask;
        }

        var anchor = audioAnchor != null ? audioAnchor : gameObject;

        RuntimeManager.AttachInstanceToGameObject(instance, anchor);

        instance.setVolume(1f);

        var result = instance.start();

        if (log)
            Debug.Log($"[FMODDialogueView] start() -> {result}");

        if (result != FMOD.RESULT.OK)
        {
            StopPlayback(allowFadeOut: false);
            return YarnTask.CompletedTask;
        }

        var tcs = new YarnTaskCompletionSource();

        waitCoroutine = StartCoroutine(
            WaitUntilFinished(tcs, cancellationToken)
        );

        return tcs.Task;
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(
        DialogueOption[] options,
        CancellationToken cancellationToken
    )
    {
        return YarnTask.FromResult<DialogueOption?>(null);
    }

    private IEnumerator WaitUntilFinished(
        YarnTaskCompletionSource tcs,
        LineCancellationToken cancellationToken
    )
    {
        while (instance.isValid())
        {
            // IMPORTANT:
            // Do NOT cancel the YarnTask.
            // Just stop audio and complete normally.
            if (cancellationToken.IsNextLineRequested)
            {
                if (log)
                    Debug.Log("[FMODDialogueView] Next line requested: stopping VO and completing task.");

                StopPlayback(allowFadeOut: false);

                tcs.TrySetResult();

                yield break;
            }

            instance.getPlaybackState(out var state);

            if (state != PLAYBACK_STATE.PLAYING &&
                state != PLAYBACK_STATE.STARTING)
            {
                break;
            }

            yield return null;
        }

        StopPlayback(allowFadeOut: true);

        tcs.TrySetResult();
    }

    private void StopPlayback(bool allowFadeOut)
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (!instance.isValid())
            return;

        instance.stop(
            allowFadeOut
                ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
                : FMOD.Studio.STOP_MODE.IMMEDIATE
        );

        instance.release();
        instance.clearHandle();
    }

    private void OnDisable()
    {
        StopPlayback(allowFadeOut: false);
    }

    private void OnDestroy()
    {
        StopPlayback(allowFadeOut: false);
    }
}