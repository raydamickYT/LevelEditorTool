using UnityEngine;

public interface IGizmoTransformOperation 
{
    void Apply(GizmoDragContext context, Vector3 currentMouseWorld);
}
