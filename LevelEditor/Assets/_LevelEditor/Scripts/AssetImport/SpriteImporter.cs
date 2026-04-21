using UnityEditor;
using UnityEngine;
using System.IO;

public class SpriteImporter : MonoBehaviour
{
    [FolderPath]
    public string path;
    public ImportedSpriteData importedSpriteData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void LoadAsset()
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning("File path is null. Cannot load sprite.");
            return;
        }

        if (!Directory.Exists(path))
        {
            Debug.LogError($"Directory does not exist: {path}");
            return;
        }

        string[] files = Directory.GetFiles(path);

        foreach (string file in files)
        {
            string extension = Path.GetExtension(file).ToLower();

            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
            {
                Debug.LogWarning($"Unsupported file type: {file}. Skipping.");
                continue;
            }

            byte[] fileBytes = File.ReadAllBytes(path);

            Texture2D texture = new Texture2D(2, 2);
            bool loaded = texture.LoadImage(fileBytes);

            if (!loaded)
            {
                Debug.LogWarning($"Failed to load image data from file: {file}. Skipping.");
                continue;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = Path.GetFileNameWithoutExtension(path);

            Debug.Log("File loaded: " + sprite.name);

        }
    }
}
