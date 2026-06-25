using UnityEngine;
using UnityEngine.UIElements;

public static class LevelViewportFrameBootstrap
{
    const string UxmlResourcePath = "ViewportFrame/ViewportFramePanel";

    static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        if (Object.FindAnyObjectByType<LevelViewportFrameRenderer>() != null)
        {
            initialized = true;
            return;
        }

        GameObject host = new GameObject("LevelViewportFrame");
        host.AddComponent<LevelViewportFrameRenderer>();
        host.AddComponent<LevelViewportFrameInteraction>();

        VisualTreeAsset panelAsset = Resources.Load<VisualTreeAsset>(UxmlResourcePath);
        UIDocument referenceDocument = Object.FindAnyObjectByType<UIDocument>();
        if (panelAsset != null && referenceDocument != null)
        {
            GameObject panelHost = new GameObject("ViewportFramePanel");
            UIDocument panelDocument = panelHost.AddComponent<UIDocument>();
            panelDocument.panelSettings = referenceDocument.panelSettings;
            panelDocument.visualTreeAsset = panelAsset;
            panelHost.AddComponent<ViewportFramePanelView>();
        }
        else
        {
            Debug.LogWarning("Viewport frame panel could not be created. Missing UXML or UIDocument reference.");
        }

        initialized = true;
    }
}
