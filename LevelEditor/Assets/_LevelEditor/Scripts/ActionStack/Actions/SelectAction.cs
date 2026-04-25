using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectAction : IUndoableAction, IEditorCommand
{
    private readonly List<int> beforeSelectionIDs = new();
    private readonly List<int> afterSelectionIDs = new();
    private readonly string debugLabel;

    public SelectAction(IEnumerable<int> beforeSelectionIDs, IEnumerable<int> afterSelectionIDs, string label = "SelectAction")
    {
        debugLabel = label;

        this.beforeSelectionIDs = beforeSelectionIDs?.ToList() ?? new List<int>();
        this.afterSelectionIDs = afterSelectionIDs?.ToList() ?? new List<int>();
    }
    public string DebugLabel => debugLabel;

    public void Execute()
    {
        ApplySelection(afterSelectionIDs);
    }

    public void Redo()
    {
        Execute();
    }

    public void Undo()
    {
        ApplySelection(beforeSelectionIDs);
    }

    private void ApplySelection(IEnumerable<int> selectionIDs)
    {
        List<GameObject> objectsToSelect = new();

        foreach (int id in selectionIDs)
        {
            LevelObject levelObject = ObjectRegistry.GetLevelObject(id);

            if (levelObject == null)
                continue;

            objectsToSelect.Add(levelObject.gameObject);
        }

        EventManager.Instance.TriggerDelegate(SelectionEvents.ReplaceSelectionWithObject, objectsToSelect);
    }
}
