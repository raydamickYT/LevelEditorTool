using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Blocks editor shortcuts while UI Toolkit text fields are focused or a modal popup is open.
/// Also suppresses the letter key from UIToolkit text fields when a modifier shortcut was just triggered.
/// </summary>
public static class EditorShortcutInputGate
{
    static KeyCode? suppressedKey;
    static int suppressUntilFrame = -1;

    public static bool AreShortcutsBlocked =>
        IsAnyTextInputFocused() || EditorPopupService.IsAnyModalVisible();

    public static void SuppressShortcutTextInput(KeyCode key)
    {
        suppressedKey = key;
        suppressUntilFrame = Time.frameCount + 1;
    }

    public static bool ShouldSuppressTextInput(KeyDownEvent evt)
    {
        if (suppressedKey == null || Time.frameCount > suppressUntilFrame)
            return false;

        if (evt.keyCode != suppressedKey)
            return false;

        return evt.ctrlKey || evt.commandKey;
    }

    public static void ClearTextInputSuppression()
    {
        suppressedKey = null;
        suppressUntilFrame = -1;
    }

    static bool IsAnyTextInputFocused()
    {
        UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (UIDocument document in documents)
        {
            if (document == null)
                continue;

            FocusController focusController = document.rootVisualElement?.panel?.focusController;
            Focusable focused = focusController?.focusedElement;
            if (IsTextInputFocusedElement(focused))
                return true;
        }

        return false;
    }

    static bool IsTextInputFocusedElement(Focusable element)
    {
        return element is TextField
            or IntegerField
            or FloatField
            or DoubleField
            or LongField;
    }
}
