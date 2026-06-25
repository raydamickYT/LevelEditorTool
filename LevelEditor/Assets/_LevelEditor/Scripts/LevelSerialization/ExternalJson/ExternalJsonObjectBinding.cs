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
}
