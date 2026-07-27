using SkiaSharp;
using SmartExchanger.Models;
using SmartExchanger.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SmartExchanger.Services
{
    public sealed class NodeStateSerializer : INodeStateSerializer
    {
        public JsonElement CaptureState(BaseNodeViewModel node,  string projectDirectory)
        {
            ArgumentNullException.ThrowIfNull(node);

            ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

            object state = node switch
            {
                ColorNodeViewModel color =>
                    new ColorNodeState(
                        color.R,
                        color.G,
                        color.B),

                ValueNodeViewModel value =>
                    new ValueNodeState(
                        value.Value),

                TextureSizeNodeViewModel textureSize =>
                    new TextureSizeNodeState(
                        textureSize.SelectedSize),

                TextureInputNodeViewModel textureInput =>
                    new TextureInputNodeState(
                        ConvertTexturePathForSave(
                            textureInput.FilePath,
                            projectDirectory)),

                LinearGradientNodeViewModel linearGradient =>
                    new LinearGradientNodeState(
                        linearGradient.AngleDegrees,
                        linearGradient.Offset,
                        linearGradient.GradientStops
                            .Select(
                                stop =>
                                    new GradientStopState(
                                        stop.Position,
                                        stop.Red,
                                        stop.Green,
                                        stop.Blue,
                                        stop.Alpha))
                            .ToArray()),

                PerlinNoiseFractalNodeViewModel perlinFractal =>
                    new PerlinNoiseFractalNodeState(
                        perlinFractal.Seed,
                        perlinFractal.FrequencyX,
                        perlinFractal.FrequencyY,
                        perlinFractal.Octaves),

                PerlinNoiseTurbulenceNodeViewModel perlinTurbulence =>
                    new PerlinNoiseTurbulenceNodeState(
                        perlinTurbulence.Seed,
                        perlinTurbulence.FrequencyX,
                        perlinTurbulence.FrequencyY,
                        perlinTurbulence.Octaves),

                BlendNodeViewModel blend =>
                    new BlendNodeState(
                        blend.Factor,
                        blend.SelectedBlendMode),

                ThresholdNodeViewModel threshold =>
                    new ThresholdNodeState(
                        threshold.Threshold,
                        threshold.Softness),

                InvertNodeViewModel invert =>
                    new InvertNodeState(
                        invert.Strength),

                WorleyNoiseNodeViewModel worley =>
                    new WorleyNoiseNodeState(
                        worley.Scale,
                        worley.Jitter,
                        worley.Seed),

                HeightToNormalNodeViewModel heightToNormal =>
                    new HeightToNormalNodeState(
                        heightToNormal.Strength,
                        heightToNormal.InvertY),

                Translate2DNodeViewModel translate2D =>
                    new Translate2DNodeState(
                        translate2D.OffsetX,
                        translate2D.OffsetY,
                        translate2D.RotationDegrees,
                        translate2D.Wrap),

                ScatterTextureNodeViewModel scatter =>
                    new ScatterTextureNodeState(
                        scatter.Count,
                        scatter.Seed,
                        scatter.MinimumScale,
                        scatter.MaximumScale,
                        scatter.MinimumRotationDegrees,
                        scatter.MaximumRotationDegrees),

                AlphaToMaskNodeViewModel alphaToMask =>
                    new AlphaToMaskNodeState(
                        alphaToMask.Strength),

                ApplyOpacityMaskNodeViewModel => new EmptyNodeState(),

                RerouteNodeViewModel => new EmptyNodeState(),

                MaterialOutputNodeViewModel => new EmptyNodeState(),

                OutputNodeViewModel => new EmptyNodeState(),

                TexturePreviewNodeViewModel => new EmptyNodeState(),

                _ => throw new InvalidOperationException($"The state of node class '{node.GetType().FullName}' is not supported.")
            };

            return JsonSerializer.SerializeToElement(state, state.GetType(), GraphJsonOptions.Instance);
        }

        public void RestoreState(BaseNodeViewModel node, JsonElement state, string projectDirectory, ICollection<string> warnings)
        {
            ArgumentNullException.ThrowIfNull(node);

            ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);

            ArgumentNullException.ThrowIfNull(warnings);

            switch (node)
            {
                case ColorNodeViewModel color:
                    {
                        ColorNodeState data = Deserialize<ColorNodeState>(state, node);
                        color.R = data.R;
                        color.G = data.G;
                        color.B = data.B;

                        break;
                    }

                case ValueNodeViewModel value:
                    {
                        ValueNodeState data = Deserialize<ValueNodeState>(state, node);
                        value.Value = data.Value;

                        break;
                    }

                case TextureSizeNodeViewModel textureSize:
                    {
                        TextureSizeNodeState data = Deserialize<TextureSizeNodeState>(state, node);

                        if (!textureSize.AvailableSizes.Contains(data.SelectedSize))
                        {
                            throw new InvalidDataException($"Texture size '{data.SelectedSize}' is not supported.");
                        }
                        textureSize.SelectedSize = data.SelectedSize;

                        break;
                    }

                case TextureInputNodeViewModel textureInput:
                    {
                        TextureInputNodeState data = Deserialize<TextureInputNodeState>(state, node);

                        if (string.IsNullOrWhiteSpace(data.FilePath))
                        {
                            break;
                        }

                        string resolvedPath = ResolveTexturePath(data.FilePath, projectDirectory);
                        if (!textureInput.TryLoadTextureFromPath(resolvedPath, out string? errorMessage, invalidateRender: false))
                        {
                            warnings.Add($"Texture Input node '{node.Id}' could not load '{resolvedPath}'. {errorMessage}");
                        }
                        break;
                    }

                case LinearGradientNodeViewModel linearGradient:
                    {
                        LinearGradientNodeState data = Deserialize<LinearGradientNodeState>(state, node);

                        if (data.Stops is null)
                        {
                            throw new InvalidDataException("Linear Gradient node does not contain a gradient stop collection.");
                        }

                        linearGradient.AngleDegrees = data.AngleDegrees;

                        linearGradient.Offset =data.Offset;

                        linearGradient.ReplaceGradientStops(
                            data.Stops.Select(
                                stop =>
                                    new GradientStopViewModel(stop.Position, stop.Red, stop.Green, stop.Blue, stop.Alpha)));
                        break;
                    }

                case PerlinNoiseFractalNodeViewModel perlinFractal:
                    {
                        PerlinNoiseFractalNodeState data =
                            Deserialize<PerlinNoiseFractalNodeState>(
                                state,
                                node);

                        perlinFractal.Seed =
                            data.Seed;

                        perlinFractal.FrequencyX =
                            data.FrequencyX;

                        perlinFractal.FrequencyY =
                            data.FrequencyY;

                        perlinFractal.Octaves =
                            data.Octaves;

                        break;
                    }

                case PerlinNoiseTurbulenceNodeViewModel perlinTurbulence:
                    {
                        PerlinNoiseTurbulenceNodeState data = Deserialize<PerlinNoiseTurbulenceNodeState>(state, node);

                        perlinTurbulence.Seed = data.Seed;
                        perlinTurbulence.FrequencyX = data.FrequencyX;
                        perlinTurbulence.FrequencyY = data.FrequencyY;
                        perlinTurbulence.Octaves = data.Octaves;
                        break;
                    }

                case BlendNodeViewModel blend:
                    {
                        BlendNodeState data = Deserialize<BlendNodeState>(state, node);

                        if (!Enum.IsDefined(typeof(SKBlendMode), data.SelectedBlendMode))
                        {
                            throw new InvalidDataException($"Blend mode '{data.SelectedBlendMode}' is not supported.");
                        }

                        blend.Factor = data.Factor;
                        blend.SelectedBlendMode = data.SelectedBlendMode;

                        break;
                    }

                case ThresholdNodeViewModel threshold:
                    {
                        ThresholdNodeState data = Deserialize<ThresholdNodeState>(state, node);
                        threshold.Threshold = data.Threshold;
                        threshold.Softness = data.Softness;

                        break;
                    }

                case InvertNodeViewModel invert:
                    {
                        InvertNodeState data = Deserialize<InvertNodeState>(state, node);
                        invert.Strength = data.Strength;

                        break;
                    }

                case WorleyNoiseNodeViewModel worley:
                    {
                        WorleyNoiseNodeState data = Deserialize<WorleyNoiseNodeState>(state, node);
                        worley.Scale = data.Scale;
                        worley.Jitter = data.Jitter;
                        worley.Seed = data.Seed;

                        break;
                    }

                case HeightToNormalNodeViewModel heightToNormal:
                    {
                        HeightToNormalNodeState data = Deserialize<HeightToNormalNodeState>(state, node);
                        heightToNormal.Strength = data.Strength;
                        heightToNormal.InvertY = data.InvertY;

                        break;
                    }

                case Translate2DNodeViewModel translate2D:
                    {
                        Translate2DNodeState data =
                            Deserialize<Translate2DNodeState>(
                                state,
                                node);

                        translate2D.OffsetX =
                            data.OffsetX;

                        translate2D.OffsetY =
                            data.OffsetY;

                        translate2D.RotationDegrees =
                            data.RotationDegrees;

                        translate2D.Wrap =
                            data.Wrap;

                        break;
                    }

                case ScatterTextureNodeViewModel scatter:
                    {
                        ScatterTextureNodeState data = Deserialize<ScatterTextureNodeState>(state, node);
                        scatter.Count = data.Count;
                        scatter.Seed = data.Seed;
                        scatter.MinimumScale = data.MinimumScale;
                        scatter.MaximumScale = data.MaximumScale;
                        scatter.MinimumRotationDegrees = data.MinimumRotationDegrees;
                        scatter.MaximumRotationDegrees = data.MaximumRotationDegrees;

                        break;
                    }

                case AlphaToMaskNodeViewModel alphaToMask:
                    {
                        AlphaToMaskNodeState data = Deserialize<AlphaToMaskNodeState>(state, node);
                        alphaToMask.Strength = data.Strength;

                        break;
                    }

                case ApplyOpacityMaskNodeViewModel:
                case RerouteNodeViewModel:
                case MaterialOutputNodeViewModel:
                case OutputNodeViewModel:
                case TexturePreviewNodeViewModel:
                    break;

                default:
                    throw new InvalidOperationException($"The state of node class '{node.GetType().FullName}' is not supported.");
            }
        }

        private static TState Deserialize<TState>(JsonElement state, BaseNodeViewModel node)
        {
            if (state.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                throw new InvalidDataException($"Node '{node.Id}' does not contain a state object.");
            }

            try
            {
                return state.Deserialize<TState>(GraphJsonOptions.Instance)
                       ?? throw new InvalidDataException($"Node '{node.Id}' contains an empty state object.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"State of node '{node.Id}' is invalid.", ex);
            }
        }

        private static string? ConvertTexturePathForSave(string? texturePath,string projectDirectory)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return null;
            }

            string fullTexturePath = Path.GetFullPath(texturePath);
            string fullProjectDirectory = Path.GetFullPath(projectDirectory);
            string? textureRoot = Path.GetPathRoot(fullTexturePath);
            string? projectRoot =  Path.GetPathRoot(fullProjectDirectory);

            if (!string.Equals(textureRoot, projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return fullTexturePath;
            }

            return Path.GetRelativePath(fullProjectDirectory, fullTexturePath);
        }

        private static string ResolveTexturePath(string savedPath,string projectDirectory)
        {
            if (Path.IsPathRooted(savedPath))
            {
                return Path.GetFullPath(savedPath);
            }

            return Path.GetFullPath(Path.Combine(projectDirectory, savedPath));
        }
    }
}
