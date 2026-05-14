using UnityEngine;

public class ToggleActive : MonoBehaviour
{

    public void ToggleActiveVoid()
    {
        this.gameObject.SetActive(!gameObject.activeSelf);
    }

}
