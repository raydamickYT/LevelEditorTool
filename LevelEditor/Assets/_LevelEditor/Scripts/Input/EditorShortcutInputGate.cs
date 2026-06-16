using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Blocks editor shortcuts while UI Toolkit text fields are focused or a modal popup is open.
/// </summary>
public static class EditorShortcutInputGate
{
    public static bool AreShortcutsBlocked =>
        IsAnyTextInputFocused() || EditorPopupService.IsAnyModalVisible();

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
