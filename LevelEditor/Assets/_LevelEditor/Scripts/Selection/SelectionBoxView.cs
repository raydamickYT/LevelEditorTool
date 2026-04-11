using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectionBoxView : MonoBehaviour
{
    private RectTransform rectTransform, parentRectTransform;
    public Rect CurrentSelectionRect => rectTransform.rect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Image image = GetComponentInChildren<Image>(true);
        if (image == null)
        {
            Debug.LogError($"No image found in the children of {gameObject.name}. Please add one for the selection box to work.");
            return;
        }

        rectTransform ??= image.rectTransform;
        parentRectTransform ??= rectTransform.parent as RectTransform;


        if (rectTransform == null || parentRectTransform == null)
        {
            Debug.LogError($"No rectTransform found in the children or parent of {gameObject.name}. Please add one for the selection box to work.");
        }


        if (rectTransform.gameObject.activeSelf)
            Hide();

    }

    public void Show()
    {
        rectTransform.gameObject.SetActive(true);
    }

    public void UpdateBox(Vector2 startScreenPosition, Vector2 currentScreenPosition)
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

    public Rect Hide()
    {
        rectTransform.gameObject.SetActive(false);
        return CurrentSelectionRect;
        //TODO: hier moet waarschijnlijk een event komen die het oppervlakte van de selectie box doorgeeft aan de selection controller
    }
}
