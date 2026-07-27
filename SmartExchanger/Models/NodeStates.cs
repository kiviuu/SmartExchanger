using SkiaSharp;

namespace SmartExchanger.Models
{
    public sealed record EmptyNodeState;

    public sealed record ColorNodeState(byte R, byte G, byte B);

    public sealed record ValueNodeState(float Value);

    public sealed record TextureSizeNodeState(int SelectedSize);

    public sealed record TextureInputNodeState(string? FilePath);

    public sealed record LinearGradientNodeState(float AngleDegrees, float Offset, IReadOnlyList<GradientStopState> Stops);

    public sealed record GradientStopState(float Position, byte Red, byte Green, byte Blue,byte Alpha);

    public sealed record PerlinNoiseFractalNodeState(float Seed, float FrequencyX, float FrequencyY, int Octaves);

    public sealed record PerlinNoiseTurbulenceNodeState(float Seed, float FrequencyX, float FrequencyY, int Octaves);

    public sealed record BlendNodeState(float Factor, SKBlendMode SelectedBlendMode);

    public sealed record ThresholdNodeState(float Threshold, float Softness);

    public sealed record InvertNodeState(float Strength);

    public sealed record WorleyNoiseNodeState(float Scale, float Jitter, float Seed);

    public sealed record HeightToNormalNodeState(float Strength, bool InvertY);

    public sealed record Translate2DNodeState(float OffsetX, float OffsetY, float RotationDegrees, bool Wrap);

    public sealed record ScatterTextureNodeState(int Count, int Seed, float MinimumScale, float MaximumScale,
        float MinimumRotationDegrees, float MaximumRotationDegrees);

    public sealed record AlphaToMaskNodeState(float Strength);
}
