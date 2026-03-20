using UnityEngine;

public interface ISelectable
{
    bool IsSelected { get; }
    void OnSelect();
}
