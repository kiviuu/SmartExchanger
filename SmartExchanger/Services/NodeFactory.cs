using Microsoft.Extensions.Options;
using SmartExchanger.Options;
using SmartExchanger.Services;
using System.IO;

namespace SmartExchanger.Persistence
{
    public sealed class NodeFactory : INodeFactory
    {
        private readonly IShaderService _shaderService;
        private readonly RenderingOptions _renderingOptions;

        public NodeFactory(IShaderService shaderService, IOptions<RenderingOptions> renderingOptions)
        {
            _shaderService = shaderService ?? throw new ArgumentNullException(nameof(shaderService));
            ArgumentNullException.ThrowIfNull(renderingOptions);
            _renderingOptions = renderingOptions.Value;
        }

        public BaseNodeViewModel Create(NodeType nodeType)
        {
            return Create(GetTypeId(nodeType));
        }

        public BaseNodeViewModel Create(string typeId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(typeId);

            return typeId switch
            {
                NodeTypeIds.Color => new ColorNodeViewModel(),
                NodeTypeIds.Value => new ValueNodeViewModel(),
                NodeTypeIds.TextureSize => CreateTextureSizeNode(),
                NodeTypeIds.TextureInput => new TextureInputNodeViewModel(),
                NodeTypeIds.LinearGradient => new LinearGradientNodeViewModel(),
                NodeTypeIds.PerlinNoiseFractal => new PerlinNoiseFractalNodeViewModel(),
                NodeTypeIds.PerlinNoiseTurbulence => new PerlinNoiseTurbulenceNodeViewModel(),
                NodeTypeIds.Blend => new BlendNodeViewModel(),
                NodeTypeIds.Reroute => new RerouteNodeViewModel(),
                NodeTypeIds.Threshold => new ThresholdNodeViewModel(_shaderService),
                NodeTypeIds.Invert => new InvertNodeViewModel(_shaderService),
                NodeTypeIds.WorleyNoise => new WorleyNoiseNodeViewModel(_shaderService),
                NodeTypeIds.HeightToNormal => new HeightToNormalNodeViewModel(_shaderService),
                NodeTypeIds.Translate2D => new Translate2DNodeViewModel(),
                NodeTypeIds.ScatterTexture => new ScatterTextureNodeViewModel(),
                NodeTypeIds.AlphaToMask => new AlphaToMaskNodeViewModel(_shaderService),
                NodeTypeIds.ApplyOpacityMask => new ApplyOpacityMaskNodeViewModel(_shaderService),
                NodeTypeIds.MaterialOutput => new MaterialOutputNodeViewModel(),
                NodeTypeIds.Output => new OutputNodeViewModel(),
                NodeTypeIds.TexturePreview => new TexturePreviewNodeViewModel(),
                _ => throw new InvalidDataException($"The project contains an unsupported node type '{typeId}'.")
            };
        }

        public string GetTypeId(BaseNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return node switch
            {
                ColorNodeViewModel => NodeTypeIds.Color,
                ValueNodeViewModel => NodeTypeIds.Value,
                TextureSizeNodeViewModel => NodeTypeIds.TextureSize,
                TextureInputNodeViewModel => NodeTypeIds.TextureInput,
                LinearGradientNodeViewModel => NodeTypeIds.LinearGradient,
                PerlinNoiseFractalNodeViewModel => NodeTypeIds.PerlinNoiseFractal,
                PerlinNoiseTurbulenceNodeViewModel => NodeTypeIds.PerlinNoiseTurbulence,
                BlendNodeViewModel => NodeTypeIds.Blend,
                RerouteNodeViewModel => NodeTypeIds.Reroute,
                ThresholdNodeViewModel => NodeTypeIds.Threshold,
                InvertNodeViewModel => NodeTypeIds.Invert,
                WorleyNoiseNodeViewModel => NodeTypeIds.WorleyNoise,
                HeightToNormalNodeViewModel => NodeTypeIds.HeightToNormal,
                Translate2DNodeViewModel => NodeTypeIds.Translate2D,
                ScatterTextureNodeViewModel => NodeTypeIds.ScatterTexture,
                AlphaToMaskNodeViewModel => NodeTypeIds.AlphaToMask,
                ApplyOpacityMaskNodeViewModel => NodeTypeIds.ApplyOpacityMask,
                MaterialOutputNodeViewModel => NodeTypeIds.MaterialOutput,
                OutputNodeViewModel => NodeTypeIds.Output,
                TexturePreviewNodeViewModel => NodeTypeIds.TexturePreview,
                _ => throw new InvalidOperationException($"The node class '{node.GetType().FullName}' is not registered in NodeFactory.")
            };
        }

        private TextureSizeNodeViewModel CreateTextureSizeNode()
        {
            var node = new TextureSizeNodeViewModel();

            if (node.AvailableSizes.Contains(_renderingOptions.DefaultTextureSize))
            {
                node.SelectedSize = _renderingOptions.DefaultTextureSize;
            }
            else if (node.AvailableSizes.Count > 0)
            {
                node.SelectedSize = node.AvailableSizes[0];
            }
            return node;
        }

        private static string GetTypeId(NodeType nodeType)
        {
            return nodeType switch
            {
                NodeType.ColorNode => NodeTypeIds.Color,
                NodeType.ValueNode => NodeTypeIds.Value,
                NodeType.TextureSizeNode => NodeTypeIds.TextureSize,
                NodeType.TextureInputNode => NodeTypeIds.TextureInput,
                NodeType.LinearGradientNode => NodeTypeIds.LinearGradient,
                NodeType.PerlinNoiseNode => NodeTypeIds.PerlinNoiseFractal,
                NodeType.PerlinTurbulenceNode => NodeTypeIds.PerlinNoiseTurbulence,
                NodeType.BlendNode => NodeTypeIds.Blend,
                NodeType.RerouteNode => NodeTypeIds.Reroute,
                NodeType.ThresholdNode => NodeTypeIds.Threshold,
                NodeType.InvertNode => NodeTypeIds.Invert,
                NodeType.WorleyNoiseNode => NodeTypeIds.WorleyNoise,
                NodeType.HeightToNormalNode => NodeTypeIds.HeightToNormal,
                NodeType.Translate2DNode => NodeTypeIds.Translate2D,
                NodeType.ScatterTextureNode => NodeTypeIds.ScatterTexture,
                NodeType.AlphaToMaskNode => NodeTypeIds.AlphaToMask,
                NodeType.ApplyOpacityMaskNode => NodeTypeIds.ApplyOpacityMask,
                NodeType.MaterialOutputNode => NodeTypeIds.MaterialOutput,
                NodeType.OutputNode => NodeTypeIds.Output,
                NodeType.TexturePreviewNode => NodeTypeIds.TexturePreview,
                _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unknown node type.")
            };
        }
    }
}