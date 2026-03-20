using UnityEngine;
using UnityEngine.InputSystem;

public class InteractibleBaseClass : MonoBehaviour, ISelectable
{

    public bool IsSelected { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
    public virtual void OnSelect()
    {
        IsSelected = true;
        Debug.Log($"{gameObject.name} has been selected.");
    }

    public virtual void OnDeselect()
    {
        // throw new System.NotImplementedException();
    }
}
