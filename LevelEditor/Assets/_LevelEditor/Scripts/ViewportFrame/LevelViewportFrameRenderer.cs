using UnityEngine;

/// <summary>
/// Draws the editor game-viewport frame in world space.
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelViewportFrameRenderer : MonoBehaviour
{
    const int CornerCount = 4;

    static Material s_LineMaterial;

    [SerializeField] float lineWidth = 0.05f;
    [SerializeField] float selectedLineWidth = 0.065f;
    [SerializeField] int sortingOrder = 31990;

    LineRenderer lineRenderer;
    readonly Vector3[] cornerBuffer = new Vector3[CornerCount + 1];

    void OnEnable()
    {
        LevelViewportFrameState.Instance.Changed += Refresh;
        EnsureLineRenderer();
        Refresh();
    }

    void OnDisable()
    {
        LevelViewportFrameState.Instance.Changed -= Refresh;
    }

    void LateUpdate() => Refresh();

    public void Refresh()
    {
        EnsureLineRenderer();
        LevelViewportFrameState state = LevelViewportFrameState.Instance;

        if (!state.Enabled)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            return;
        }

        Bounds bounds = state.WorldBounds;
        float z = 0f;

        cornerBuffer[0] = new Vector3(bounds.min.x, bounds.min.y, z);
        cornerBuffer[1] = new Vector3(bounds.max.x, bounds.min.y, z);
        cornerBuffer[2] = new Vector3(bounds.max.x, bounds.max.y, z);
        cornerBuffer[3] = new Vector3(bounds.min.x, bounds.max.y, z);
        cornerBuffer[4] = cornerBuffer[0];

        bool selected = state.IsSelected;
        Color color = selected ? state.SelectedOutlineColor : state.OutlineColor;
        float width = selected ? selectedLineWidth : lineWidth;

        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.enabled = true;
        lineRenderer.SetPositions(cornerBuffer);
    }

    void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.material = GetLineMaterial();
        lineRenderer.positionCount = CornerCount + 1;
    }

    static Material GetLineMaterial()
    {
        if (s_LineMaterial != null)
            return s_LineMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        s_LineMaterial = shader != null ? new Material(shader) : null;
        return s_LineMaterial;
    }
}
