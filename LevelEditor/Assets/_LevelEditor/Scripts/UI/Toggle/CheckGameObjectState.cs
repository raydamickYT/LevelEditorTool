using UnityEngine;
using UnityEngine.UI;

public class CheckGameObjectState : MonoBehaviour
{
    private Toggle toggle;
    public GameObject UIObject;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        toggle = gameObject.GetComponent<Toggle>();

        if (UIObject == null) return;
        toggle.SetIsOnWithoutNotify(UIObject.activeSelf);
    }
}