using UnityEngine;

namespace LevelEditorJsonImporter
{
    public sealed class LevelJsonImportedObject : MonoBehaviour
    {
        public int instanceId;
        public int parentInstanceId;
        public bool isGroup;
        public bool usesWrapper;
        public string assetId;
        public string prefabGuid;
        public bool hasCollision;
        public string recordSignature;
    }
}
