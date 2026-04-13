using UnityEngine;
using UnityEngine.UI;

public class SelectionBoxVisual : SelectionBoxBase
{
    private Image image;

    public override void Start()
    {
        image = GetComponentInChildren<Image>(true);
        if (image == null)
        {
            Debug.LogError($"No image found in the children of {gameObject.name}. Please add one for the selection box to work.");
            return;
        }

        rectTransform = image.rectTransform;
        parentRectTransform = rectTransform.parent as RectTransform;

        if (rectTransform.gameObject.activeSelf)
            Hide();

        base.Start();
    }

    public override void SetupEvents()
    {
        base.SetupEvents();

        //UnityEvents
        EventManager.Instance.AddUnityEventListener(SelectionEvents.ShowSelectionBox, Show);
        EventManager.Instance.AddUnityEventListener(SelectionEvents.HideSelectionBox, Hide);
    }

    public override void UpdateBox(Vector2 startScreenPosition, Vector2 currentScreenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
    parentRectTransform,
    startScreenPosition,
    null,
    out Vector2 startLocalPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRectTransform,
            currentScreenPosition,
            null,
            out Vector2 currentLocalPosition);

        Vector2 min = Vector2.Min(startLocalPosition, currentLocalPosition);
        Vector2 max = Vector2.Max(startLocalPosition, currentLocalPosition);

        rectTransform.anchoredPosition = min;
        rectTransform.sizeDelta = max - min;
    }
}
