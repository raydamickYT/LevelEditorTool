using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Right-click context menu for scene-level editor options.
/// The document stays hidden until the menu opens so it never blocks scene clicks.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class EditorSceneContextMenuController : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    [SerializeField] int sortingOrder = 33000;

    VisualElement documentRoot;
    VisualElement menuPanel;
    Toggle showAllCollisionBoxesToggle;
    Coroutine subscribeRoutine;
    bool inputSubscribed;

    void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        BindElements();
        subscribeRoutine = StartCoroutine(WaitForInputHandler());
    }

    void OnDisable()
    {
        if (subscribeRoutine != null)
        {
            StopCoroutine(subscribeRoutine);
            subscribeRoutine = null;
        }

        UnsubscribeInput();
        PickColliderOutlineSettings.Changed -= OnOutlineSettingsChanged;
        HideMenu();
    }

    IEnumerator WaitForInputHandler()
    {
        yield return new WaitUntil(() => InputHandler.Instance != null);
        SubscribeInput();
    }

    void SubscribeInput()
    {
        if (inputSubscribed || InputHandler.Instance == null)
            return;

        InputHandler.Instance.onRightClickEvent += OnRightClick;
        InputHandler.Instance.TriggerSelectionCommand += OnLeftClick;
        inputSubscribed = true;
    }

    void UnsubscribeInput()
    {
        if (!inputSubscribed || InputHandler.Instance == null)
            return;

        InputHandler.Instance.onRightClickEvent -= OnRightClick;
        InputHandler.Instance.TriggerSelectionCommand -= OnLeftClick;
        inputSubscribed = false;
    }

    void BindElements()
    {
        if (uiDocument == null)
            return;

        TrySetSortingOrder();

        documentRoot = uiDocument.rootVisualElement;
        menuPanel = documentRoot.Q<VisualElement>("scene-context-menu");
        showAllCollisionBoxesToggle = documentRoot.Q<Toggle>("toggle-show-all-collision-boxes");

        if (documentRoot != null)
            documentRoot.pickingMode = PickingMode.Ignore;

        HideMenu();

        if (showAllCollisionBoxesToggle != null)
        {
            showAllCollisionBoxesToggle.SetValueWithoutNotify(PickColliderOutlineSettings.ShowAllCollisionBoxes);
            showAllCollisionBoxesToggle.RegisterValueChangedCallback(OnShowAllCollisionBoxesToggleChanged);
        }

        PickColliderOutlineSettings.Changed -= OnOutlineSettingsChanged;
        PickColliderOutlineSettings.Changed += OnOutlineSettingsChanged;
    }

    void TrySetSortingOrder()
    {
        if (uiDocument == null)
            return;

        System.Reflection.PropertyInfo property = typeof(UIDocument).GetProperty("sortingOrder");
        if (property == null || !property.CanWrite)
            return;

        property.SetValue(uiDocument, sortingOrder);
    }

    void OnRightClick(InputAction.CallbackContext context)
    {
        if (!context.performed || menuPanel == null)
            return;

        if (IsPointerOverEditorUi())
            return;

        ShowAtPointer();
    }

    void OnLeftClick(SelectionCommand command, InputAction.CallbackContext context)
    {
        if (!context.started || menuPanel == null || !IsMenuVisible())
            return;

        if (IsPointerOverMenuPanel())
            return;

        HideMenu();
    }

    void ShowAtPointer()
    {
        if (Mouse.current == null || menuPanel == null || documentRoot == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 panelPosition = ScreenToPanelPosition(screenPosition, documentRoot);
        menuPanel.style.left = panelPosition.x;
        menuPanel.style.top = panelPosition.y;
        menuPanel.pickingMode = PickingMode.Position;
        menuPanel.style.display = DisplayStyle.Flex;
        documentRoot.style.display = DisplayStyle.Flex;
        menuPanel.BringToFront();
    }

    void HideMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.style.display = DisplayStyle.None;
            menuPanel.pickingMode = PickingMode.Ignore;
        }

        if (documentRoot != null)
            documentRoot.style.display = DisplayStyle.None;
    }

    bool IsMenuVisible()
        => menuPanel != null && menuPanel.resolvedStyle.display != DisplayStyle.None;

    bool IsPointerOverMenuPanel()
    {
        if (Mouse.current == null || menuPanel?.panel == null || documentRoot == null)
            return false;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 panelPosition = ScreenToPanelPosition(screenPosition, documentRoot);
        VisualElement picked = menuPanel.panel.Pick(panelPosition);
        return picked != null && menuPanel.Contains(picked);
    }

    void OnShowAllCollisionBoxesToggleChanged(ChangeEvent<bool> evt)
        => PickColliderOutlineSettings.SetShowAllCollisionBoxes(evt.newValue);

    void OnOutlineSettingsChanged()
    {
        if (showAllCollisionBoxesToggle != null)
            showAllCollisionBoxesToggle.SetValueWithoutNotify(PickColliderOutlineSettings.ShowAllCollisionBoxes);
    }

    static bool IsPointerOverEditorUi()
    {
        if (UIHelper.IsPointerOverUI())
            return true;

        if (Mouse.current == null)
            return false;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];
            if (document == null || document.gameObject.GetComponent<EditorSceneContextMenuController>() != null)
                continue;

            VisualElement root = document.rootVisualElement;
            if (root == null || root.panel == null || root.resolvedStyle.display == DisplayStyle.None)
                continue;

            Vector2 panelPosition = ScreenToPanelPosition(screenPosition, root);
            VisualElement picked = root.panel.Pick(panelPosition);
            if (picked == null || picked == root)
                continue;

            if (picked.pickingMode == PickingMode.Ignore)
                continue;

            return true;
        }

        return false;
    }

    static Vector2 ScreenToPanelPosition(Vector2 screenPosition, VisualElement root)
    {
        Rect panelBounds = root != null
            ? root.worldBound
            : new Rect(0f, 0f, Screen.width, Screen.height);

        float width = Mathf.Max(1f, panelBounds.width);
        float height = Mathf.Max(1f, panelBounds.height);

        return new Vector2(
            panelBounds.xMin + (screenPosition.x / Mathf.Max(1f, Screen.width)) * width,
            panelBounds.yMin + ((Screen.height - screenPosition.y) / Mathf.Max(1f, Screen.height)) * height);
    }
}
