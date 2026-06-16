using UnityEngine;

namespace LevelEditorJsonImporter
{
    public sealed class LevelJsonImportSource : MonoBehaviour
    {
        public string levelJsonPath;
        public string importRootName = "Imported Level";
        public bool autoReimportOnUnityFocus = true;
        public long lastImportedUtcTicks;
    }
}
