using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Yarn.Unity;

public class YarnTimelineController : MonoBehaviour
{
    [System.Serializable]
    public class TimelineEntry
    {
        public string id;
        public PlayableDirector director;
    }

    [Header("Timelines")]
    public List<TimelineEntry> timelines = new List<TimelineEntry>();

    private Dictionary<string, PlayableDirector> timelineDict;

    [Header("References")]
    public DialogueRunner dialogueRunner;

    void Awake()
    {
        // Build dictionary for fast lookup
        timelineDict = new Dictionary<string, PlayableDirector>();
        foreach (var t in timelines)
        {
            if (!timelineDict.ContainsKey(t.id) && t.director != null)
            {
                timelineDict.Add(t.id, t.director);
            }
        }
    }

    void Start()
    {
        dialogueRunner.AddCommandHandler<string>("play_timeline", PlayTimeline);
        dialogueRunner.AddCommandHandler<string>("pause_timeline", PauseTimeline);
        dialogueRunner.AddCommandHandler<string>("resume_timeline", ResumeTimeline);
        dialogueRunner.AddCommandHandler<string>("restart_timeline", RestartTimeline);
        dialogueRunner.AddCommandHandler<string>("play_timeline_wait", PlayTimelineAndWait);
    }

    PlayableDirector GetDirector(string id)
    {
        if (timelineDict.TryGetValue(id, out var director))
            return director;

        Debug.LogWarning($"Timeline '{id}' not found!");
        return null;
    }

    // =========================
    // BASIC CONTROLS
    // =========================

    void PlayTimeline(string id)
    {
        var director = GetDirector(id);
        if (director == null) return;

        if (director.time >= director.duration)
        {
            director.time = 0;
            director.Evaluate();
        }

        director.Play();
    }

    void PauseTimeline(string id)
    {
        var director = GetDirector(id);
        if (director == null) return;

        director.Pause();
    }

    void ResumeTimeline(string id)
    {
        var director = GetDirector(id);
        if (director == null) return;

        director.time += 0.001f;
        director.Play();
    }

    void RestartTimeline(string id)
    {
        var director = GetDirector(id);
        if (director == null) return;

        director.time = 0;
        director.Evaluate();
        director.Play();
    }

    // =========================
    // PLAY AND WAIT
    // =========================

    IEnumerator PlayTimelineAndWait(string id)
    {
        var director = GetDirector(id);
        if (director == null) yield break;

        if (director.time >= director.duration)
        {
            director.time = 0;
            director.Evaluate();
        }

        director.Play();

        while (director.state == PlayState.Playing)
        {
            yield return null;
        }

        director.Evaluate();
    }
}