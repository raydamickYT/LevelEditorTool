using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Background auto-save with a shared interval timer reset by every trigger (interval, focus loss, manual save, etc.).
/// </summary>
public sealed class LevelProjectAutoSaveService : MonoBehaviour
{
    public const float DefaultIntervalSeconds = 300f;

    static LevelProjectAutoSaveService _instance;

    [SerializeField] float intervalSeconds = DefaultIntervalSeconds;

    readonly List<AutoSaveTriggerBase> _triggers = new();

    float _intervalDeadline;

    public static LevelProjectAutoSaveService Instance => _instance;

    public float IntervalSeconds => intervalSeconds;

    internal float IntervalDeadline => _intervalDeadline;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        RegisterDefaultTriggers();
        ResetIntervalTimer();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void Update()
    {
        for (int i = 0; i < _triggers.Count; i++)
            _triggers[i].OnUpdate();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        for (int i = 0; i < _triggers.Count; i++)
            _triggers[i].OnApplicationFocus(hasFocus);
    }

    /// <summary>Registers an additional trigger (e.g. custom editor events).</summary>
    public void RegisterTrigger(AutoSaveTriggerBase trigger)
    {
        trigger.Install(this);
        _triggers.Add(trigger);
    }

    /// <summary>Called by triggers and after any successful save to restart the interval countdown.</summary>
    public void ResetIntervalTimer()
    {
        _intervalDeadline = Time.unscaledTime + intervalSeconds;
    }

    /// <summary>Resets the interval timer when a save completed outside this service (manual save).</summary>
    public static void ResetIntervalTimerIfActive()
    {
        if (_instance != null)
            _instance.ResetIntervalTimer();
    }

    internal void NotifyTriggerFired(AutoSaveTriggerKind kind)
    {
        ResetIntervalTimer();

        if (!LevelProjectSession.HasOpenProject)
            return;

        if (!LevelProjectDirtyState.HasUnsavedChanges())
            return;

        LevelEditorFileMenuCommands.TrySaveOpenProjectSilent();
    }

    void RegisterDefaultTriggers()
    {
        RegisterTrigger(new IntervalAutoSaveTrigger());
        RegisterTrigger(new ApplicationFocusLostAutoSaveTrigger());
    }
}

public enum AutoSaveTriggerKind
{
    Interval,
    ApplicationFocusLost,
}

/// <summary>Base for auto-save triggers; override <see cref="OnUpdate"/> and/or <see cref="OnApplicationFocus"/>.</summary>
public abstract class AutoSaveTriggerBase
{
    protected LevelProjectAutoSaveService Service { get; private set; }

    public abstract AutoSaveTriggerKind Kind { get; }

    public void Install(LevelProjectAutoSaveService service) => Service = service;

    public virtual void OnUpdate() { }

    public virtual void OnApplicationFocus(bool hasFocus) { }
}

sealed class IntervalAutoSaveTrigger : AutoSaveTriggerBase
{
    public override AutoSaveTriggerKind Kind => AutoSaveTriggerKind.Interval;

    public override void OnUpdate()
    {
        if (Time.unscaledTime >= Service.IntervalDeadline)
            Service.NotifyTriggerFired(Kind);
    }
}

sealed class ApplicationFocusLostAutoSaveTrigger : AutoSaveTriggerBase
{
    public override AutoSaveTriggerKind Kind => AutoSaveTriggerKind.ApplicationFocusLost;

    public override void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            Service.NotifyTriggerFired(Kind);
    }
}
