using UnityEngine;

//this class is responsible for opening a window for the user to select a path to their assets
public class ImportButton : MonoBehaviour
{
    public void ImportFolder()
    {
        LevelEditorFileMenuCommands.ImportFolder();
    }

    public void ImportFile()
    {
        LevelEditorFileMenuCommands.ImportAssets();
    }
}
