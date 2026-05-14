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

    private Transform selectedTransform;
    private LevelObject selectedLevelObject;
    private bool isUpdatingUI;
    private bool isCapturingTransformAction;
    private TransformAction currentTransformAction;
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

        positionX.RegisterValueChangedCallback(_ => ApplyPosition());
        positionY.RegisterValueChangedCallback(_ => ApplyPosition());
        positionZ.RegisterValueChangedCallback(_ => ApplyPosition());

        rotationZ.RegisterValueChangedCallback(_ => ApplyRotation());

        scaleX.RegisterValueChangedCallback(_ => ApplyScale());
        scaleY.RegisterValueChangedCallback(_ => ApplyScale());

        resetButton.clicked += ResetTransform;
        mirrorScaleHorizontalButton.clicked += MirrorScaleHorizontal;
    }

    //update UI visuals
    public void RefreshUI()
    {
        if (selectedTransform == null)
            return;

        UpdateGizmo();

        isUpdatingUI = true;

        if(selectedTransform.TryGetComponent(out SpriteRenderer renderer))
        {
            int renderLayer = renderer.sortingOrder;
            layerField.SetValueWithoutNotify(renderLayer);
            // this.spriteRenderer = renderer;
        }


        Vector3 pos = selectedTransform.position;
        positionX.SetValueWithoutNotify(pos.x);
        positionY.SetValueWithoutNotify(pos.y);
        positionZ.SetValueWithoutNotify(pos.z);

        Vector3 rot = selectedTransform.eulerAngles;
        rotationZ.SetValueWithoutNotify(rot.z);

        Vector3 scale = selectedTransform.localScale;
        scaleX.SetValueWithoutNotify(scale.x);
        scaleY.SetValueWithoutNotify(scale.y);
        // Debug.Log("scale value" + scaleX.value);

        // Debug.Log($"RefreshUI target: {selectedTransform.name}");
        // Debug.Log($"Position: {pos}, Rotation Z: {rot.z}, Scale: {scale}");

        isUpdatingUI = false;
    }

    public void ActivateWindow(HashSet<SelectableTargetData> selectableTargetDatas)
    {
        // Debug.Log(EditorBlackBoard.HasSelection + " ... " + EditorBlackBoard.HasMultiSelection);
        if (!EditorBlackBoard.HasSelection || EditorBlackBoard.HasMultiSelection)
        {
            SetTarget(null);
            return;
        }

        LevelObject selectedLevelObject = EditorBlackBoard.CurrentSelectedLevelObjects.FirstOrDefault();

        if (selectedLevelObject == null)
        {
            SetTarget(null);
            return;
        }

        SetTarget(selectedLevelObject);
    }

    public void SetTarget(LevelObject target)
    {
        selectedLevelObject = target;
        selectedTransform = target != null ? target.transform : null;

        bool hasTarget = selectedTransform != null;

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


    private void ApplyPosition()
    {
        if (isUpdatingUI || selectedTransform == null)
            return;

        BeginTransformAction();

        float clampedZ = Mathf.Max(0f, positionZ.value);

        if (!Mathf.Approximately(positionZ.value, clampedZ))
            positionZ.value = clampedZ;

        selectedTransform.position = new Vector3(
            positionX.value,
            positionY.value,
            positionZ.value
        );

        EndTransformAction();
    }

    private void ApplyRotation()
    {
        if (isUpdatingUI || selectedTransform == null)
            return;

        BeginTransformAction();


        selectedTransform.eulerAngles = new Vector3(
            0f,
            0f,
            rotationZ.value
        );

        EndTransformAction();
    }

    private void ApplyScale()
    {
        if (isUpdatingUI || selectedTransform == null)
            return;

        BeginTransformAction();

        selectedTransform.localScale = new Vector3(
            scaleX.value,
            scaleY.value,
            1f
        );

        EndTransformAction();
    }

    private void ApplyLayer()
    {
        if(isUpdatingUI || selectedTransform == null) return;

        if(!selectedTransform.TryGetComponent(out SpriteRenderer renderer))
        {
            Debug.LogWarning($"{selectedTransform.name} has no active sprite renderer");
            return;
        }

        renderer.sortingOrder = layerField.value;
    }

    private void ResetTransform()
    {
        if (selectedTransform == null)
            return;

        BeginTransformAction();

        selectedTransform.position = Vector3.zero;
        selectedTransform.eulerAngles = Vector3.zero;
        selectedTransform.localScale = Vector3.one;

        RefreshUI();

        EndTransformAction();
    }

    private void MirrorScaleHorizontal()
    {
        if (selectedTransform == null)
            return;

        BeginTransformAction();

        Vector3 s = selectedTransform.localScale;
        s.x = -s.x;
        selectedTransform.localScale = s;

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
    }

    private void UpdateGizmo()
    {
        EventManager.Instance.TriggerDelegate(TransformWindowEvents.OnSelectedObjectTransformUpdated, selectedTransform);
    }

    private void BeginTransformAction()
    {
        if (selectedLevelObject == null || isCapturingTransformAction)
            return;

        currentTransformAction = new TransformAction(selectedLevelObject);
        isCapturingTransformAction = true;
    }

    private void EndTransformAction()
    {
        if (!isCapturingTransformAction || currentTransformAction == null)
            return;

        currentTransformAction.CaptureAfterState();

        if (currentTransformAction.HasChanged())
        {
            EventManager.Instance.TriggerDelegate(ActionStackEvents.RegisterAction, currentTransformAction);
        }

        currentTransformAction = null;
        isCapturingTransformAction = false;
    }
}

public static class TransformWindowEvents
{
    public const string OnSelectedObjectTransformUpdated = "OnSelectedObjectTransformUpdated";
    public const string OnTransformValuesUpdated = "OnTransformValuesUpdated";
}