using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TransformWindowView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    private FloatField positionX;
    private FloatField positionY;
    private FloatField positionZ;

    private FloatField rotationZ;

    private FloatField scaleX;
    private FloatField scaleY;

    private Button resetButton;

    private Transform selectedTransform;
    private bool isUpdatingUI;
    private VisualElement transformWindowRoot;

    private void Awake()
    {
        VisualElement root = uiDocument.rootVisualElement;

        transformWindowRoot = root.Q<VisualElement>("transform-window");

        positionX = root.Q<FloatField>("position-x");
        positionY = root.Q<FloatField>("position-y");
        positionZ = root.Q<FloatField>("position-z");

        rotationZ = root.Q<FloatField>("rotation-z");

        scaleX = root.Q<FloatField>("scale-x");
        scaleY = root.Q<FloatField>("scale-y");

        resetButton = root.Q<Button>("reset-transform-button");

        RegisterCallbacks();
        SetTarget(null);

        EventManager.Instance.AddDelegateListener(SelectionEvents.OnSelectionChanged, (Action<HashSet<SelectableTargetData>>)ActivateWindow);
    }
    void Start()
    {
        EventManager.Instance.AddUnityEventListener(TransformWindowEvents.OnTransformValuesUpdated, RefreshUI);
    }

    void Update()
    {
        // if(!EditorBlackBoard.HasSelection) return;

        // RefreshUI();
    }

    private void RegisterCallbacks()
    {
        positionX.RegisterValueChangedCallback(_ => ApplyPosition());
        positionY.RegisterValueChangedCallback(_ => ApplyPosition());
        positionZ.RegisterValueChangedCallback(_ => ApplyPosition());

        rotationZ.RegisterValueChangedCallback(_ => ApplyRotation());

        scaleX.RegisterValueChangedCallback(_ => ApplyScale());
        scaleY.RegisterValueChangedCallback(_ => ApplyScale());

        resetButton.clicked += ResetTransform;
    }

    public void ActivateWindow(HashSet<SelectableTargetData> selectableTargetDatas)
    {
        Debug.Log(EditorBlackBoard.HasSelection + " ... " + EditorBlackBoard.HasMultiSelection);
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

        SetTarget(selectedLevelObject.transform);
    }

    public void SetTarget(Transform target)
    {
        selectedTransform = target;

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

    public void RefreshUI()
    {
        if (selectedTransform == null)
            return;

        UpdateGizmo();

        isUpdatingUI = true;


        Vector3 pos = selectedTransform.position;
        positionX.SetValueWithoutNotify(pos.x);
        positionY.SetValueWithoutNotify(pos.y);
        positionZ.SetValueWithoutNotify(pos.z);

        Vector3 rot = selectedTransform.eulerAngles;
        rotationZ.SetValueWithoutNotify(rot.z);

        Vector3 scale = selectedTransform.localScale;
        scaleX.SetValueWithoutNotify(scale.x);
        scaleY.SetValueWithoutNotify(scale.y);
        Debug.Log("scale value" + scaleX.value);

        Debug.Log($"RefreshUI target: {selectedTransform.name}");
        Debug.Log($"Position: {pos}, Rotation Z: {rot.z}, Scale: {scale}");

        isUpdatingUI = false;
    }

    private void ApplyPosition()
    {
        if (isUpdatingUI || selectedTransform == null)
            return;

        float clampedZ = Mathf.Max(0f, positionZ.value);

        if (!Mathf.Approximately(positionZ.value, clampedZ))
            positionZ.value = clampedZ;

        selectedTransform.position = new Vector3(
            positionX.value,
            positionY.value,
            positionZ.value
        );
    }

    private void ApplyRotation()
    {
        if (isUpdatingUI || selectedTransform == null)
            return;

        selectedTransform.eulerAngles = new Vector3(
            0f,
            0f,
            rotationZ.value
        );
    }

    private void ApplyScale()
    {
        if (isUpdatingUI || selectedTransform == null)
            return;

        selectedTransform.localScale = new Vector3(
            scaleX.value,
            scaleY.value,
            1f
        );
    }

    private void ResetTransform()
    {
        if (selectedTransform == null)
            return;

        selectedTransform.position = Vector3.zero;
        selectedTransform.eulerAngles = Vector3.zero;
        selectedTransform.localScale = Vector3.one;

        RefreshUI();
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
    }

    private void UpdateGizmo()
    {
        EventManager.Instance.TriggerDelegate(TransformWindowEvents.OnSelectedObjectTransformUpdated, selectedTransform);
    }
}

public static class TransformWindowEvents
{
    public const string OnSelectedObjectTransformUpdated = "OnSelectedObjectTransformUpdated";
    public const string OnTransformValuesUpdated = "OnTransformValuesUpdated";
}