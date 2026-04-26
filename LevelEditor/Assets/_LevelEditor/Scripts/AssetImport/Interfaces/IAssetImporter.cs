using UnityEngine;

public interface IAssetImporter
{
    bool CanImport(string path);
    ImportedSpriteData Import(string path);
}
