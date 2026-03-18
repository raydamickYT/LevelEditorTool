using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BackgroundGridController : MonoBehaviour
{
    public GameObject BackgroundGridParent;
    public GameObject BackgroundGrid; //prefab of the grid
    public Camera TargetCam;
    private bool isPanning;
    private Vector3 lastMouseWorldPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(BackgroundGrid == null)
        {
            Debug.LogError($"BackgroundGrid is not assigned in the inspector for {gameObject.name}.");
        }
        if(TargetCam == null)
        {
            Debug.LogError($"TargetCam is not assigned in the inspector for {gameObject.name}.");
            return;
        }

        EventManager.Instance.AddDelegateListener("OnMiddleMouseButton", (Action<InputAction.CallbackContext>)OnMoveCamera);
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current == null) return;

        if (!isPanning) return;

        Vector3 currentMouseWorldPos = GetMouseWorldPosition();
        Vector3 delta = lastMouseWorldPos - currentMouseWorldPos;

        BackgroundGrid.transform.position += delta;
        lastMouseWorldPos = GetMouseWorldPosition();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = TargetCam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, TargetCam.nearClipPlane));
        worldPos.z = 0f;
        return worldPos;
    }

    public void OnMoveCamera(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isPanning = true;
            lastMouseWorldPos = GetMouseWorldPosition();
        }
        if (context.canceled) isPanning = false;
    }
}
