using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// With just this base you can specify 1 custom function and it'll be linked to the event.
/// you can add more to that if you'd like
/// If you want to use actions with attributes or maybe func's then you'll have to write your own init function
/// </summary> 
public abstract class DelegateListener : EventListener
{
    public override void Initialize()
    {
        if (EventManager.Instance != null && EventManager.Instance.DelegateDictionary.Count > 0)
        {
            string eventName = EventManager.Instance.DelegatesNamesList[selectedEventIndex];

            EventManager.Instance.AddDelegateListener(eventName, new Action(EventFunction)); // by making the action here, you get control over what it need to be
        }
    }

    public override void EventFunction()
    {
        throw new NotImplementedException();
    }
}
