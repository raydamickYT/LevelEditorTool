using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit popup for user-facing import/save/load warnings. Add this to a GameObject with a UIDocument using PopupWindow.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class EditorPopupService : MonoBehaviour
{
    public static EditorPopupService Instance { get; private set; }

    [SerializeField] UIDocument uiDocument;
    [SerializeField] int sortingOrder = 32000;

    VisualElement overlay;
    Label icon;
    Label title;
    Label message;
    ScrollView detailsScroll;
    Label details;
    Button okButton;

    public static void ShowInfo(string title, string message, string details = null)
        => Show(PopupSeverity.Info, title, message, details);

    public static void ShowWarning(string title, string message, string details = null)
        => Show(PopupSeverity.Warning, title, message, details);

    public static void ShowError(string title, string message, string details = null)
        => Show(PopupSeverity.Error, title, message, details);

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

        if (okButton != null)
            okButton.clicked += Hide;
    }

    void OnDisable()
    {
        if (okButton != null)
            okButton.clicked -= Hide;
    }

    void OnDestroy()
    {
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

        if (overlay != null)
            overlay.style.display = DisplayStyle.None;
    }

    void ShowInternal(PopupSeverity severity, string titleText, string messageText, string detailsText)
    {
        BindElements();

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
    }

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
