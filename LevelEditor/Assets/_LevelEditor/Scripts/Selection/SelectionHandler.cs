using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

        EventManager.Instance.AddDelegateListener("OnRegisterToSelectionController", (Action<GameObject, SelectableTargetData>)HandleRegister);
        EventManager.Instance.AddDelegateListener("OnDeRegisterToSelectionController", (Action<GameObject>)HandleDeregister);
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
        if (context.performed)
        {
            if (RaycastHelper.IsClickingOnLayer(cam, LayerMask.GetMask("GizmoHandle")))
                return;
            selectionController?.HandleLeftClick();
        }
    }

    private void HandleRegister(GameObject obj, SelectableTargetData data)
    => selectionController?.Register(obj, data);

    private void HandleDeregister(GameObject obj)
        => selectionController?.Deregister(obj);
}