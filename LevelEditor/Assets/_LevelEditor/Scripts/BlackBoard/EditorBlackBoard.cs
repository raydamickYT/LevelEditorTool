using System.Collections.Generic;
using System.Linq;

public static class EditorBlackBoard
{
    private static readonly HashSet<SelectableTargetData> currentSelection = new();
    private static readonly HashSet<LevelObject> currentSelectedLevelObjects = new();

    public static IReadOnlyCollection<SelectableTargetData> CurrentSelection => currentSelection;
    public static IReadOnlyCollection<LevelObject> CurrentSelectedLevelObjects => currentSelectedLevelObjects;

    public static bool HasSelection => currentSelection.Count > 0;
    public static bool HasMultiSelection => currentSelection.Count > 1;

    public static void SetSelection(IEnumerable<SelectableTargetData> selection)
    {
        currentSelection.Clear();
        currentSelectedLevelObjects.Clear();

        if (selection == null) return;

        foreach (SelectableTargetData item in selection)
        {
            if (item == null || item.BaseObject == null) continue;

            currentSelection.Add(item);

            if (item.BaseObject.TryGetComponent(out LevelObject component))
                currentSelectedLevelObjects.Add(component);
        }
    }


    public static void ClearSelection()
    {
        currentSelection.Clear();
        currentSelectedLevelObjects.Clear();
    }

    public static List<LevelObject> GetSelectedLevelObjectsList()
    {
        return currentSelectedLevelObjects.ToList();
    }
}
