using UnityEngine;
using SFB;

//this class is responsible for opening a window for the user to select a path to their assets
public class ImportButton : MonoBehaviour
{
    public void ImportFolder()
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel(
            "Select Asset Folder",
            "",
            false
        );

        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            Debug.Log("No folder selected");
            return;
        }

        string selectedFolder = paths[0];
        EventManager.Instance.TriggerDelegate(AssetRegistryEvents.ImportAssets, selectedFolder, true);
    }

    public void ImportFile()
    {
        ExtensionFilter[] extensions =
        {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
            new ExtensionFilter("All Files", "*")
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select File",
            "",
            extensions, false);

        if (paths == null || paths.Length == 0)
        {
            Debug.Log("No file selected");
            return;
        }

        EventManager.Instance.TriggerDelegate(AssetRegistryEvents.ImportAssets, paths[0], false);
    }
}
