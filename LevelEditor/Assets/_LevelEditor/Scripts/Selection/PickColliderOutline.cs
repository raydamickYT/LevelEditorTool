using System;
using UnityEngine;

/// <summary>
/// Draws a subtle world-space outline around the editor pick <see cref="BoxCollider2D"/>.
/// Selected objects always show an outline. Optional show-all mode reveals every pick box in the level.
/// </summary>
[DisallowMultipleComponent]
public sealed class PickColliderOutline : MonoBehaviour
{
    const int CornerCount = 4;

    static Material s_LineMaterial;

    [SerializeField] Color selectedOutlineColor = new(1f, 1f, 1f, 0.55f);
    [SerializeField] Color allObjectsOutlineColor = new(1f, 1f, 1f, 0.2f);
    [SerializeField] float selectedLineWidth = 0.035f;
    [SerializeField] float allObjectsLineWidth = 0.025f;
    [SerializeField] int sortingOrder = 32000;

    LineRenderer lineRenderer;
    BoxCollider2D boxCollider;
    SelectableObject selectableObject;
    readonly Vector3[] cornerBuffer = new Vector3[CornerCount + 1];

    public void Refresh()
    {
        if (!isActiveAndEnabled)
            return;

        EnsureComponents();
        UpdateOutlineGeometry();
    }

    void OnEnable()
    {
        selectableObject = GetComponent<SelectableObject>();
        if (selectableObject != null)
            selectableObject.OnSelectionChanged += Refresh;

        PickColliderOutlineSettings.Changed += Refresh;
        EnsureComponents();
        UpdateOutlineGeometry();
    }

    void OnDisable()
    {
        if (selectableObject != null)
            selectableObject.OnSelectionChanged -= Refresh;

        PickColliderOutlineSettings.Changed -= Refresh;
    }

    void LateUpdate()
    {
        if (!ShouldShow())
        {
            if (lineRenderer != null && lineRenderer.enabled)
                lineRenderer.enabled = false;

            return;
        }

        UpdateOutlineGeometry();
    }

    bool IsSelected()
    {
        if (selectableObject == null)
            selectableObject = GetComponent<SelectableObject>();

        return selectableObject != null && selectableObject.IsSelected;
    }

    bool ShouldShow()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider == null || !boxCollider.enabled)
            return false;

        if (IsSelected())
            return true;

        return PickColliderOutlineSettings.ShowAllCollisionBoxes;
    }

    bool UseSelectedStyle() => IsSelected();

    void EnsureComponents()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        ConfigureLineRenderer(lineRenderer);
    }

    void ConfigureLineRenderer(LineRenderer renderer)
    {
        renderer.useWorldSpace = true;
        renderer.loop = true;
        renderer.alignment = LineAlignment.View;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.numCapVertices = 0;
        renderer.numCornerVertices = 0;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = sortingOrder;
        renderer.material = GetLineMaterial();
        renderer.positionCount = CornerCount + 1;
    }

    void UpdateOutlineGeometry()
    {
        if (lineRenderer == null)
            return;

        if (!ShouldShow() || boxCollider == null || !boxCollider.enabled)
        {
            lineRenderer.enabled = false;
            return;
        }

        Bounds bounds = boxCollider.bounds;
        float z = bounds.center.z;

        cornerBuffer[0] = new Vector3(bounds.min.x, bounds.min.y, z);
        cornerBuffer[1] = new Vector3(bounds.max.x, bounds.min.y, z);
        cornerBuffer[2] = new Vector3(bounds.max.x, bounds.max.y, z);
        cornerBuffer[3] = new Vector3(bounds.min.x, bounds.max.y, z);
        cornerBuffer[4] = cornerBuffer[0];

        bool selectedStyle = UseSelectedStyle();
        Color color = selectedStyle ? selectedOutlineColor : allObjectsOutlineColor;
        float width = selectedStyle ? selectedLineWidth : allObjectsLineWidth;

        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.enabled = true;
        lineRenderer.SetPositions(cornerBuffer);
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

public static class PickColliderOutlineSettings
{
    public static bool ShowAllCollisionBoxes { get; private set; }

    public static event Action Changed;

    public static void SetShowAllCollisionBoxes(bool showAll)
    {
        if (ShowAllCollisionBoxes == showAll)
            return;

        ShowAllCollisionBoxes = showAll;
        Changed?.Invoke();
        RefreshAll();
    }

    public static void RefreshAll()
    {
        PickColliderOutline[] outlines = UnityEngine.Object.FindObjectsByType<PickColliderOutline>(FindObjectsSortMode.None);
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] != null)
                outlines[i].Refresh();
        }
    }
}
