using Platformer.Mechanics;
using Unity.Cinemachine;
using UnityEngine;

public class CameraHelper : MonoBehaviour
{
    private CinemachineCamera cineCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var playerController = FindAnyObjectByType<PlayerController>();
        if (TryGetComponent(out cineCamera))
        {
            if (playerController != null)
            {
                cineCamera.Follow = playerController.transform;
            }
            else
                Debug.LogWarning("No player controller found");

        }
        else
            Debug.LogWarning("No cinemachine found");

    }
}
