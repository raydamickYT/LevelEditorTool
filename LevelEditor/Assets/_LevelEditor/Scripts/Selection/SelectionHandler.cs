using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionHandler : MonoBehaviour
{
    [SerializeField] private SelectionBoxView selectionBoxView;
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

        if(selectionBoxView == null)
        {
            Debug.LogError($"No selection box view assigned to {gameObject.name}, please assign one for the selection box to work.");
        }
        
        selectionController = new selectionController(cam, selectionBoxView);

        EventManager.Instance.AddDelegateListener("OnRegisterToSelectionController", (Action<GameObject, SelectableTargetData>)HandleRegister);
        EventManager.Instance.AddDelegateListener("OnDeRegisterToSelectionController", (Action<GameObject>)HandleDeregister);
    }

    void Start()
    {
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

        if(context.canceled)
        {
            selectionController?.OnStopLeftClick();
        }
    }

    private void HandleRegister(GameObject obj, SelectableTargetData data)
    => selectionController?.Register(obj, data);

    private void HandleDeregister(GameObject obj)
        => selectionController?.Deregister(obj);
}