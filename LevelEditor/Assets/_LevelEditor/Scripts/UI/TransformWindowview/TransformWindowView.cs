using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TransformWindowView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    private IntegerField layerField;
    private SpriteRenderer spriteRenderer;

    private FloatField positionX;
    private FloatField positionY;
    private FloatField positionZ;

    private FloatField rotationZ;

    private FloatField scaleX;
    private FloatField scaleY;

    private Button resetButton;
    private Button mirrorScaleHorizontalButton;
    private Toggle collisionToggle;

    private Transform selectedTransform;
    private LevelObject selectedLevelObject;
    private readonly List<LevelObject> selectedLevelObjects = new();
    private bool isUpdatingUI;
    private bool isCapturingTransformAction;
    private readonly List<TransformAction> currentTransformActions = new();
    private VisualElement transformWindowRoot;

    private void Awake()
    {
        VisualElement root = uiDocument.rootVisualElement;

        transformWindowRoot = root.Q<VisualElement>("transform-window");

        layerField = root.Q<IntegerField>("Object Layer");

        positionX = root.Q<FloatField>("position-x");
        positionY = root.Q<FloatField>("position-y");
        positionZ = root.Q<FloatField>("position-z");

        rotationZ = root.Q<FloatField>("rotation-z");

        scaleX = root.Q<FloatField>("scale-x");
        scaleY = root.Q<FloatField>("scale-y");

        resetButton = root.Q<Button>("reset-transform-button");
        mirrorScaleHorizontalButton = root.Q<Button>("mirror-scale-h-button");
        collisionToggle = root.Q<Toggle>("collision-toggle");

        RegisterCallbacks();
        SetTarget(null);

        EventManager.Instance.AddDelegateListener(SelectionEvents.OnSelectionChanged, (Action<HashSet<SelectableTargetData>>)ActivateWindow);
    }
    void Start()
    {
        EventManager.Instance.AddUnityEventListener(TransformWindowEvents.OnTransformValuesUpdated, RefreshUI);
        EventManager.Instance.AddDelegateListener(ShortcutBindingEvents.OnCommandTriggered, (Action<EditorCommand>)HandleCommand);
    }

    private void HandleCommand(EditorCommand command)
    {
        if (command == EditorCommand.Undo || command == EditorCommand.Redo)
            RefreshUI();
    }

    private void RegisterCallbacks()
    {
        layerField.RegisterValueChangedCallback(_ => ApplyLayer());

        positionX.RegisterValueChangedCallback(_ => ApplyPositionAxis(0));
        positionY.RegisterValueChangedCallback(_ => ApplyPositionAxis(1));
        positionZ.RegisterValueChangedCallback(_ => ApplyPositionAxis(2));

        rotationZ.RegisterValueChangedCallback(_ => ApplyRotation());

        scaleX.RegisterValueChangedCallback(_ => ApplyScaleAxis(0));
        scaleY.RegisterValueChangedCallback(_ => ApplyScaleAxis(1));

        resetButton.clicked += ResetTransform;
        mirrorScaleHorizontalButton.clicked += MirrorScaleHorizontal;
        if (collisionToggle != null)
            collisionToggle.RegisterValueChangedCallback(_ => ApplyCollision());
    }

    //update UI visuals
    public void RefreshUI()
    {
        if (selectedLevelObjects.Count == 0)
            return;

        UpdateGizmo();

        isUpdatingUI = true;

        SetIntegerFieldSharedValue(layerField, TryGetSharedSortingOrder(out int renderLayer), renderLayer);
        layerField.SetEnabled(AllSelectedHaveSpriteRenderer());

        bool canToggleCollision = selectedLevelObject != null
            && !(selectedLevelObject is LevelObjectGroup)
            && selectedTransform.TryGetComponent(out SpriteRenderer _)
            && AllSelectedAreSpriteAssets();
        if (collisionToggle != null)
        {
            collisionToggle.SetEnabled(canToggleCollision);
            bool hasSharedCollision = false;
            bool hasCollision = false;
            if (canToggleCollision)
                hasSharedCollision = TryGetSharedCollisionValue(out hasCollision);

            SetToggleSharedValue(collisionToggle, hasSharedCollision, hasCollision);
        }

        SetFloatFieldSharedValue(positionX, TryGetSharedPositionAxis(0, out float px), px);
        SetFloatFieldSharedValue(positionY, TryGetSharedPositionAxis(1, out float py), py);
        SetFloatFieldSharedValue(positionZ, TryGetSharedPositionAxis(2, out float pz), pz);

        SetFloatFieldSharedValue(rotationZ, TryGetSharedRotationZ(out float rz), rz);

        SetFloatFieldSharedValue(scaleX, TryGetSharedScaleAxis(0, out float sx), sx);
        SetFloatFieldSharedValue(scaleY, TryGetSharedScaleAxis(1, out float sy), sy);
        // Debug.Log("scale value" + scaleX.value);

        // Debug.Log($"RefreshUI target: {selectedTransform.name}");
        // Debug.Log($"Position: {pos}, Rotation Z: {rot.z}, Scale: {scale}");

        isUpdatingUI = false;
    }

    public void ActivateWindow(HashSet<SelectableTargetData> selectableTargetDatas)
    {
        // Debug.Log(EditorBlackBoard.HasSelection + " ... " + EditorBlackBoard.HasMultiSelection);
        if (!EditorBlackBoard.HasSelection)
        {
            SetTargets(null);
            return;
        }

        List<LevelObject> selectedObjects = EditorBlackBoard.CurrentSelectedLevelObjects
            .Where(x => x != null)
            .ToList();

        if (selectedObjects.Count == 0)
        {
            SetTargets(null);
            return;
        }

        SetTargets(selectedObjects);
    }

    public void SetTarget(LevelObject target)
    {
        SetTargets(target != null ? new List<LevelObject> { target } : null);
    }

    public void SetTargets(IEnumerable<LevelObject> targets)
    {
        selectedLevelObjects.Clear();
        if (targets != null)
            selectedLevelObjects.AddRange(targets.Where(x => x != null));

        selectedLevelObject = selectedLevelObjects.FirstOrDefault();
        selectedTransform = selectedLevelObject != null ? selectedLevelObject.transform : null;

        bool hasTarget = selectedLevelObjects.Count > 0;

        if (hasTarget)
        {
            ShowWindow();
            SetEnabled(true);
            RefreshUI();
        }
        else
        {
            HideWindow();
            SetEnabled(false);
        }
    }

    private void ShowWindow()
    {
        transformWindowRoot.style.display = DisplayStyle.Flex;
    }

    private void HideWindow()
    {
        transformWindowRoot.style.display = DisplayStyle.None;
    }


    private void ApplyPositionAxis(int axis)
    {
        if (isUpdatingUI || selectedLevelObjects.Count == 0)
            return;

        BeginTransformAction();

        FloatField sourceField = axis == 0 ? positionX : axis == 1 ? positionY : positionZ;
        float value = axis == 2 ? Mathf.Max(0f, sourceField.value) : sourceField.value;

        if (axis == 2 && !Mathf.Approximately(positionZ.value, value))
            positionZ.value = value;

        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                continue;

            Vector3 position = levelObject.transform.position;
            position[axis] = value;
            levelObject.transform.position = position;
        }

        EndTransformAction();
    }

    private void ApplyRotation()
    {
        if (isUpdatingUI || selectedLevelObjects.Count == 0)
            return;

        BeginTransformAction();

        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                continue;

            levelObject.transform.eulerAngles = new Vector3(0f, 0f, rotationZ.value);
        }

        EndTransformAction();
    }

    private void ApplyScaleAxis(int axis)
    {
        if (isUpdatingUI || selectedLevelObjects.Count == 0)
            return;

        BeginTransformAction();

        float value = axis == 0 ? scaleX.value : scaleY.value;
        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                continue;

            Vector3 scale = levelObject.transform.localScale;
            scale[axis] = value;
            scale.z = 1f;
            levelObject.transform.localScale = scale;
        }

        EndTransformAction();
    }

    private void ApplyLayer()
    {
        if(isUpdatingUI || selectedLevelObjects.Count == 0) return;

        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                continue;

            levelObject.ApplySortingOrder(layerField.value);
        }
    }

    private void ApplyCollision()
    {
        if (isUpdatingUI || selectedLevelObjects.Count == 0)
            return;

        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (IsSpriteAsset(levelObject))
                levelObject.HasCollision = collisionToggle.value;
        }
    }

    private bool IsSpriteAsset(LevelObject levelObject)
    {
        if (levelObject == null)
            return false;

        ImportedAssetMetaData asset = AssetStorageService.GetAssetByID(levelObject.AssetID);
        return asset != null
            && string.Equals(asset.AssetType, ImportedAssetTypes.Sprite, StringComparison.OrdinalIgnoreCase);
    }

    private bool AllSelectedAreSpriteAssets()
    {
        return selectedLevelObjects.Count > 0
            && selectedLevelObjects.All(IsSpriteAsset);
    }

    private bool AllSelectedHaveSpriteRenderer()
    {
        return selectedLevelObjects.Count > 0
            && selectedLevelObjects.All(levelObject =>
                levelObject != null
                && levelObject.GetComponentsInChildren<SpriteRenderer>(true).Length > 0);
    }

    private bool TryGetSharedPositionAxis(int axis, out float value)
    {
        value = 0f;
        return TryGetSharedFloat(levelObject => levelObject.transform.position[axis], out value);
    }

    private bool TryGetSharedScaleAxis(int axis, out float value)
    {
        value = 0f;
        return TryGetSharedFloat(levelObject => levelObject.transform.localScale[axis], out value);
    }

    private bool TryGetSharedRotationZ(out float value)
    {
        value = 0f;
        return TryGetSharedFloat(levelObject => levelObject.transform.eulerAngles.z, out value);
    }

    private bool TryGetSharedSortingOrder(out int value)
    {
        value = 0;
        if (!AllSelectedHaveSpriteRenderer())
            return false;

        bool hasValue = false;
        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null || levelObject.GetComponentsInChildren<SpriteRenderer>(true).Length == 0)
                return false;

            SpriteRenderer[] renderers = levelObject.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                if (!hasValue)
                {
                    value = renderers[i].sortingOrder;
                    hasValue = true;
                    continue;
                }

                if (value != renderers[i].sortingOrder)
                    return false;
            }
        }

        return hasValue;
    }

    private bool TryGetSharedCollisionValue(out bool value)
    {
        value = false;
        if (!AllSelectedAreSpriteAssets())
            return false;

        bool hasValue = false;
        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                return false;

            if (!hasValue)
            {
                value = levelObject.HasCollision;
                hasValue = true;
                continue;
            }

            if (value != levelObject.HasCollision)
                return false;
        }

        return hasValue;
    }

    private bool TryGetSharedFloat(Func<LevelObject, float> getter, out float value)
    {
        value = 0f;
        if (selectedLevelObjects.Count == 0 || getter == null)
            return false;

        bool hasValue = false;
        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                return false;

            float current = getter(levelObject);
            if (!hasValue)
            {
                value = current;
                hasValue = true;
                continue;
            }

            if (!Mathf.Approximately(value, current))
                return false;
        }

        return hasValue;
    }

    private void SetFloatFieldSharedValue(FloatField field, bool hasSharedValue, float value)
    {
        if (field == null)
            return;

        field.showMixedValue = !hasSharedValue;
        field.SetValueWithoutNotify(hasSharedValue ? value : 0f);
    }

    private void SetIntegerFieldSharedValue(IntegerField field, bool hasSharedValue, int value)
    {
        if (field == null)
            return;

        field.showMixedValue = !hasSharedValue;
        field.SetValueWithoutNotify(hasSharedValue ? value : 0);
    }

    private void SetToggleSharedValue(Toggle toggle, bool hasSharedValue, bool value)
    {
        if (toggle == null)
            return;

        toggle.showMixedValue = !hasSharedValue;
        toggle.SetValueWithoutNotify(hasSharedValue && value);
    }

    private void ResetTransform()
    {
        if (selectedLevelObjects.Count == 0)
            return;

        BeginTransformAction();

        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                continue;

            levelObject.transform.position = Vector3.zero;
            levelObject.transform.eulerAngles = Vector3.zero;
            levelObject.transform.localScale = Vector3.one;
        }

        RefreshUI();

        EndTransformAction();
    }

    private void MirrorScaleHorizontal()
    {
        if (selectedLevelObjects.Count == 0)
            return;

        BeginTransformAction();

        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject == null)
                continue;

            Vector3 s = levelObject.transform.localScale;
            s.x = -s.x;
            levelObject.transform.localScale = s;
        }

        RefreshUI();

        EndTransformAction();
    }

    private void SetEnabled(bool enabled)
    {
        positionX.SetEnabled(enabled);
        positionY.SetEnabled(enabled);
        positionZ.SetEnabled(enabled);

        rotationZ.SetEnabled(enabled);

        scaleX.SetEnabled(enabled);
        scaleY.SetEnabled(enabled);

        resetButton.SetEnabled(enabled);
        mirrorScaleHorizontalButton.SetEnabled(enabled);
        if (collisionToggle != null)
            collisionToggle.SetEnabled(enabled);
    }

    private void UpdateGizmo()
    {
        EventManager.Instance.TriggerDelegate(TransformWindowEvents.OnSelectedObjectTransformUpdated, selectedTransform);
    }

    private void BeginTransformAction()
    {
        if (selectedLevelObjects.Count == 0 || isCapturingTransformAction)
            return;

        currentTransformActions.Clear();
        foreach (LevelObject levelObject in selectedLevelObjects)
        {
            if (levelObject != null)
                currentTransformActions.Add(new TransformAction(levelObject));
        }

        if (currentTransformActions.Count == 0)
            return;

        isCapturingTransformAction = true;
    }

    private void EndTransformAction()
    {
        if (!isCapturingTransformAction || currentTransformActions.Count == 0)
            return;

        foreach (TransformAction action in currentTransformActions)
            action.CaptureAfterState();

        List<TransformAction> changedActions = currentTransformActions
            .Where(action => action.HasChanged())
            .ToList();

        if (changedActions.Count == 1)
            EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, changedActions[0]);
        else if (changedActions.Count > 1)
        {
            var compositeAction = new CompositeAction(changedActions.Cast<IUndoableAction>(), "Transform Multiple Objects");
            EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, compositeAction);
        }

        currentTransformActions.Clear();
        isCapturingTransformAction = false;
    }
}

public static class TransformWindowEvents
{
    public const string OnSelectedObjectTransformUpdated = "OnSelectedObjectTransformUpdated";
    public const string OnTransformValuesUpdated = "OnTransformValuesUpdated";
}