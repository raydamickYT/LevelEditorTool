using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BackgroundGridController : MonoBehaviour
{
    [Tooltip("Extra margin to ensure the grid covers the entire view")]
    public float EdgeMargin = 2f;
    private Camera cam;
    private bool gridNeedsResize;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        cam = Camera.main;
        if(cam == null)
        {
            Debug.LogError("Main Camera not found. Please ensure there is a camera in the scene tagged as 'MainCamera'.");
        }
        EventManager.Instance.AddUnityEventListener("OnCameraZoom", () => gridNeedsResize = true);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(!gridNeedsResize) return;

        var viewHeight = 2 * cam.orthographicSize + EdgeMargin;
        var viewWidth = viewHeight * cam.aspect;
        Vector3 newScale = new Vector3(viewWidth, viewHeight, 1);

        gameObject.transform.localScale = newScale;

        gridNeedsResize = false;
    }
}
