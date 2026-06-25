using System;
using System.Collections.Generic;

[Serializable]
public sealed class ExternalJsonImportProfile
{
    public string formatId = "custom.json";
    public string displayName = "Custom JSON";
    public float pixelScale = 0.01f;
    public string viewportWidthPath = "";
    public string viewportHeightPath = "";
    public ExternalJsonObjectSourceProfile[] objectSources = System.Array.Empty<ExternalJsonObjectSourceProfile>();

    public System.Collections.Generic.List<ExternalJsonObjectSourceProfile> GetObjectSourcesList()
    {
        var list = new System.Collections.Generic.List<ExternalJsonObjectSourceProfile>();
        if (objectSources != null)
            list.AddRange(objectSources);
        return list;
    }

    public void SetObjectSources(System.Collections.Generic.IEnumerable<ExternalJsonObjectSourceProfile> sources)
    {
        objectSources = sources == null
            ? System.Array.Empty<ExternalJsonObjectSourceProfile>()
            : new System.Collections.Generic.List<ExternalJsonObjectSourceProfile>(sources).ToArray();
    }
}

[Serializable]
public sealed class ExternalJsonObjectSourceProfile
{
    public bool enabled = true;
    public string id = "";
    public string displayName = "";
    public string jsonPath = "";
    public bool isArray = true;
    public ExternalJsonShapeKind shape = ExternalJsonShapeKind.RectObject;
    public string xField = "x";
    public string yField = "y";
    public string widthField = "width";
    public string heightField = "height";
    public float defaultWidth = 32f;
    public float defaultHeight = 32f;
    public string spriteAssetId = "";
}
