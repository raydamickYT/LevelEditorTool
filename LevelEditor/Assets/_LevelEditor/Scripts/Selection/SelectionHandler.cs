using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This class is responsible for handling any events that call on the selection controller. In going and out going.
/// </summary>

public class SelectionHandler : MonoBehaviour
{
    public Camera cam;
    private selectionController selectionController;
    private Coroutine subscribeRoutine;

    void Awake()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning($"No cam found on {gameObject.name} ");
        }

        selectionController = new selectionController(cam);

        EventManager.Instance.AddDelegateListener(SelectionEvents.RegisterToSelectionController, (Action<GameObject, SelectableTargetData>)HandleRegister);
        EventManager.Instance.AddDelegateListener(SelectionEvents.DeRegisterToSelectionController, (Action<GameObject>)HandleDeregister);
        EventManager.Instance.AddDelegateListener(SelectionEvents.ReplaceSelectionWithObject, (Action<List<GameObject>>)ReplaceSelection);
        EventManager.Instance.AddDelegateListener(SelectionEvents.OnTrySelection, (Action<GameObject>)OnTrySelection);

        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)OnDeleteTriggered);
    }

    void Update()
    {
        selectionController?.TickSelectionInput();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        subscribeRoutine = StartCoroutine(waitForInputHandler());
    }

    void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        if (InputHandler.Instance != null)
            InputHandler.Instance.OnLeftMouseButtonEvent -= OnLeftMouseButtonEvent;

        selectionController?.ClearDict();
    }

    private IEnumerator waitForInputHandler()
    {
        yield return new WaitUntil(() => InputHandler.Instance != null);
        InputHandler.Instance.OnLeftMouseButtonEvent += OnLeftMouseButtonEvent;
    }


    void OnLeftMouseButtonEvent(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            selectionController?.OnStartLeftClick();
        }

        if (context.canceled)
        {
            selectionController?.OnStopLeftClick();
        }
    }
    private void ReplaceSelection(List<GameObject> gameObjects)
    => selectionController?.ReplaceSelection(gameObjects);

    private void HandleRegister(GameObject obj, SelectableTargetData data)
    => selectionController?.Register(obj, data);

    private void HandleDeregister(GameObject obj)
        => selectionController?.Deregister(obj);

    private void OnTrySelection(GameObject gameObject)
    => selectionController?.TrySelect(gameObject);

    private void OnDeleteTriggered(EditorCommand editorCommand)
    {
        if (editorCommand == EditorCommand.Delete)
        {
            selectionController?.TryDeleteSelected();
        }
    }
}

/// <summary>
/// All the events we use for selection in a class with strings to prevent typos and make it easier to find where they're used. If an event needs to pass data, we can add a new one with the appropriate parameters, but for now these are the ones we need.
/// </summary>
public static class SelectionEvents
{
    public const string RegisterToSelectionController = "OnRegisterToSelectionController";
    public const string DeRegisterToSelectionController = "OnDeRegisterToSelectionController";
    public const string ShowSelectionBox = "ShowSelectionBox";
    public const string UpdateSelectionBox = "UpdateSelectionBox";
    public const string HideSelectionBox = "HideSelectionBox";
    public const string OnSelectionChanged = "OnSelectionChanged";
    public const string OnTrySelection = "OnTrySelection";
    public const string ReplaceSelectionWithObject = "ReplaceSelectionWithObject";
    
    //signals
    public static Func<Rect> FinalizeSelectionRect;

}