using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Win32;
using SkiaSharp;
using SmartExchanger.Models;
using SmartExchanger.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartExchanger.ViewModels
{
    /// <summary>
    /// Manages graph of nodes and a persistent shared renderer
    /// - in entire editor exists only one persistent SKGElement and its GRContext
    /// - OutpuNode does not have own SKGElement or GRContext
    /// - SKImages are created only for one rendering process and after preview rendering GPU result is copied to WritableBitmap
    /// - every SKImage has only one owner and it is Disposed in deterministic way
    /// </summary>
    public partial class EditorViewModel : ObservableObject, IDisposable
    {
        private readonly IShaderService shaderService;

        private const int PreviewWidth = 256;
        private const int PreviewHeight = 256;

        // GPU resource caching params
        private const long MinimumGpuCacheBytes = 16L * 1024L * 1024L;
        private const long MaximumGpuCacheBytes = 96L * 1024L * 1024L;

        public ObservableCollection<BaseNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
        public ObservableCollection<ConnectionViewModel> SelectedConnections { get; } = new();

        private readonly Dictionary<BaseNodeViewModel, Action> _nodePropertyHandlers =
            new(ReferenceComparer<BaseNodeViewModel>.Instance);

        private ConnectorViewModel? _pendingSourceConnector;

        private readonly Queue<PendingExportRequest> _pendingExports = new();

        private const int _materialPreviewTextureSize = 512;
        public event Action<MaterialPreviewFrame>? MaterialPreviewFrameReady;


        // One persistnet SKGElement is the owner of this GRContext
        private GRContext? _graphicsContext;
        private Action? _requestGpuRender;

        private long _graphRevision;
        private long _renderedGraphRevision = -1;
        private long _configuredGpuCacheLimitBytes;

        private bool _purgeOnNextRender = true;
        private bool _isRendering;
        private bool _isDisposed;

        public EditorViewModel(IShaderService shaderService)
        {
            this.shaderService = shaderService ?? throw new ArgumentNullException(nameof(shaderService));
            SetupDefaultScene();
            UpdateConnectorStates();
        }

        public void SetGpuRenderRequest(Action? requestGpuRender)
        {
            if (_isDisposed)
            {
                return;
            }

            _requestGpuRender = requestGpuRender;

            if (requestGpuRender is not null)
            {
                _renderedGraphRevision = -1;
                _purgeOnNextRender = true;
                requestGpuRender();
            }
        }

        public void SetGraphicsContext(GRContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (_isDisposed || ReferenceEquals(_graphicsContext, context))
            {
                return;
            }

            _graphicsContext = context;
            _configuredGpuCacheLimitBytes = 0;
            _renderedGraphRevision = -1;
            _purgeOnNextRender = true;
        }

        private void SetupDefaultScene()
        {
            AddNodeInternal(new TextureSizeNodeViewModel
            {
                Location = new Point(0, 0)
            });
        }

        private void AddNodeInternal(BaseNodeViewModel node)
        {
            Action handler = () => OnNodePropertiesChanged(node);
            _nodePropertyHandlers.Add(node, handler);
            node.PropsChanged += handler;
            Nodes.Add(node);
        }

        private void DetachNode(BaseNodeViewModel node)
        {
            if (_nodePropertyHandlers.Remove(node, out var handler))
            {
                node.PropsChanged -= handler;
            }
        }

        private void OnNodePropertiesChanged(BaseNodeViewModel node)
        {
            InvalidateGraph(requestGpuPurge: node is TextureSizeNodeViewModel);
        }

        [RelayCommand]
        private void StartConnection(object? parameter)
        {
            if (!_isDisposed)
            {
                _pendingSourceConnector = parameter as ConnectorViewModel;
            }
        }

        [RelayCommand]
        private void CompleteConnection(object? parameter)
        {
            if (_isDisposed)
            {
                _pendingSourceConnector = null;
                return;
            }

            try
            {
                if (parameter is not ValueTuple<object, object> tuple ||
                    tuple.Item2 is not ConnectorViewModel targetConnector)
                {
                    return;
                }

                var sourceConnector = _pendingSourceConnector;
                if (sourceConnector is null || sourceConnector == targetConnector)
                {
                    return;
                }

                if (sourceConnector.Node == targetConnector.Node)
                {
                    return;
                }

                bool isSourceOutput = sourceConnector.Node.Outputs.Contains(sourceConnector);
                bool isTargetInput = targetConnector.Node.Inputs.Contains(targetConnector);
                if (!isSourceOutput || !isTargetInput)
                {
                    return;
                }

                if (WouldCreateCycle(sourceConnector.Node, targetConnector.Node))
                {
                    return;
                }

                var previous = Connections.FirstOrDefault(c => c.Target == targetConnector);
                if (previous is not null)
                {
                    RemoveConnectionInternal(previous);
                }

                Connections.Add(new ConnectionViewModel(sourceConnector, targetConnector));
                InvalidateGraph(requestGpuPurge: true);
            }
            finally
            {
                _pendingSourceConnector = null;
            }
        }

        [RelayCommand]
        private void DisconnectConnector(object? parameter)
        {
            if (_isDisposed || parameter is not ConnectorViewModel connector)
            {
                return;
            }

            var toRemove = Connections
                .Where(c => c.Source == connector || c.Target == connector)
                .ToList();

            if (toRemove.Count == 0)
            {
                return;
            }

            foreach (var connection in toRemove)
            {
                RemoveConnectionInternal(connection);
            }

            InvalidateGraph(requestGpuPurge: true);
        }

        [RelayCommand]
        private void RemoveConnection(object? parameter)
        {
            if (_isDisposed || parameter is not ConnectionViewModel connection)
            {
                return;
            }

            if (!Connections.Contains(connection))
            {
                SelectedConnections.Remove(connection);
                return;
            }

            RemoveConnectionInternal(connection);
            InvalidateGraph(requestGpuPurge: true);
        }

        private void RemoveConnectionInternal(ConnectionViewModel connection)
        {
            Connections.Remove(connection);
            SelectedConnections.Remove(connection);
        }

        private void InvalidateGraph(bool requestGpuPurge)
        {
            unchecked
            {
                _graphRevision++;
            }

            _purgeOnNextRender |= requestGpuPurge;
            UpdateConnectorStates();
            RequestGpuRender();
        }

        private void UpdateConnectorStates()
        {
            foreach (var node in Nodes)
            {
                foreach (var input in node.Inputs)
                {
                    input.IsConnected = Connections.Any(c => c.Target == input);
                }

                foreach (var output in node.Outputs)
                {
                    output.IsConnected = Connections.Any(c => c.Source == output);
                }
            }
        }

        private void RequestGpuRender()
        {
            _requestGpuRender?.Invoke();
        }

        /// <summary>
        /// Invoked only from persistent SKGElement (from PaintSurface event)
        /// </summary>
        public void RenderPendingOutputs(GRContext context, SKCanvas hostCanvas)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(hostCanvas);

            hostCanvas.Clear(SKColors.Transparent);

            if (_isDisposed || context.IsAbandoned || _isRendering)
            {
                return;
            }

            SetGraphicsContext(context);
            ConfigureGpuResourceCache(context, GetTextureSize());

            bool graphChanged = _renderedGraphRevision != _graphRevision;
            bool hasPendingExports = _pendingExports.Count > 0;

            if (!graphChanged && !_purgeOnNextRender && !hasPendingExports)
            {
                return;
            }

            _isRendering = true;

            try
            {
                // Purge has to be invoked with active OpenGL context!
                if (_purgeOnNextRender)
                {
                    FlushAndPurge(context);
                }

                if (graphChanged)
                {
                    int textureSize = GetTextureSize();
                    var outputs = Nodes.OfType<OutputNodeViewModel>().ToList();

                    foreach (var output in outputs)
                    {
                        RenderOutputPreview(output, context, textureSize);
                    }

                    RenderMaterialPreview(context);
                    _renderedGraphRevision = _graphRevision;
                }
                ProcessPendingExports(context);

                FlushAndPurge(context);
                _purgeOnNextRender = false;

                LogGpuCacheUsage(context, GetTextureSize());
            }
            catch (Exception exception)
            {
                _renderedGraphRevision = -1;
                _purgeOnNextRender = true;
                Debug.WriteLine($"[Skia GPU] Render failed: {exception}");
                throw;
            }
            finally
            {
                _isRendering = false;
            }
        }

        private void RenderOutputPreview(OutputNodeViewModel output, GRContext context, int textureSize)
        {
            SKImage? finalImage = null;

            try
            {
                finalImage = BuildOutputImage(output, context, textureSize);

                if (finalImage is null)
                {
                    output.ClearPreview();
                    return;
                }

                using var previewBitmap = CreatePreviewBitmap(
                    context,
                    finalImage,
                    PreviewWidth,
                    PreviewHeight);

                output.UpdatePreview(previewBitmap);
            }
            finally
            {
                finalImage?.Dispose();
            }
        }

        private static SKBitmap CreatePreviewBitmap(GRContext context, SKImage source, int width, int height)
        {
            var previewInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            using var previewSurface = SKSurface.Create(context, true, previewInfo)
                ?? throw new InvalidOperationException(
                    "SkiaSharp could not create the GPU preview surface.");

            var canvas = previewSurface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var prevewSampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            canvas.DrawImage(source, new SKRect(0, 0, width, height), prevewSampling);

            // ReadPixels copy data from GPU into small CPU buffor - this process required preview surface rendering completion
            context.Flush(submit: true, synchronous: true);

            var bitmap = new SKBitmap(previewInfo);
            bool readSucceeded = previewSurface.ReadPixels(previewInfo, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);

            if (!readSucceeded)
            {
                bitmap.Dispose();
                throw new InvalidOperationException(
                    "SkiaSharp could not read the GPU preview into CPU memory.");
            }

            return bitmap;
        }

        private int GetTextureSize()
        {
            return Nodes.OfType<TextureSizeNodeViewModel>()
                       .FirstOrDefault()?.SelectedSize ?? 512;
        }

        /// <summary>
        /// Conigure GPU resource caching
        /// </summary>
        private void ConfigureGpuResourceCache(GRContext context, int textureSize)
        {
            long oneTextureBytes = checked((long)textureSize * textureSize * 4L);
            long desiredLimit = Math.Clamp(
                oneTextureBytes,
                MinimumGpuCacheBytes,
                MaximumGpuCacheBytes);

            if (_configuredGpuCacheLimitBytes == desiredLimit)
            {
                return;
            }

            context.SetResourceCacheLimit(desiredLimit);
            _configuredGpuCacheLimitBytes = desiredLimit;
        }

        private static void FlushAndPurge(GRContext context)
        {
            if (context.IsAbandoned)
            {
                return;
            }

            context.Flush(submit: true, synchronous: true);
            context.PurgeUnlockedResources(scratchResourcesOnly: false);
        }

        /// <summary>
        /// Render one path leading to the targeted OutputNodeVM
        /// Returns final SKImage whose owner is invoking object
        /// </summary>
        /// 
        private SKImage? BuildOutputImage(OutputNodeViewModel targetOutput, GRContext context, int textureSize)
        {
            // this is wrapper for existing usages
            return BuildImageForInput(
                targetOutput,
                targetOutput.InputConnector,
                context,
                textureSize);
        }

        private SKImage? BuildImageForInput(BaseNodeViewModel targetNode, ConnectorViewModel targetInput,GRContext context, int textureSize)
        {
            var sortedNodes = GetTopologicallySortedNodes(targetNode, targetInput);
            var reachableNodes = new HashSet<BaseNodeViewModel>(
                sortedNodes,
                ReferenceComparer<BaseNodeViewModel>.Instance);

            var relevantConnections = Connections
                .Where(c => reachableNodes.Contains(c.Source.Node) &&
                            reachableNodes.Contains(c.Target.Node) &&
                            (c.Target.Node != targetNode || c.Target == targetInput))
                .ToList();

            var incomingByNode = relevantConnections
                .GroupBy(c => c.Target.Node, ReferenceComparer<BaseNodeViewModel>.Instance)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList(),
                    ReferenceComparer<BaseNodeViewModel>.Instance);

            var remainingConsumers = relevantConnections
                .GroupBy(c => c.Source.Node, ReferenceComparer<BaseNodeViewModel>.Instance)
                .ToDictionary(
                    group => group.Key,
                    group => group.Count(),
                    ReferenceComparer<BaseNodeViewModel>.Instance);

            var liveImages = new Dictionary<BaseNodeViewModel, SKImage>(
                ReferenceComparer<BaseNodeViewModel>.Instance);

            SKImage? resultImage = null;

            try
            {
                foreach (var node in sortedNodes)
                {
                    incomingByNode.TryGetValue(node, out var incomingConnections);
                    incomingConnections ??= new List<ConnectionViewModel>();

                    if (node == targetNode)
                    {
                        var finalConnection = incomingConnections.FirstOrDefault(
                            c => c.Target == targetInput);

                        if (finalConnection is not null &&
                            liveImages.Remove(finalConnection.Source.Node, out var finalImage))
                        {
                            resultImage = finalImage;
                        }

                        continue;
                    }

                    if (!node.ProducesTexture)
                    {
                        ReleaseConsumedInputs(incomingConnections, remainingConsumers, liveImages);
                        continue;
                    }

                    var inputImages = new Dictionary<ConnectorViewModel, SKImage>();
                    foreach (var connection in incomingConnections)
                    {
                        if (liveImages.TryGetValue(connection.Source.Node, out var sourceImage))
                        {
                            inputImages[connection.Target] = sourceImage;
                        }
                    }

                    var renderInputs = new NodeRenderInputs(inputImages);
                    var renderedImage = node.Render(context, textureSize, renderInputs);

                    if (renderedImage is not null)
                    {
                        if (inputImages.Values.Any(
                                input => ReferenceEquals(input, renderedImage)))
                        {
                            renderedImage.Dispose();
                            throw new InvalidOperationException(
                                $"Node '{node.Title}' returned a borrowed input SKImage. " +
                                "A node must return a newly owned SKImage.");
                        }

                        liveImages[node] = renderedImage;
                    }

                    ReleaseConsumedInputs(incomingConnections, remainingConsumers, liveImages);
                }

                return resultImage;
            }
            catch
            {
                resultImage?.Dispose();
                resultImage = null;
                throw;
            }
            finally
            {
                DisposeImages(liveImages.Values);
                liveImages.Clear();
            }
        }

        private static void ReleaseConsumedInputs(IEnumerable<ConnectionViewModel> incomingConnections, Dictionary<BaseNodeViewModel, int> remainingConsumers,
            Dictionary<BaseNodeViewModel, SKImage> liveImages)
        {
            foreach (var connection in incomingConnections)
            {
                var sourceNode = connection.Source.Node;
                if (!remainingConsumers.TryGetValue(sourceNode, out int count))
                {
                    continue;
                }

                count--;
                remainingConsumers[sourceNode] = count;

                if (count == 0 && liveImages.Remove(sourceNode, out var image))
                {
                    image.Dispose();
                }
            }
        }

        private static void DisposeImages(IEnumerable<SKImage> images)
        {
            var unique = new HashSet<SKImage>(ReferenceComparer<SKImage>.Instance);

            foreach (var image in images)
            {
                if (unique.Add(image))
                {
                    image.Dispose();
                }
            }
        }

        [Conditional("DEBUG")]
        private static void LogGpuCacheUsage(GRContext context, int textureSize)
        {
            context.GetResourceCacheUsage(out int resources, out long bytes);
            Debug.WriteLine(
                $"[Skia GPU] shared-context, size={textureSize}, " +
                $"cacheResources={resources}, cacheBytes={bytes:N0}, " +
                $"limitBytes={context.GetResourceCacheLimit():N0}");
        }

        [RelayCommand]
        private void DeleteSelection()
        {
            if (_isDisposed)
            {
                return;
            }

            var toRemove = SelectedConnections.ToList();
            if (toRemove.Count == 0)
            {
                return;
            }

            foreach (var connection in toRemove)
            {
                RemoveConnectionInternal(connection);
            }

            SelectedConnections.Clear();
            InvalidateGraph(requestGpuPurge: true);
        }



        /* TODO: maybe some reflection ? */
        [RelayCommand]
        private void CreateNode(NodeType nodeType)
        {
            if (_isDisposed)
            {
                return;
            }

            // only one TextureSize node and only one MaterialOutputNode
            if (nodeType == NodeType.TextureSizeNode &&
                Nodes.OfType<TextureSizeNodeViewModel>().Any())
            {
                return;
            }
            if (nodeType == NodeType.MaterialOutputNode &&
                Nodes.OfType<MaterialOutputNodeViewModel>().Any())
            {
                return;
            }

            var mousePosition = Mouse.GetPosition(Application.Current.MainWindow);

            BaseNodeViewModel newNode = nodeType switch
            {
                NodeType.ColorNode => new ColorNodeViewModel(),
                NodeType.PerlinNoiseNode => new PerlinNoiseFractalNodeViewModel(),
                NodeType.OutputNode => new OutputNodeViewModel(),
                NodeType.BlendNode => new BlendNodeViewModel(),
                NodeType.TextureSizeNode => new TextureSizeNodeViewModel(),
                NodeType.PerlinTurbulenceNode => new PerlinNoiseTurbulenceNodeViewModel(),
                NodeType.RerouteNode => new RerouteNodeViewModel(),
                NodeType.ThresholdNode => new ThresholdNodeViewModel(shaderService),
                NodeType.InvertNode => new InvertNodeViewModel(shaderService),
                NodeType.WorleyNoiseNode => new WorleyNoiseNodeViewModel(shaderService),
                NodeType.ValueNode => new ValueNodeViewModel(),
                NodeType.HeightToNormalNode => new HeightToNormalNodeViewModel(shaderService),
                NodeType.MaterialOutputNode => new MaterialOutputNodeViewModel(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(nodeType), nodeType, "Unknown node type")
            };

            newNode.Location = new Point(mousePosition.X - 100, mousePosition.Y - 50);
            AddNodeInternal(newNode);
            InvalidateGraph(requestGpuPurge: false);
        }

        [RelayCommand]
        private void DeleteNode(BaseNodeViewModel node)
        {
            if (_isDisposed || node is null || !Nodes.Contains(node))
            {
                return;
            }

            if (node is TextureSizeNodeViewModel)
            {
                return;
            }

            if (_pendingSourceConnector?.Node == node)
            {
                _pendingSourceConnector = null;
            }

            var connectedEdges = Connections
                .Where(c => c.Source.Node == node || c.Target.Node == node)
                .ToList();

            foreach (var connection in connectedEdges)
            {
                RemoveConnectionInternal(connection);
            }

            if (node is OutputNodeViewModel outputNode)
            {
                outputNode.ClearPreview();
            }

            DetachNode(node);
            Nodes.Remove(node);
            InvalidateGraph(requestGpuPurge: true);
        }

        private List<BaseNodeViewModel> GetTopologicallySortedNodes(
            BaseNodeViewModel targetNode, ConnectorViewModel targetInput)
        {
            var sorted = new List<BaseNodeViewModel>();
            var states = new Dictionary<BaseNodeViewModel, VisitState>(
                ReferenceComparer<BaseNodeViewModel>.Instance);

            var terminalConnection = Connections.FirstOrDefault(c => c.Target == targetInput);

            if (terminalConnection is not null &&
                Nodes.Contains(terminalConnection.Source.Node))
            {
                VisitNode(terminalConnection.Source.Node, states, sorted);
            }

            /*
             * Końcowy node dodajemy jako ostatni.
             * Sam nie generuje tekstury — odbiera wynik.
             */
            sorted.Add(targetNode);

            return sorted;
        }
        private void VisitNode(BaseNodeViewModel node, Dictionary<BaseNodeViewModel, VisitState> states, List<BaseNodeViewModel> sorted)
        {
            if (states.TryGetValue(node, out var state))
            {
                if (state == VisitState.Visiting)
                {
                    throw new InvalidOperationException(
                        "A cycle was detected in the node graph.");
                }

                if (state == VisitState.Visited)
                {
                    return;
                }
            }

            states[node] = VisitState.Visiting;

            var parents = Connections
                .Where(c => c.Target.Node == node)
                .Select(c => c.Source.Node)
                .Distinct(ReferenceComparer<BaseNodeViewModel>.Instance);

            foreach (var parent in parents)
            {
                if (Nodes.Contains(parent))
                {
                    VisitNode(parent, states, sorted);
                }
            }

            states[node] = VisitState.Visited;
            sorted.Add(node);
        }

        /// <summary>
        ///  Used to prevent cycles in graph
        /// </summary>
        private bool WouldCreateCycle(BaseNodeViewModel source, BaseNodeViewModel target)
        {
            var visited = new HashSet<BaseNodeViewModel>(
                ReferenceComparer<BaseNodeViewModel>.Instance);
            var stack = new Stack<BaseNodeViewModel>();
            stack.Push(target);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Add(current))
                {
                    continue;
                }

                if (current == source)
                {
                    return true;
                }

                foreach (var child in Connections
                             .Where(c => c.Source.Node == current)
                             .Select(c => c.Target.Node))
                {
                    stack.Push(child);
                }
            }

            return false;
        }

        public void ForceReleaseVRAM()
        {
            if (_isDisposed)
            {
                return;
            }

            _purgeOnNextRender = true;
            RequestGpuRender();
        }

        /// <summary>
        /// Mthod is called only from persistent object (SKGElement) with GPU access
        /// </summary>
        public void ClearGraphicsContext(GRContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!ReferenceEquals(_graphicsContext, context))
            {
                return;
            }

            _graphicsContext = null;
            _configuredGpuCacheLimitBytes = 0;
            _renderedGraphRevision = -1;
            _purgeOnNextRender = true;
        }

        [RelayCommand]
        private void CleanupGraphics()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _pendingSourceConnector = null;
            _requestGpuRender = null;
            _graphicsContext = null;

            foreach (var output in Nodes.OfType<OutputNodeViewModel>())
            {
                output.ClearPreview();
            }

            foreach (var node in Nodes.ToList())
            {
                DetachNode(node);
            }

            Connections.Clear();
            SelectedConnections.Clear();
            Nodes.Clear();
            _pendingExports.Clear();

            GC.SuppressFinalize(this);
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static ReferenceComparer<T> Instance { get; } = new();

            private ReferenceComparer()
            {
            }

            public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }

        [RelayCommand]
        private void ClearWorkspace()
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to clear the workspace? This process cannot be undone!",
                "Clear workspace", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (_isDisposed)
            {
                return;
            }
            _pendingSourceConnector = null;
            foreach (var node in Nodes.OfType<OutputNodeViewModel>())
            {
                node.ClearPreview();
            }
            foreach(var node in Nodes.ToList())
            {
                DetachNode(node);
            }
            Connections.Clear();
            SelectedConnections.Clear();
            Nodes.Clear();
            AddNodeInternal(CreateDefaultTextureSizeNode());
            InvalidateGraph(true);
        }

        private TextureSizeNodeViewModel CreateDefaultTextureSizeNode()
        {
            var node = new TextureSizeNodeViewModel { Location = new Point(0, 0) };
            if (node.AvailableSizes.Count > 0)
            {
                node.SelectedSize = node.AvailableSizes[0];
            }
            return node;
        }

        // Export texture
        private enum TextureExportFormat
        {
            Png,
            Jpeg,
            Tiff
        }
        private sealed record PendingExportRequest(OutputNodeViewModel OutputNode, string FilePath, int TextureSize, TextureExportFormat Format);

        [RelayCommand]
        private void SaveRender(OutputNodeViewModel node)
        {
            if (_isDisposed || node is null || !Nodes.Contains(node))
            {
                return;
            }
            var sizeNode = Nodes.OfType<TextureSizeNodeViewModel>().FirstOrDefault();
            if (sizeNode is null)
            {
                MessageBox.Show("Texture Size Node does not exist.", "Texture export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            bool hasInputConnection = Connections.Any(c => c.Target.Node == node && c.Target == node.InputConnector);
            if (!hasInputConnection)
            {
                MessageBox.Show("Output Node does not have any input.", "Texture export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_requestGpuRender is null)
            {
                MessageBox.Show("GPU renderer has not been created yet.", "Texture export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var textureSize = sizeNode.SelectedSize;
            var fileDialog = new SaveFileDialog
            {
                Title = "Save texture",
                FileName = $"texture_{textureSize}x{textureSize}",
                DefaultExt = "png",
                AddExtension = true,
                OverwritePrompt = true,
                CheckPathExists = true,
                Filter = "PNG image (*.png)|*.png|" +
                        "JPEG image (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                        "TIFF image (*.tif;*.tiff)|*.tif;*.tiff",
                FilterIndex = 1
            };
            bool? dialogResult = Application.Current?.MainWindow is Window owner ? fileDialog.ShowDialog(owner) : fileDialog.ShowDialog();
            if (dialogResult != true)
            {
                return;
            }
            TextureExportFormat format = fileDialog.FilterIndex switch
            {
                2 => TextureExportFormat.Jpeg,
                3 => TextureExportFormat.Tiff,
                _ => TextureExportFormat.Png
            };
            string filePath = NormalizeExportExtension(fileDialog.FileName, format);
            _pendingExports.Enqueue(new PendingExportRequest(node, filePath, textureSize, format));
            RequestGpuRender();
        }

        private void ProcessPendingExports(GRContext context)
        {
            while (_pendingExports.TryDequeue(out var exportRequest))
            {
                try
                {
                    ExportOutput(context, exportRequest);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"[Texture export] Export failed: {ex}");
                    Application.Current?.Dispatcher.BeginInvoke(
                            () =>
                            {
                                MessageBox.Show($"Something went wrong during texture export process.", "Texture export failure.",MessageBoxButton.OK, MessageBoxImage.Error );
                            }
                        );
                }
            }
        }
        private void ExportOutput(GRContext context, PendingExportRequest request)
        {
            if (_isDisposed || !Nodes.Contains(request.OutputNode) || context.IsAbandoned)
            {
                return;
            }
            using var outputImage = BuildOutputImage(request.OutputNode, context, request.TextureSize);
            if (outputImage is null)
            {
                throw new InvalidOperationException("Output Node did not generate any texture.");
            }
            using var cpuBitmap = CreateExportBitmap(context, outputImage, request.Format != TextureExportFormat.Jpeg);
            SaveBitmap(cpuBitmap, request.FilePath,  request.Format);
        }
        private static SKBitmap CreateExportBitmap(GRContext context, SKImage src, bool preserveTransparency)
        {
            var imageInfo = new SKImageInfo(src.Width, src.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var exportSurface = SKSurface.Create(
                    context, true, imageInfo
                );
            var canvas = exportSurface.Canvas;
            canvas.Clear(preserveTransparency ? SKColors.Transparent : SKColors.White);
            canvas.DrawImage(src, new SKPoint(0, 0), new SKSamplingOptions());
            context.Flush(submit: true, synchronous: true);
            var bitmap = new SKBitmap(imageInfo);
            bool readSucceed = exportSurface.ReadPixels(imageInfo, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
            if (!readSucceed)
            {
                bitmap.Dispose();
                throw new InvalidOperationException("Failed to copy image from GPU to RAM.");
            }
            return bitmap;
        }
        private static void SaveBitmap(SKBitmap bitmap, string filePath, TextureExportFormat format)
        {
            int buffSize = checked(bitmap.RowBytes * bitmap.Height);
            BitmapSource bitmapSrc = BitmapSource.Create(
                    bitmap.Width, bitmap.Height, 96.0, 96.0, PixelFormats.Pbgra32, null, bitmap.GetPixels(), buffSize, bitmap.RowBytes
                );
            bitmapSrc.Freeze();
            BitmapEncoder encoder;
            BitmapSource frameSource = bitmapSrc;
            switch (format)
            {
                case TextureExportFormat.Png:
                    encoder = new PngBitmapEncoder();
                    break;
                case TextureExportFormat.Jpeg:
                    {
                        var convertedBitmap = new FormatConvertedBitmap();
                        convertedBitmap.BeginInit();
                        convertedBitmap.Source = bitmapSrc;
                        convertedBitmap.DestinationFormat = PixelFormats.Bgr24;
                        convertedBitmap.EndInit();
                        convertedBitmap.Freeze();

                        frameSource = convertedBitmap;

                        encoder = new JpegBitmapEncoder
                        {
                            QualityLevel = 95
                        };

                        break;
                    }
                case TextureExportFormat.Tiff:
                    encoder = new TiffBitmapEncoder
                    {
                        Compression = TiffCompressOption.Zip
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Wrong format.");
            }
            encoder.Frames.Add(BitmapFrame.Create(frameSource));
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(fileStream);
        }
        private static string NormalizeExportExtension(string filePath, TextureExportFormat format)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool extensionMatchesFormat = format switch
            {
                TextureExportFormat.Png =>
                    ext == ".png",

                TextureExportFormat.Jpeg =>
                    ext is ".jpg" or ".jpeg",

                TextureExportFormat.Tiff =>
                    ext is ".tif" or ".tiff",

                _ => false
            };
            if (extensionMatchesFormat)
            {
                return filePath;
            }
            string defaultExtension = format switch
            {
                TextureExportFormat.Png => ".png",
                TextureExportFormat.Jpeg => ".jpg",
                TextureExportFormat.Tiff => ".tiff",
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };

            return Path.ChangeExtension(filePath, defaultExtension);
        }


        // For material
        private void RenderMaterialPreview(GRContext context)
        {
            var materialOutput = Nodes.OfType<MaterialOutputNodeViewModel>().FirstOrDefault();
            if (materialOutput is null)
            {
                PublishMaterialPreview(MaterialPreviewFrame.Empty);
                return;
            }

            using SKImage? baseColorImage = BuildImageForInput(materialOutput, materialOutput.BaseColorConnector, context, _materialPreviewTextureSize);

            using SKImage? normalImage = BuildImageForInput(materialOutput, materialOutput.NormalConnector, context, _materialPreviewTextureSize);

            byte[]? baseColorPng = baseColorImage is null ? null :
                EncodeMaterialPreviewTexture(context, baseColorImage, _materialPreviewTextureSize);
            byte[]? normalPng = normalImage is null ? null :
                EncodeMaterialPreviewTexture(context, normalImage, _materialPreviewTextureSize);

            using SKImage? roughnessImage = BuildImageForInput(materialOutput, materialOutput.RoughnessConnector, context, _materialPreviewTextureSize);

            using SKImage? metallicImage = BuildImageForInput(materialOutput, materialOutput.MetallicConnector, context, _materialPreviewTextureSize);
            SKImage? roughnessMetallicImage = null;
            try
            {
                if (roughnessImage is not null ||
                    metallicImage is not null)
                {
                    roughnessMetallicImage =BuildRoughnessMetallicImage(context, _materialPreviewTextureSize, roughnessImage, metallicImage);
                }

                byte[]? roughnessMetallicPng =
                    roughnessMetallicImage is null
                        ? null
                        : EncodeMaterialPreviewTexture(context, roughnessMetallicImage, _materialPreviewTextureSize);

                PublishMaterialPreview(
                    new MaterialPreviewFrame(
                        BaseColorPng: baseColorPng,
                        NormalPng: normalPng,
                        RoughnessMetallicPng:roughnessMetallicPng
                        ));
            }
            finally
            {
                roughnessMetallicImage?.Dispose();
            }
        }

        private static byte[] EncodeMaterialPreviewTexture(GRContext context, SKImage source, int size)
        {
            using var bitmap = CreatePreviewBitmap(context, source, size, size);
            using var cpuImage = SKImage.FromBitmap(bitmap);
            using var encodedData = cpuImage.Encode(SKEncodedImageFormat.Png, quality: 100) ??
                throw new InvalidOperationException("SkiaSharp failed to encode the material preview.");
            return encodedData.ToArray();
        }

        private void PublishMaterialPreview(MaterialPreviewFrame frame)
        {
            try
            {
                MaterialPreviewFrameReady?.Invoke(frame);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"[Material Preview] Update failed: {ex}");
            }
        }

        private SKImage BuildRoughnessMetallicImage(GRContext context, int size, SKImage? roughnessImage, SKImage? metallicImage)
        {
            var info = new SKImageInfo(size, size, SKColorType.RgbaF16, SKAlphaType.Opaque);
            using var surface = SKSurface.Create(context, true, info) ?? throw new InvalidOperationException("Could not create the Roughness/Metallic GPU surface");
            SKRuntimeEffect effect = shaderService.GetCompiledShader(Shaders.Shader.PackRoughnessMetallic);

            // without Roughness: default value = 0.5
            using SKShader roughnessShader = roughnessImage is null ? SKShader.CreateColor(new SKColor(128,128,128,255)) :
                roughnessImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);

            // without Metallic: default value = 0.0
            using SKShader metallicShader = metallicImage is null ? SKShader.CreateColor(new SKColor(0,0,0,255)) :
                metallicImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
            using var uniforms = new SKRuntimeEffectUniforms(effect);
            using var children = new SKRuntimeEffectChildren(effect)
            {
                ["roughnessImage"] = roughnessShader,
                ["metallicImage"] = metallicShader
            };
            using var packedShader = effect.ToShader(uniforms: uniforms, children: children) ?? throw new InvalidOperationException("Could not create the Roughness/Metallic shader");
            using var paint = new SKPaint
            {
                Shader = packedShader,
                BlendMode = SKBlendMode.Src
            };
            var destination = new SKRect(0, 0, size, size);
            surface.Canvas.Clear(SKColors.Transparent);
            surface.Canvas.DrawRect(destination, paint);
            return surface.Snapshot();
        }
    }
}
