using UnityEngine;

public class OverLayCameraUpdateSize : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError($"No camera found on {gameObject.name}");
            return;
        }
    }
    void LateUpdate()
    {
        if (Camera.main == null) return;
        if (cam == null) return;
        cam.orthographicSize = Camera.main.orthographicSize; //update the size of the overlay cam to match the main cam.

    }
}
