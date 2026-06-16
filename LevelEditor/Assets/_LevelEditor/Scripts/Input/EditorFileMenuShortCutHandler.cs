using System;
using UnityEngine;

public class EditorFileMenuShortcutHandler : MonoBehaviour
{
    void Start()
    {
        EventManager.Instance.AddDelegateListener(
            ShortcutBindingEvents.OnCommandTriggered,
            (Action<EditorCommand>)HandleCommand);
    }

    void HandleCommand(EditorCommand command)
    {
        switch (command)
        {
            case EditorCommand.SaveFile:      LevelEditorFileMenuCommands.SaveLevel(); break;
            case EditorCommand.SaveFileAs:    LevelEditorFileMenuCommands.SaveLevelAs(); break;
            case EditorCommand.OpenFile:      LevelEditorFileMenuCommands.OpenLevel(); break;
            case EditorCommand.NewFile:       LevelEditorFileMenuCommands.NewEmptyLevel(); break;
            case EditorCommand.ImportAssets:  LevelEditorFileMenuCommands.ImportGameAssets(); break;
        }
    }
}