using System;
using UnityEngine;

public sealed class LevelViewportFrameState
{
    public static readonly Color DefaultOutlineColor = new(1f, 0f, 0f, 0.85f);

    public static LevelViewportFrameState Instance { get; } = new();

    bool _enabled;
    bool _isSelected;
    float _pixelX;
    float _pixelY;
    float _pixelWidth = 600f;
    float _pixelHeight = 600f;
    float _pixelScale = ExternalJsonCoordinateUtil.DefaultPixelScale;
    bool _lockAspectRatio;
    float _lockedAspectWidthOverHeight = 1f;
    Color _outlineColor = DefaultOutlineColor;

    public event Action Changed;

    public bool Enabled
    {
        get => _enabled;
        set => SetField(ref _enabled, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        private set => SetField(ref _isSelected, value);
    }

    public float PixelX
    {
        get => _pixelX;
        set => SetField(ref _pixelX, value);
    }

    public float PixelY
    {
        get => _pixelY;
        set => SetField(ref _pixelY, value);
    }

    public float PixelWidth
    {
        get => _pixelWidth;
        set
        {
            float clamped = Mathf.Max(1f, value);
            if (Mathf.Approximately(_pixelWidth, clamped))
                return;

            _pixelWidth = clamped;
            if (_lockAspectRatio)
                _pixelHeight = Mathf.Max(1f, _pixelWidth / Mathf.Max(0.0001f, _lockedAspectWidthOverHeight));

            NotifyChanged();
        }
    }

    public float PixelHeight
    {
        get => _pixelHeight;
        set
        {
            float clamped = Mathf.Max(1f, value);
            if (Mathf.Approximately(_pixelHeight, clamped))
                return;

            _pixelHeight = clamped;
            if (_lockAspectRatio)
                _pixelWidth = Mathf.Max(1f, _pixelHeight * _lockedAspectWidthOverHeight);

            NotifyChanged();
        }
    }

    public float PixelScale
    {
        get => _pixelScale;
        set => SetField(ref _pixelScale, Mathf.Max(0.0001f, value));
    }

    public bool LockAspectRatio
    {
        get => _lockAspectRatio;
        set
        {
            if (_lockAspectRatio == value)
                return;

            _lockAspectRatio = value;
            if (_lockAspectRatio)
                _lockedAspectWidthOverHeight = Mathf.Max(0.0001f, _pixelWidth / Mathf.Max(1f, _pixelHeight));

            NotifyChanged();
        }
    }

    public Color OutlineColor
    {
        get => _outlineColor;
        set
        {
            if (ColorsApproximatelyEqual(_outlineColor, value))
                return;

            _outlineColor = value;
            NotifyChanged();
        }
    }

    public Color SelectedOutlineColor
    {
        get
        {
            Color c = _outlineColor;
            c.r = Mathf.Clamp01(c.r + 0.15f);
            c.g = Mathf.Clamp01(c.g + 0.15f);
            c.b = Mathf.Clamp01(c.b + 0.15f);
            c.a = Mathf.Clamp01(c.a + 0.1f);
            return c;
        }
    }

    public Bounds WorldBounds
        => LevelViewportFrameUtil.PixelRectToWorldBounds(_pixelX, _pixelY, _pixelWidth, _pixelHeight, _pixelScale);

    public void Select()
    {
        if (IsSelected)
            return;

        IsSelected = true;
    }

    public void Deselect()
    {
        if (!IsSelected)
            return;

        IsSelected = false;
    }

    public void ApplyRecord(LevelViewportRecord record)
    {
        if (record == null)
        {
            ResetToDefaults();
            return;
        }

        _enabled = record.enabled;
        _pixelX = record.pixelX;
        _pixelY = record.pixelY;
        _pixelWidth = Mathf.Max(1f, record.pixelWidth);
        _pixelHeight = Mathf.Max(1f, record.pixelHeight);
        _pixelScale = Mathf.Max(0.0001f, record.pixelScale);
        _lockAspectRatio = record.lockAspectRatio;
        _lockedAspectWidthOverHeight = Mathf.Max(0.0001f, _pixelWidth / Mathf.Max(1f, _pixelHeight));
        bool hasSavedOutlineColor = record.outlineR > 0f
            || record.outlineG > 0f
            || record.outlineB > 0f
            || record.outlineA > 0f;
        _outlineColor = hasSavedOutlineColor
            ? new Color(
                record.outlineR,
                record.outlineG,
                record.outlineB,
                record.outlineA > 0f ? record.outlineA : DefaultOutlineColor.a)
            : DefaultOutlineColor;
        _isSelected = false;
        NotifyChanged();
    }

    public LevelViewportRecord ToRecord()
    {
        return new LevelViewportRecord
        {
            enabled = _enabled,
            pixelX = _pixelX,
            pixelY = _pixelY,
            pixelWidth = _pixelWidth,
            pixelHeight = _pixelHeight,
            pixelScale = _pixelScale,
            lockAspectRatio = _lockAspectRatio,
            outlineR = _outlineColor.r,
            outlineG = _outlineColor.g,
            outlineB = _outlineColor.b,
            outlineA = _outlineColor.a,
        };
    }

    public void ResetToDefaults()
    {
        _enabled = false;
        _isSelected = false;
        _pixelX = 0f;
        _pixelY = 0f;
        _pixelWidth = 600f;
        _pixelHeight = 600f;
        _pixelScale = ExternalJsonCoordinateUtil.DefaultPixelScale;
        _lockAspectRatio = false;
        _lockedAspectWidthOverHeight = 1f;
        _outlineColor = DefaultOutlineColor;
        NotifyChanged();
    }

    public void ApplyPlatformerDefaults()
    {
        _enabled = true;
        _isSelected = false;
        _pixelX = 0f;
        _pixelY = 0f;
        _pixelWidth = 600f;
        _pixelHeight = 600f;
        _pixelScale = ExternalJsonCoordinateUtil.DefaultPixelScale;
        _lockAspectRatio = true;
        _lockedAspectWidthOverHeight = 1f;
        _outlineColor = DefaultOutlineColor;
        NotifyChanged();
    }

    public void MoveByPixelDelta(Vector2 pixelDelta)
    {
        if (pixelDelta.sqrMagnitude <= 0f)
            return;

        _pixelX += pixelDelta.x;
        _pixelY += pixelDelta.y;
        NotifyChanged();
    }

    static bool ColorsApproximatelyEqual(Color a, Color b)
        => Mathf.Approximately(a.r, b.r)
            && Mathf.Approximately(a.g, b.g)
            && Mathf.Approximately(a.b, b.b)
            && Mathf.Approximately(a.a, b.a);

    void SetField(ref bool field, bool value)
    {
        if (field == value)
            return;

        field = value;
        NotifyChanged();
    }

    void SetField(ref float field, float value)
    {
        if (Mathf.Approximately(field, value))
            return;

        field = value;
        NotifyChanged();
    }

    void NotifyChanged() => Changed?.Invoke();
}
