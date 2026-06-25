using System.Collections.Generic;

public sealed class JsonStructureScanResult
{
    public readonly List<JsonArrayCandidate> ArrayCandidates = new();
    public readonly List<JsonSingleObjectCandidate> SingleObjectCandidates = new();
    public readonly List<JsonScalarFieldCandidate> ScalarFieldCandidates = new();
}

public sealed class JsonArrayCandidate
{
    public string Path = "";
    public ExternalJsonShapeKind InferredShape = ExternalJsonShapeKind.RectObject;
    public int ItemCount;
    public readonly List<string> ObjectFieldNames = new();
    public string SampleJson = "";
}

public sealed class JsonSingleObjectCandidate
{
    public string Path = "";
    public ExternalJsonShapeKind InferredShape = ExternalJsonShapeKind.PointObject;
    public readonly List<string> FieldNames = new();
    public string SampleJson = "";
}

public sealed class JsonScalarFieldCandidate
{
    public string Path = "";
    public string FieldName = "";
    public float SampleValue;
}
