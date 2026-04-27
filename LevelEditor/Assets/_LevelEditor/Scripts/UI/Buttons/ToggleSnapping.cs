using UnityEngine;
using UnityEngine.UI;

public class ToggleSnapping : MonoBehaviour
{
    private Toggle toggle;

    void Start()
    {
        if (toggle == null)
            if (!TryGetComponent<Toggle>(out toggle))
            {
                Debug.LogWarning("No toggle found on this gameObject");
                return;
            }

        toggle.isOn = false;
    }
    public void OnToggleSnapping()
    {
        if(toggle == null) return;
        EventManager.Instance.TriggerDelegate(SnappingEvent.OnToggleSnapping, toggle.isOn);
    }
}

public static class SnappingEvent
{
    public const string OnToggleSnapping = "OnToggleSnapping";
}