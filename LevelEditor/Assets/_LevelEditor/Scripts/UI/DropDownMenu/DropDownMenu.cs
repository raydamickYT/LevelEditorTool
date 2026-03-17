using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DropDownMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private RectTransform menuRect;
    [SerializeField] private RectTransform buttonRect;

    // Update is called once per frame
    void Update()
    {
        if (!menuPanel.activeSelf || Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            bool clickedMenu = RectTransformUtility.RectangleContainsScreenPoint(
                menuRect,
                mousePos);

            bool clickedButton = RectTransformUtility.RectangleContainsScreenPoint(
                buttonRect,
                mousePos);

            if (!clickedMenu && !clickedButton)
            {
                menuPanel.SetActive(false);
            }
        }
    }
}
