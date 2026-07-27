using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfGradientStop = System.Windows.Media.GradientStop;
using WpfLinearGradientBrush = System.Windows.Media.LinearGradientBrush;
using WpfPoint = System.Windows.Point;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class LinearGradientNodeViewModel : BaseNodeViewModel
    {
        private const int MinimumGradientStops = 2;
        private const int MaximumGradientStops = 8;


        private const float PreviewWidth = 260f;
        private const float PreviewHeight = 44f;

        [ObservableProperty]
        private float _angleDegrees;

        [ObservableProperty]
        private float _offset;

        private Brush _gradientPreviewBrush = Brushes.Transparent;

        public Brush GradientPreviewBrush
        {
            get => _gradientPreviewBrush;
            private set => SetProperty(ref _gradientPreviewBrush, value);
        }

        public ObservableCollection<GradientStopViewModel> GradientStops { get; } = new();

        public string StopsCountText =>$"{GradientStops.Count}/{MaximumGradientStops}";

        public ConnectorViewModel OutputConnector { get; }

        public LinearGradientNodeViewModel()
        {
            Title = "Linear Gradient";

            OutputConnector = new ConnectorViewModel(this, "Out");

            Outputs.Add(OutputConnector);

            AddStopInternal(new GradientStopViewModel(
                    position: 0f,
                    red: 0,
                    green: 0,
                    blue: 0),
                updateNode: false);

            AddStopInternal(new GradientStopViewModel(
                    position: 1f,
                    red: 255,
                    green: 255,
                    blue: 255),
                updateNode: false);

            UpdateGradientPreviewBrush();
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            GradientStopViewModel[] orderedStops = GetOrderedStops();

            if (orderedStops.Length < MinimumGradientStops)
            {
                return null;
            }

            using SKSurface surface = CreateGpuSurface(context, size);

            SKCanvas canvas = surface.Canvas;

            canvas.Clear(SKColors.Transparent);

            GradientLine gradientLine = CalculateGradientLine(AngleDegrees, Offset, size, size);

            SKColor[] colors = orderedStops.Select(stop =>new SKColor(stop.Red, stop.Green, stop.Blue, stop.Alpha)).ToArray();

            float[] positions = orderedStops.Select(stop => NormalizePosition(stop.Position)).ToArray();

            using SKShader gradientShader = SKShader.CreateLinearGradient(
                    new SKPoint(
                        gradientLine.StartX,
                        gradientLine.StartY),
                    new SKPoint(
                        gradientLine.EndX,
                        gradientLine.EndY),
                    colors,
                    positions,
                    SKShaderTileMode.Clamp) ?? throw new InvalidOperationException("SkiaSharp could not create the linear gradient shader.");

            using var paint = new SKPaint
                {
                    Shader = gradientShader,
                    BlendMode = SKBlendMode.Src,
                    IsAntialias =false
                };
            var destination = new SKRect(0, 0, size, size);

            canvas.DrawRect(destination, paint);

            return surface.Snapshot();
        }

        [RelayCommand(CanExecute = nameof(CanAddGradientStop))]
        private void AddGradientStop()
        {
            GradientStopViewModel[] orderedStops = GetOrderedStops();

            if (orderedStops.Length < MinimumGradientStops)
            {
                return;
            }

            GradientStopViewModel leftStop = orderedStops[0];

            GradientStopViewModel rightStop = orderedStops[1];

            float largestGap = GetStopPosition(rightStop) - GetStopPosition(leftStop);

            for (int index = 1; index < orderedStops.Length - 1; index++)
            {
                GradientStopViewModel candidateLeft = orderedStops[index];

                GradientStopViewModel candidateRight = orderedStops[index + 1];

                float candidateGap = GetStopPosition(candidateRight) - GetStopPosition(candidateLeft);

                if (candidateGap <= largestGap)
                {
                    continue;
                }

                largestGap = candidateGap;
                leftStop = candidateLeft;
                rightStop = candidateRight;
            }

            // new point is created in the bigest gap between exsisting points
            float newPosition = ( GetStopPosition(leftStop) + GetStopPosition(rightStop) ) * 0.5f;

            var newStop = new GradientStopViewModel(
                    position: newPosition,
                    red: InterpolateByte(leftStop.Red, rightStop.Red, 0.5f),
                    green: InterpolateByte(leftStop.Green, rightStop.Green, 0.5f),
                    blue: InterpolateByte(leftStop.Blue, rightStop.Blue, 0.5f),
                    alpha: InterpolateByte(leftStop.Alpha, rightStop.Alpha, 0.5f));

            AddStopInternal(newStop, updateNode: true);
        }

        private bool CanAddGradientStop()
        {
            return GradientStops.Count < MaximumGradientStops;
        }

        [RelayCommand(CanExecute = nameof(CanRemoveGradientStop))]
        private void RemoveGradientStop(GradientStopViewModel? stop)
        {
            if (stop is null || GradientStops.Count <= MinimumGradientStops)
            {
                return;
            }

            stop.PropertyChanged -= OnGradientStopPropertyChanged;

            GradientStops.Remove(stop);
            RefreshGradientState();
        }

        private bool CanRemoveGradientStop(GradientStopViewModel? stop)
        {
            return stop is not null && GradientStops.Count > MinimumGradientStops;
        }

        [RelayCommand]
        private void ResetGradient()
        {
            foreach (GradientStopViewModel stop in GradientStops)
            {
                stop.PropertyChanged -= OnGradientStopPropertyChanged;
            }

            GradientStops.Clear();

            AddStopInternal(
                new GradientStopViewModel(
                    position: 0f,
                    red: 0,
                    green: 0,
                    blue: 0),
                updateNode: false);

            AddStopInternal(
                new GradientStopViewModel(
                    position: 1f,
                    red: 255,
                    green: 255,
                    blue: 255),
                updateNode: false);

            RefreshGradientState();
        }

        private void AddStopInternal(GradientStopViewModel stop, bool updateNode)
        {
            stop.PropertyChanged += OnGradientStopPropertyChanged;

            int insertIndex = FindVisualInsertIndex(stop.Position);

            GradientStops.Insert(insertIndex, stop);

            if (updateNode)
            {
                RefreshGradientState();
            }
        }

        private int FindVisualInsertIndex(float position)
        {
            float normalizedPosition = NormalizePosition(position);

            for (int index = 0; index < GradientStops.Count; index++)
            {
                float currentPosition = NormalizePosition(GradientStops[index].Position);

                if (currentPosition > normalizedPosition)
                {
                    return index;
                }
            }

            return GradientStops.Count;
        }

        private void OnGradientStopPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            UpdateGradientPreviewBrush();
            InvalidateRender();
        }

        private void RefreshGradientState()
        {
            OnPropertyChanged(nameof(StopsCountText));

            UpdateGradientPreviewBrush();
            InvalidateRender();

            AddGradientStopCommand.NotifyCanExecuteChanged();

            RemoveGradientStopCommand.NotifyCanExecuteChanged();
        }

        partial void OnAngleDegreesChanged(float value)
        {
            UpdateGradientPreviewBrush();
        }

        partial void OnOffsetChanged(float value)
        {
            UpdateGradientPreviewBrush();
        }

        private void UpdateGradientPreviewBrush()
        {
            GradientStopViewModel[] orderedStops = GetOrderedStops();

            if (orderedStops.Length == 0)
            {
                GradientPreviewBrush = Brushes.Transparent;

                return;
            }

            GradientLine line = CalculateGradientLine(AngleDegrees, Offset, PreviewWidth, PreviewHeight);


            var brush = new WpfLinearGradientBrush
                {
                    MappingMode = BrushMappingMode.RelativeToBoundingBox,

                    SpreadMethod =GradientSpreadMethod.Pad,

                    StartPoint =new WpfPoint(line.StartX / PreviewWidth, line.StartY / PreviewHeight),

                    EndPoint = new WpfPoint(line.EndX / PreviewWidth, line.EndY / PreviewHeight),

                    ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation
                };

            foreach (GradientStopViewModel stop in orderedStops)
            {
                brush.GradientStops.Add(new WpfGradientStop(WpfColor.FromArgb(stop.Alpha, stop.Red, stop.Green, stop.Blue),
                        NormalizePosition(stop.Position)));
            }

            brush.Freeze();

            GradientPreviewBrush = brush;
        }

        private GradientStopViewModel[] GetOrderedStops()
        {
            return GradientStops .OrderBy(stop => NormalizePosition(stop.Position)).ToArray();
        }

        private static GradientLine CalculateGradientLine(float angleDegrees, float offset, float width, float height)
        {
            float angle = NormalizeAngle(angleDegrees);

            float normalizedOffset = NormalizeOffset(offset);

            float radians = angle * MathF.PI / 180f;
            float directionX = MathF.Cos(radians);
            float directionY = MathF.Sin(radians);

            float centerX = width * 0.5f;
            float centerY = height * 0.5f;

            float halfLength = MathF.Abs(directionX) * width * 0.5f + MathF.Abs(directionY) * height * 0.5f;
            float offsetDistance = normalizedOffset * halfLength;

            float gradientCenterX = centerX + directionX * offsetDistance;
            float gradientCenterY = centerY + directionY * offsetDistance;

            return new GradientLine(
                StartX: gradientCenterX - directionX * halfLength,
                StartY: gradientCenterY - directionY * halfLength,

                EndX: gradientCenterX + directionX * halfLength,
                EndY: gradientCenterY + directionY * halfLength);
        }

        private static float NormalizeAngle(float value)
        {
            if (!float.IsFinite(value))
            {
                return 0f;
            }
            value %= 360f;

            if (value > 180f)
            {
                value -= 360f;
            }
            else if (value < -180f)
            {
                value += 360f;
            }
            return value;
        }

        private static float NormalizeOffset(float value)
        {
            if (!float.IsFinite(value))
            {
                return 0f;
            }
            return Math.Clamp(value, -1f, 1f);
        }

        private static float NormalizePosition(float value)
        {
            if (!float.IsFinite(value))
            {
                return 0f;
            }

            return Math.Clamp(value, 0f, 1f);
        }

        private static float GetStopPosition(GradientStopViewModel stop)
        {
            return NormalizePosition(stop.Position);
        }

        private static byte InterpolateByte(byte first, byte second, float amount)
        {
            float result = first + (second - first) * amount;

            return (byte)Math.Clamp( (int)MathF.Round(result), byte.MinValue, byte.MaxValue);
        }

        protected override bool IsRenderAffectingProperty(string? propertyName)
        {
            return propertyName is
                       nameof(AngleDegrees) or
                       nameof(Offset)
                   ||
                   base.IsRenderAffectingProperty(
                       propertyName);
        }

        private readonly record struct GradientLine(float StartX, float StartY, float EndX, float EndY);
    }
}