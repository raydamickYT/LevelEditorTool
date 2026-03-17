using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class is used for managing all events and actions. Since it's not a monobehaviour it doesn't need to be added to a scene, you can just reference it.
/// 
/// Creating Delegates:
/// Delegates allow you to add any kind of extra data to the event call. for example: you want to send 2 ints to the other functions. you can do that with a Delegate.
/// EventManager.Instance.AddDelegateListener("ExampleDelegate", (Action<int, int>)ExampleVoid); (The limit is about 4-6 parameters)
/// void ExampleVoid(int t, int i) {}
/// 
/// If you want to add more parameters it's adviced to make a seperate object and assign that.
/// eg
/// public class EGClass
/// {
///     public int t;
///     public int B;
///     public String something;
/// }
/// after which you just give the delegate an Action<EGClass>
/// 
/// Creating Unity Actions:
/// You can also just reference some actions. this is similar to the delegates but it doesn't allow you to send over any extra data, this'll just trigger whatever function is listening
/// to your action.
/// EventManager.Instance.AddUnityEventListener("TestUnityEvent", test);
/// void Test(){}
/// 
/// Triggering is as simple as referencing if you want to trigger a delegate or a unity event and adding the name
/// Eventmanager.Instance.TriggerUnityEvent("TestUnityEvent");
/// Eventmanager.Instance.TriggerDelegate("ExampleDelegate", 1, 2);
/// with a custom class:
/// Eventmanager.Instance.TriggerDelegate("ExampleDelegate", new EGClass{ ... });
/// </summary>

public class EventManager
{
    private static EventManager _instance;
    public static EventManager Instance => _instance ??= new EventManager();

    public List<string> UnityEventsNamesList = new List<string>();
    public List<string> DelegatesNamesList = new List<string>();

    private Dictionary<string, UnityEvent> unityEventDictionary = new Dictionary<string, UnityEvent>();
    public Dictionary<string, UnityEvent> UnityEventDictionary => unityEventDictionary;

    private Dictionary<string, Delegate> delegateDictionary = new Dictionary<string, Delegate>();
    public Dictionary<string, Delegate> DelegateDictionary => delegateDictionary;

    private EventManager()
    {
        Initialize();
    } //private constructor to prevent multiple instances

    private void Initialize()
    {
        unityEventDictionary = new Dictionary<string, UnityEvent>();
        foreach (string eventName in UnityEventsNamesList)
        {
            // Debug.Log($"added event {eventName}");
            unityEventDictionary[eventName] = new UnityEvent(); // Make a new event for each name.
        }

        foreach (string delegateName in DelegatesNamesList)
        {
            delegateDictionary[delegateName] = null; //In the Evenlistener.cs you can assign what kind of delegate you'd like this to be.
        }
    }
    // Function to remove all listeners from UnityEvent and Delegate dictionaries
    public void RemoveAllListeners()
    {
        // Remove listeners from UnityEvent dictionary
        foreach (var unityEventPair in unityEventDictionary)
        {
            unityEventPair.Value.RemoveAllListeners(); // Remove all UnityEvent listeners
        }

        // Remove listeners from Delegate dictionary
        Dictionary<string, Delegate> newDelegateDictionary = new Dictionary<string, Delegate>();
        foreach (var delegatePair in delegateDictionary)
        {
            if (delegatePair.Value != null)
            {
                Delegate[] invocationList = delegatePair.Value.GetInvocationList();
                foreach (var del in invocationList)
                {
                    if (!newDelegateDictionary.ContainsKey(delegatePair.Key))
                    {
                        newDelegateDictionary[delegatePair.Key] = delegatePair.Value;
                    }
                    newDelegateDictionary[delegatePair.Key] = Delegate.Remove(newDelegateDictionary[delegatePair.Key], del);
                }
            }
        }
        delegateDictionary = newDelegateDictionary;

        Debug.Log("All listeners have been removed from UnityEvents and Delegates.");
    }

    public void TriggerUnityEvent(string eventName)
    {
        if (unityEventDictionary.ContainsKey(eventName))
        {
            UnityEvent unityEvent = unityEventDictionary[eventName];

            // Debug.Log($"triggered event {unityEvent.GetPersistentEventCount()}");
            unityEventDictionary[eventName].Invoke();
        }
    }

    public void AddUnityEventListener(string eventName, UnityAction listener)
    {
        if (!unityEventDictionary.ContainsKey(eventName)) //if the dict doesn't contain the key
        {
            // Debug.Log($"added new event {eventName}, {listener}");
            unityEventDictionary[eventName] = new UnityEvent(); //make a new key
        }
        // Debug.Log($"key already exists, overwriting {eventName}");
        unityEventDictionary[eventName].AddListener(listener);
        UnityEvent unityEvent = unityEventDictionary[eventName];

        // Debug.Log($"made event {unityEvent.GetPersistentEventCount()}");
    }

    // Trigger delegate with any number of parameters
    public void TriggerDelegate(string eventName, params object[] parameters)
    {
        if (delegateDictionary.ContainsKey(eventName))
        {
            // Debug.Log(eventName);
            Delegate eventDelegate = delegateDictionary[eventName];
            eventDelegate?.DynamicInvoke(parameters); // Use DynamicInvoke to pass any parameters
        }
    }

    // Add a listener that can accept any combination of parameters
    public void AddDelegateListener(string eventName, Delegate listener)
    {
        if (!delegateDictionary.ContainsKey(eventName))
        {
            delegateDictionary[eventName] = null;
        }
        delegateDictionary[eventName] = Delegate.Combine(delegateDictionary[eventName], listener);
    }
}
