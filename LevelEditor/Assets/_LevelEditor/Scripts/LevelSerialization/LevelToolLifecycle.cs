using UnityEngine;

/// <summary>
/// Standalone tool lifecycle: no Unity Editor hooks. Resets session UserData when the application quits.
/// </summary>
public sealed class LevelToolLifecycle : MonoBehaviour
{
    static LevelToolLifecycle _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureHost()
    {
        if (FindAnyObjectByType<LevelToolLifecycle>() != null)
            return;

        GameObject host = new GameObject(nameof(LevelToolLifecycle));
        host.AddComponent<LevelToolLifecycle>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    void OnApplicationQuit()
    {
        AssetStorageService.ResetRuntimeWorkspace();
    }
}
