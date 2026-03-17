using UnityEngine;
using UnityEngine.Events;

public abstract class EventListener : MonoBehaviour
{
    [Header("Event")]
    [Tooltip("Select which event you want this object to listen to")]
    [HideInInspector]
    public int selectedEventIndex = -1; // Selected event dropdown, set it to -1 so that it has nothing selected by default.
    // public EventType eventType = EventType.UnityEvent;

    public abstract void Initialize();

    void Awake()
    {
        Initialize();
    }

    public abstract void EventFunction();
}
