using UnityEngine;

/// <summary>
/// Links a spawned scene object back to its source entry in an external JSON level file (round-trip export).
/// </summary>
public sealed class ExternalJsonObjectBinding : MonoBehaviour
{
    public string SourceFormatId;
    public string SourceCategory;
    public int SourceIndex = -1;
    [TextArea(1, 4)]
    public string SourceJsonFragment;

    public static void CaptureToMemento(LevelObject.Memento memento, GameObject source)
    {
        if (memento == null || source == null)
            return;

        if (!source.TryGetComponent(out ExternalJsonObjectBinding binding))
            return;

        memento.HasExternalJsonBinding = true;
        memento.ExternalJsonSourceFormatId = binding.SourceFormatId ?? string.Empty;
        memento.ExternalJsonSourceCategory = binding.SourceCategory ?? string.Empty;
        memento.ExternalJsonSourceIndex = binding.SourceIndex;
        memento.ExternalJsonSourceFragment = binding.SourceJsonFragment ?? string.Empty;
    }

    public static void ApplyFromMemento(GameObject target, LevelObject.Memento memento, bool preserveSourceIndex)
    {
        if (target == null || memento == null || !memento.HasExternalJsonBinding)
            return;

        ExternalJsonObjectBinding binding = target.GetComponent<ExternalJsonObjectBinding>();
        if (binding == null)
            binding = target.AddComponent<ExternalJsonObjectBinding>();

        binding.SourceFormatId = memento.ExternalJsonSourceFormatId;
        binding.SourceCategory = memento.ExternalJsonSourceCategory;
        binding.SourceJsonFragment = memento.ExternalJsonSourceFragment ?? string.Empty;
        binding.SourceIndex = preserveSourceIndex ? memento.ExternalJsonSourceIndex : -1;
    }
}
