using System;
using System.Collections;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit notifications: blocking popups for warnings/errors and a lightweight toast for frequent feedback (e.g. save).
/// Add this to a GameObject with a UIDocument using PopupWindow.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class EditorPopupService : MonoBehaviour
{
    public static EditorPopupService Instance { get; private set; }

    [SerializeField] UIDocument uiDocument;
    [SerializeField] int sortingOrder = 32000;
    [SerializeField] float defaultToastDurationSeconds = 2.5f;

    VisualElement overlay;
    Label icon;
    Label title;
    Label message;
    ScrollView detailsScroll;
    Label details;
    Button okButton;
    VisualElement toastRoot;
    Label toastLabel;
    IVisualElementScheduledItem toastHideSchedule;
    Coroutine subscribeInputRoutine;

    VisualElement saveProjectOverlay;
    Label saveProjectTitle;
    Label saveProjectParentPath;
    TextField saveProjectNameField;
    Button saveProjectSaveButton;
    Button saveProjectBrowseButton;
    Button saveProjectCancelButton;
    Action<string, string> pendingSaveProjectCallback;
    string saveProjectParentFolder;

    VisualElement confirmOverlay;
    Label confirmTitle;
    Label confirmMessage;
    Button confirmOkButton;
    Button confirmCancelButton;
    Action pendingConfirmCallback;
    Action pendingConfirmCancelCallback;
    bool dialogHandlersBound;

    public static void ShowSaveProjectFolderDialog(
        string parentFolder,
        string defaultProjectName,
        string dialogTitle,
        Action<string, string> onSave)
    {
        if (Instance == null)
        {
            Debug.LogWarning("Save project dialog requested before EditorPopupService exists.");
            onSave?.Invoke(parentFolder, defaultProjectName);
            return;
        }

        Instance.ShowSaveProjectFolderDialogInternal(parentFolder, defaultProjectName, dialogTitle, onSave);
    }

    public static void ShowConfirmDialog(
        string titleText,
        string messageText,
        string confirmButtonText,
        Action onConfirm,
        Action onCancel,
        string cancelButtonText = "Cancel")
    {
        if (Instance == null)
        {
            Debug.LogWarning($"Confirm dialog requested before EditorPopupService exists: {titleText}");
            return;
        }

        Instance.ShowConfirmDialogInternal(titleText, messageText, confirmButtonText, cancelButtonText, onConfirm, onCancel);
    }

    public static bool IsAnyModalVisible()
        => Instance != null && Instance.IsAnyModalVisibleInternal();

    public static void ShowInfo(string title, string message, string details = null)
        => Show(PopupSeverity.Info, title, message, details);

    public static void ShowWarning(string title, string message, string details = null)
        => Show(PopupSeverity.Warning, title, message, details);

    public static void ShowError(string title, string message, string details = null)
        => Show(PopupSeverity.Error, title, message, details);

    public static void ShowToast(string message, float durationSeconds = -1f)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"Toast requested before EditorPopupService exists: {message}");
            return;
        }

        Instance.ShowToastInternal(message, durationSeconds);
    }

    /// <summary>Runs after the save toast has time to appear (e.g. before quit, open, or clearing the scene).</summary>
    public static void RunAfterSaveFeedback(Action action, float delaySeconds = 0.45f)
    {
        if (Instance == null)
        {
            action?.Invoke();
            return;
        }

        Instance.RunAfterSaveFeedbackInternal(action, delaySeconds);
    }

    static void Show(PopupSeverity severity, string title, string message, string details)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"Popup requested before EditorPopupService exists: {title} - {message}");
            return;
        }

        Instance.ShowInternal(severity, title, message, details);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        TrySetSortingOrder();
    }

    void OnEnable()
    {
        BindElements();
        BindDialogHandlers();

        if (overlay != null)
        {
            overlay.focusable = true;
            overlay.RegisterCallback<KeyDownEvent>(OnPopupKeyDown);
        }

        if (saveProjectOverlay != null)
            saveProjectOverlay.RegisterCallback<KeyDownEvent>(OnSaveProjectKeyDown);

        if (confirmOverlay != null)
            confirmOverlay.RegisterCallback<KeyDownEvent>(OnConfirmKeyDown);

        subscribeInputRoutine = StartCoroutine(WaitForInputHandler());
    }

    IEnumerator WaitForInputHandler()
    {
        yield return new WaitUntil(() => InputHandler.Instance != null);

        InputHandler.Instance.OnCancelEvent += OnDismissInput;
        InputHandler.Instance.onSubmitEvent += OnDismissInput;
    }

    void OnDismissInput(InputAction.CallbackContext context)
    {
        if (!context.started || !IsAnyModalVisibleInternal())
            return;

        if (IsSaveProjectDialogVisible())
            HideSaveProjectDialog();
        else if (IsConfirmDialogVisible())
            HideConfirmDialog();
        else
            Hide();
    }

    void OnDisable()
    {
        CancelToastHideSchedule();

        if (subscribeInputRoutine != null)
        {
            StopCoroutine(subscribeInputRoutine);
            subscribeInputRoutine = null;
        }

        if (InputHandler.Instance != null)
        {
            InputHandler.Instance.OnCancelEvent -= OnDismissInput;
            InputHandler.Instance.onSubmitEvent -= OnDismissInput;
        }

        if (overlay != null)
            overlay.UnregisterCallback<KeyDownEvent>(OnPopupKeyDown);

        if (saveProjectOverlay != null)
            saveProjectOverlay.UnregisterCallback<KeyDownEvent>(OnSaveProjectKeyDown);

        if (confirmOverlay != null)
            confirmOverlay.UnregisterCallback<KeyDownEvent>(OnConfirmKeyDown);

        UnbindDialogHandlers();
    }

    void OnDestroy()
    {
        CancelToastHideSchedule();

        if (Instance == this)
            Instance = null;
    }

    void BindElements()
    {
        if (uiDocument == null)
            return;

        VisualElement root = uiDocument.rootVisualElement;
        overlay = root.Q<VisualElement>("popup-overlay");
        icon = root.Q<Label>("popup-icon");
        title = root.Q<Label>("popup-title");
        message = root.Q<Label>("popup-message");
        detailsScroll = root.Q<ScrollView>("popup-details-scroll");
        details = root.Q<Label>("popup-details");
        okButton = root.Q<Button>("popup-ok-button");
        toastRoot = root.Q<VisualElement>("status-toast");
        toastLabel = root.Q<Label>("status-toast-label");
        saveProjectOverlay = root.Q<VisualElement>("save-project-overlay");
        saveProjectTitle = root.Q<Label>("save-project-title");
        saveProjectParentPath = root.Q<Label>("save-project-parent-path");
        saveProjectBrowseButton = root.Q<Button>("save-project-browse-button");
        saveProjectNameField = root.Q<TextField>("save-project-name-field");
        saveProjectSaveButton = root.Q<Button>("save-project-save-button");
        saveProjectCancelButton = root.Q<Button>("save-project-cancel-button");
        confirmOverlay = root.Q<VisualElement>("confirm-overlay");
        confirmTitle = root.Q<Label>("confirm-title");
        confirmMessage = root.Q<Label>("confirm-message");
        confirmOkButton = root.Q<Button>("confirm-ok-button");
        confirmCancelButton = root.Q<Button>("confirm-cancel-button");

        if (overlay != null)
            overlay.style.display = DisplayStyle.None;

        if (saveProjectOverlay != null)
            saveProjectOverlay.style.display = DisplayStyle.None;

        if (confirmOverlay != null)
            confirmOverlay.style.display = DisplayStyle.None;

        if (toastRoot != null)
            toastRoot.style.display = DisplayStyle.None;
    }

    void BindDialogHandlers()
    {
        if (dialogHandlersBound)
            return;

        if (okButton != null)
            okButton.clicked += Hide;

        if (saveProjectSaveButton != null)
            saveProjectSaveButton.clicked += OnSaveProjectSaveClicked;

        if (saveProjectCancelButton != null)
            saveProjectCancelButton.clicked += HideSaveProjectDialog;

        if (saveProjectBrowseButton != null)
            saveProjectBrowseButton.clicked += OnSaveProjectBrowseClicked;

        if (confirmOkButton != null)
            confirmOkButton.clicked += OnConfirmOkClicked;

        if (confirmCancelButton != null)
            confirmCancelButton.clicked += HideConfirmDialog;

        dialogHandlersBound = true;
    }

    void UnbindDialogHandlers()
    {
        if (!dialogHandlersBound)
            return;

        if (okButton != null)
            okButton.clicked -= Hide;

        if (saveProjectSaveButton != null)
            saveProjectSaveButton.clicked -= OnSaveProjectSaveClicked;

        if (saveProjectCancelButton != null)
            saveProjectCancelButton.clicked -= HideSaveProjectDialog;

        if (saveProjectBrowseButton != null)
            saveProjectBrowseButton.clicked -= OnSaveProjectBrowseClicked;

        if (confirmOkButton != null)
            confirmOkButton.clicked -= OnConfirmOkClicked;

        if (confirmCancelButton != null)
            confirmCancelButton.clicked -= HideConfirmDialog;

        dialogHandlersBound = false;
    }

    void ShowToastInternal(string messageText, float durationSeconds)
    {
        BindElements();

        if (toastRoot == null || toastLabel == null)
        {
            Debug.LogWarning($"Toast UXML is missing status-toast: {messageText}");
            return;
        }

        CancelToastHideSchedule();

        toastLabel.text = string.IsNullOrWhiteSpace(messageText) ? "Saved" : messageText;
        toastRoot.style.display = DisplayStyle.Flex;
        toastRoot.BringToFront();

        float duration = durationSeconds > 0f ? durationSeconds : defaultToastDurationSeconds;
        toastHideSchedule = toastRoot.schedule.Execute(HideToast).StartingIn((long)(duration * 1000f));
    }

    void RunAfterSaveFeedbackInternal(Action action, float delaySeconds)
    {
        VisualElement root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
        {
            action?.Invoke();
            return;
        }

        root.schedule.Execute(() => action?.Invoke()).StartingIn((long)(delaySeconds * 1000f));
    }

    void HideToast()
    {
        CancelToastHideSchedule();

        if (toastRoot != null)
            toastRoot.style.display = DisplayStyle.None;
    }

    void CancelToastHideSchedule()
    {
        toastHideSchedule?.Pause();
        toastHideSchedule = null;
    }

    void ShowSaveProjectFolderDialogInternal(string parentFolder, string defaultProjectName, string dialogTitle, Action<string, string> onSave)
    {
        BindElements();
        BindDialogHandlers();

        if (saveProjectOverlay == null || saveProjectNameField == null)
        {
            Debug.LogWarning("Save project dialog UXML is missing.");
            onSave?.Invoke(parentFolder, defaultProjectName);
            return;
        }

        pendingSaveProjectCallback = onSave;

        if (saveProjectTitle != null)
            saveProjectTitle.text = string.IsNullOrWhiteSpace(dialogTitle) ? "Save project" : dialogTitle;

        saveProjectParentFolder = Path.GetFullPath(parentFolder ?? string.Empty);
        if (saveProjectParentPath != null)
            saveProjectParentPath.text = saveProjectParentFolder;

        saveProjectNameField.SetValueWithoutNotify(string.IsNullOrWhiteSpace(defaultProjectName)
            ? "NewLevel"
            : defaultProjectName);

        saveProjectOverlay.style.display = DisplayStyle.Flex;
        saveProjectOverlay.BringToFront();
        saveProjectNameField.Focus();
    }

    void ShowConfirmDialogInternal(
        string titleText,
        string messageText,
        string confirmButtonText,
        string cancelButtonText,
        Action onConfirm,
        Action onCancel)
    {
        BindElements();
        BindDialogHandlers();

        if (confirmOverlay == null)
        {
            Debug.LogWarning($"Confirm dialog UXML is missing: {titleText}");
            return;
        }

        pendingConfirmCallback = onConfirm;
        pendingConfirmCancelCallback = onCancel;

        if (confirmTitle != null)
            confirmTitle.text = string.IsNullOrWhiteSpace(titleText) ? "Confirm" : titleText;

        if (confirmMessage != null)
            confirmMessage.text = messageText ?? string.Empty;

        if (confirmOkButton != null)
            confirmOkButton.text = string.IsNullOrWhiteSpace(confirmButtonText) ? "OK" : confirmButtonText;

        if (confirmCancelButton != null)
            confirmCancelButton.text = string.IsNullOrWhiteSpace(cancelButtonText) ? "Cancel" : cancelButtonText;

        confirmOverlay.style.display = DisplayStyle.Flex;
        confirmOverlay.BringToFront();
        confirmOkButton?.Focus();
    }

    void OnSaveProjectSaveClicked()
    {
        string projectName = saveProjectNameField != null ? saveProjectNameField.value : string.Empty;
        var callback = pendingSaveProjectCallback;
        string parent = saveProjectParentFolder;
        HideSaveProjectDialog();
        callback?.Invoke(parent, projectName);
    }

    void OnSaveProjectBrowseClicked()
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Choose parent folder for project", saveProjectParentFolder ?? string.Empty, false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        saveProjectParentFolder = Path.GetFullPath(paths[0]);
        if (saveProjectParentPath != null)
            saveProjectParentPath.text = saveProjectParentFolder;
    }

    void HideSaveProjectDialog()
    {
        if (saveProjectOverlay != null)
            saveProjectOverlay.style.display = DisplayStyle.None;

        pendingSaveProjectCallback = null;
    }

    void OnConfirmOkClicked()
    {
        Action callback = pendingConfirmCallback;
        pendingConfirmCallback = null;
        pendingConfirmCancelCallback = null;

        if (confirmOverlay != null)
            confirmOverlay.style.display = DisplayStyle.None;

        callback?.Invoke();
    }

    void HideConfirmDialog()
    {
        if (confirmOverlay != null)
            confirmOverlay.style.display = DisplayStyle.None;

        pendingConfirmCancelCallback?.Invoke();
        pendingConfirmCallback = null;
        pendingConfirmCancelCallback = null;
    }
    void OnSaveProjectKeyDown(KeyDownEvent evt)
    {
        if (!IsSaveProjectDialogVisible())
            return;

        if (evt.keyCode == KeyCode.Escape)
        {
            HideSaveProjectDialog();
            evt.StopPropagation();
            return;
        }

        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            OnSaveProjectSaveClicked();
            evt.StopPropagation();
        }
    }

    void OnConfirmKeyDown(KeyDownEvent evt)
    {
        if (!IsConfirmDialogVisible())
            return;

        if (evt.keyCode == KeyCode.Escape)
        {
            HideConfirmDialog();
            evt.StopPropagation();
            return;
        }

        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            OnConfirmOkClicked();
            evt.StopPropagation();
        }
    }
    void ShowInternal(PopupSeverity severity, string titleText, string messageText, string detailsText)
    {
        BindElements();
        BindDialogHandlers();

        if (overlay == null)
        {
            Debug.LogWarning($"Popup UXML is missing popup-overlay: {titleText} - {messageText}");
            return;
        }

        SetSeverity(severity);

        if (title != null)
            title.text = string.IsNullOrWhiteSpace(titleText) ? "Message" : titleText;

        if (message != null)
            message.text = messageText ?? string.Empty;

        bool hasDetails = !string.IsNullOrWhiteSpace(detailsText);
        if (detailsScroll != null)
            detailsScroll.style.display = hasDetails ? DisplayStyle.Flex : DisplayStyle.None;

        if (details != null)
            details.text = detailsText ?? string.Empty;

        overlay.style.display = DisplayStyle.Flex;
        overlay.BringToFront();
        okButton?.Focus();
    }

    void OnPopupKeyDown(KeyDownEvent evt)
    {
        if (!IsPopupVisible())
            return;

        if (evt.keyCode == KeyCode.Escape
            || evt.keyCode == KeyCode.Return)
        {
            Hide();
            evt.StopPropagation();
        }
        if (evt.keyCode == KeyCode.KeypadEnter)
            OnConfirmOkClicked();

    }

    bool IsAnyModalVisibleInternal()
        => IsPopupVisible() || IsSaveProjectDialogVisible() || IsConfirmDialogVisible();

    bool IsPopupVisible()
        => overlay != null && overlay.resolvedStyle.display == DisplayStyle.Flex;

    bool IsSaveProjectDialogVisible()
        => saveProjectOverlay != null && saveProjectOverlay.resolvedStyle.display == DisplayStyle.Flex;

    bool IsConfirmDialogVisible()
        => confirmOverlay != null && confirmOverlay.resolvedStyle.display == DisplayStyle.Flex;

    void Hide()
    {
        if (overlay != null)
            overlay.style.display = DisplayStyle.None;
    }

    void SetSeverity(PopupSeverity severity)
    {
        if (icon == null)
            return;

        icon.RemoveFromClassList("popup-icon-info");
        icon.RemoveFromClassList("popup-icon-warning");
        icon.RemoveFromClassList("popup-icon-error");

        switch (severity)
        {
            case PopupSeverity.Warning:
                icon.text = "!";
                icon.AddToClassList("popup-icon-warning");
                break;
            case PopupSeverity.Error:
                icon.text = "x";
                icon.AddToClassList("popup-icon-error");
                break;
            default:
                icon.text = "i";
                icon.AddToClassList("popup-icon-info");
                break;
        }
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
}

public enum PopupSeverity
{
    Info,
    Warning,
    Error
}
