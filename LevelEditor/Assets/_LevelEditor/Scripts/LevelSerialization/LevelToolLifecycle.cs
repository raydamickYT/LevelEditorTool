using UnityEngine;


// Standalone tool lifecycle: no Unity Editor hooks. Resets session UserData when the application quits, so we can start with a clean slate next time.

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

        Application.wantsToQuit += OnWantsToQuit;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        Application.wantsToQuit -= OnWantsToQuit;
        if (_instance == this)
            _instance = null;
    }

    bool OnWantsToQuit()
    {
        if(LevelProjectDirtyState.HasUnsavedChanges())
        {
            EditorPopupService.ShowConfirmDialog(
                "Unsaved changes",
                "You have unsaved changes. Do you want to save them before quitting?",
                "Save and quit",
                () =>
                {
                    LevelEditorFileMenuCommands.SaveLevel();
                    if (!LevelProjectDirtyState.HasUnsavedChanges())
                        EditorPopupService.RunAfterSaveFeedback(Application.Quit);
                },
                () => Application.Quit(),
                "Discard changes"); // canceltext
            return false;
        }
        return true;
    }

    void OnApplicationQuit()
    {
        AssetStorageService.ResetRuntimeWorkspace();
    }
}
