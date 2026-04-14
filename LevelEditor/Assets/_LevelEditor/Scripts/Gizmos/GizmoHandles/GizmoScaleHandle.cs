using UnityEngine;

public class GizmoScaleHandle : GizmoHandle
{
    [SerializeField] private Transform handleTip;
    [SerializeField] private Transform handleStem;

    private Vector3 tipStartLocalPosition;
    private Vector3 stemStartLocalScale;

    public float tipMoveMultiplier = 0.5f;
    public float stemScaleMultiplier = 0.25f;

    protected override void Start()
    {
        base.Start();

        if (handleTip != null)
            tipStartLocalPosition = handleTip.localPosition;

        if (handleStem != null)
            stemStartLocalScale = handleStem.localScale;
    }

    public void UpdateScaleVisual(float dragAmount)
    {

        Vector3 axis = Axis switch
        {
            GizmoAxis.X => Vector3.right,
            GizmoAxis.Y => Vector3.up,
            GizmoAxis.Z => Vector3.forward,
            _ => Vector3.zero
        };

        if (handleTip != null)
            handleTip.localPosition = tipStartLocalPosition + axis * (dragAmount * tipMoveMultiplier);

        if (handleStem != null)
        {
            Vector3 scale = stemStartLocalScale; //scale from the pivor point
            scale.z = stemStartLocalScale.z + (dragAmount * stemScaleMultiplier);

            handleStem.localScale = scale;
        }
    }

    public void ResetScaleVisual()
    {
        if (handleTip != null)
            handleTip.localPosition = tipStartLocalPosition;

        if (handleStem != null)
            handleStem.localScale = stemStartLocalScale;
    }
}
