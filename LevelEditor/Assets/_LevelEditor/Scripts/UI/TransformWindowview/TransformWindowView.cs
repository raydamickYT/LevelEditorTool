using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        // SetTarget(null);

        // ForceFieldTextColor(positionX);
        // ForceFieldTextColor(positionY);
        // ForceFieldTextColor(positionZ);

        // ForceFieldTextColor(rotationZ);

        // ForceFieldTextColor(scaleX);
        // ForceFieldTextColor(scaleY);

        LogReferenceState("transform-window", transformWindowRoot);

        LogReferenceState("position-x", positionX);
        LogReferenceState("position-y", positionY);
        LogReferenceState("position-z", positionZ);

        LogReferenceState("rotation-z", rotationZ);

        LogReferenceState("scale-x", scaleX);
        LogReferenceState("scale-y", scaleY);

        LogReferenceState("reset-transform-button", resetButton);

        EventManager.Instance.AddDelegateListener(SelectionEvents.OnSelectionChanged, (Action<HashSet<SelectableTargetData>>)ActivateWindow);
    }
    private void LogReferenceState(string referenceName, VisualElement element)
    {
        if (element == null)
            Debug.LogError($"TransformWindow reference missing: {referenceName}");
        else
            Debug.Log($"TransformWindow reference found: {referenceName} -> {element.name}");
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

    private void ForceFieldTextColor(FloatField field)
    {
        field.style.color = Color.black;
        field.style.backgroundColor = new Color(0.93f, 0.93f, 0.93f);

        VisualElement input = field.Q(className: "unity-text-input");

        if (input != null)
        {
            input.style.color = Color.black;
            input.style.backgroundColor = new Color(0.93f, 0.93f, 0.93f);
        }
    }

    private void ApplyPosition()
    {
        if (isUpdatingUI || selectedTransform == null)
            return;

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
}