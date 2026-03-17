using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// With just this base you can specify 1 custom function and it'll be linked to the event.
/// you can add more to that if you'd like
/// </summary> 
public abstract class UnityEventListener : EventListener
{
    [HideInInspector]
    public int selectedEventIndex1 = 0; // Selected event dropdown

    public override void Initialize()
    {
        // Zorg ervoor dat het event wordt gekoppeld bij het starten
        if (EventManager.Instance != null && EventManager.Instance.UnityEventsNamesList.Count > 0)
        {
            // Verbind het geselecteerde event met de functie. Pakt hem hier gelijk uit de lijst van de eventmanager
            // UnityEvent selectedEvent = EventManager.Instance.events[selectedEventIndex].unityEvent;

            //maar het moet uit de dictionary komen
            string eventName = EventManager.Instance.UnityEventsNamesList[selectedEventIndex];

            Debug.Log($"init {eventName}");
            EventManager.Instance.AddUnityEventListener(eventName, EventFunction); //link the unity event to the specified event
        }
    }
    public override void EventFunction()
    {
        throw new System.NotImplementedException();
    }

}
