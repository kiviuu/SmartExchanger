using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SmartExchanger.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ScatterTextureNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private int _count = 100;

        [ObservableProperty]
        private int _seed = 1;

        [ObservableProperty]
        private float _minimumScale = 0.02f;

        [ObservableProperty]
        private float _maximumScale = 0.07f;

        [ObservableProperty]
        private float _minimumRotationDegrees = -180f;

        [ObservableProperty]
        private float _maximumRotationDegrees = 180f;

        public ConnectorViewModel StampInputConnector { get; }
        public ConnectorViewModel OutpuConnector { get; }

        public ScatterTextureNodeViewModel()
        {
            Title = "Scatter Texture";
            StampInputConnector = new ConnectorViewModel(this, "Stamp", "stampIn");
            OutpuConnector = new ConnectorViewModel(this, "Out", "out");
            Inputs.Add(StampInputConnector);
            Outputs.Add(OutpuConnector);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            var stampImage = inputs.Get(StampInputConnector);
            if (stampImage is null)
            {
                return null;
            }

            ScatterPlacement[] placements = ScatterPlacementGenerator.Generate(Count, Seed, MinimumScale, MaximumScale,
                MinimumRotationDegrees, MaximumRotationDegrees);

            using SKSurface surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

            float maxInputDimension = Math.Max(stampImage.Width, stampImage.Height);
            float widthRatio = stampImage.Width / maxInputDimension;
            float heightRatio = stampImage.Height / maxInputDimension;

            foreach (var placement in placements)
            {
                DrawStamp(canvas, stampImage, size, widthRatio, heightRatio, placement, sampling);
            }
            return surface.Snapshot();
        }

        private static void DrawStamp(SKCanvas canvas, SKImage stampImage, int outputSize, float widthRatio, float heightRatio,
            ScatterPlacement placement, SKSamplingOptions sampling)
        {
            float centerX = placement.X * outputSize;
            float centerY = placement.Y * outputSize;

            float baseStampSize = placement.Scale * outputSize;
            float stampWidth = baseStampSize * widthRatio;
            float stampHeight = baseStampSize * heightRatio;

            var destination = new SKRect(-stampWidth * 0.5f, -stampHeight * 0.5f, stampWidth * 0.5f, stampHeight * 0.5f);

            int canvasSaveCount = canvas.Save();
            try
            {
                canvas.Translate(centerX, centerY);
                canvas.RotateDegrees(placement.RotationDegrees);
                canvas.DrawImage(stampImage, destination, sampling);
            }
            finally
            {
                canvas.RestoreToCount(canvasSaveCount);
            }
        }

        protected override bool IsRenderAffectingProperty(string? propertyName)
        {
            return propertyName is
                      nameof(Count) or
                      nameof(Seed) or
                      nameof(MinimumScale) or
                      nameof(MaximumScale) or
                      nameof(MinimumRotationDegrees) or
                      nameof(MaximumRotationDegrees)
                  ||
                  base.IsRenderAffectingProperty(
                      propertyName);
        }
    }
}
