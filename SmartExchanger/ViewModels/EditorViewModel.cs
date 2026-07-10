using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using SmartExchanger.ViewModels.Nodes;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Xml.Linq;

namespace SmartExchanger.ViewModels
{
    public partial class EditorViewModel : ObservableObject
    {
        public ObservableCollection<BaseNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
        public ObservableCollection<ConnectionViewModel> SelectedConnections { get; } = new();

        private ConnectorViewModel? _pendingSourceConnector;
        private GRContext? _grContext;
        public bool IsGraphicsContextSet { get; set; } = false;

        public EditorViewModel()
        {
            SetupDefaultScene();
        }
        public void SetGraphicsContext(GRContext context)
        {
            _grContext = context;
            RecalculateGraph();
        }

        private void SetupDefaultScene()
        {
            //var colorNode = new ColorNodeViewModel { Location = new Point(100, 150), R = 255, G = 128, B = 0 };
            //var outputNode = new OutputNodeViewModel { Location = new Point(450, 150) };
            //var perlinNoiseNode = new PerlinNoiseFractalNodeViewModel { Location = new Point(100, 300) };

            //colorNode.ColorChanged += RecalculateGraph;
            //perlinNoiseNode.PropsChanged += RecalculateGraph;

            //Nodes.Add(colorNode);
            //Nodes.Add(outputNode);
            //Nodes.Add(perlinNoiseNode);
            var textureSizeNode = new TextureSizeNodeViewModel { Location = new Point(0, 0) };
            textureSizeNode.PropsChanged += RecalculateGraph;
            Nodes.Add(textureSizeNode);
        }

        [RelayCommand]
        private void StartConnection(object? parameter)
        {
            if (parameter is ConnectorViewModel connector)
            {
                _pendingSourceConnector = connector;
            }
        }

        [RelayCommand]
        private void CompleteConnection(object? parameter)
        {
            if (parameter is ValueTuple<object, object> tuple && tuple.Item2 is ConnectorViewModel targetConnector)
            {
                if (_pendingSourceConnector == null || _pendingSourceConnector == targetConnector) return;
                if (_pendingSourceConnector.Node == targetConnector.Node) return; 

                if (_pendingSourceConnector.Node.Outputs.Contains(_pendingSourceConnector) &&
                    targetConnector.Node.Inputs.Contains(targetConnector))
                {
                    var oldConnection = Connections.FirstOrDefault(c => c.Target == targetConnector);
                    if (oldConnection != null) Connections.Remove(oldConnection);

                    Connections.Add(new ConnectionViewModel(_pendingSourceConnector, targetConnector));

                    RecalculateGraph();
                }
            }
            _pendingSourceConnector = null;
        }

        [RelayCommand]
        private void DisconnectConnector(object? parameter)
        {
            if (parameter is ConnectorViewModel connector)
            {
                var toRemove = Connections.Where(c => c.Source == connector || c.Target == connector).ToList();
                foreach (var conn in toRemove)
                {
                    Connections.Remove(conn);
                    SelectedConnections.Remove(conn);
                }
                RecalculateGraph();
            }
        }

        [RelayCommand]
        private void RemoveConnection(object? parameter)
        {
            if (parameter is ConnectionViewModel connection)
            {
                Connections.Remove(connection);
                SelectedConnections.Remove(connection);
                RecalculateGraph();
            }
        }


        //private void RecalculateGraph()
        //{
        //    ////var outputNodes = Nodes.OfType<OutputNodeViewModel>();
        //    ////foreach (var outNode in outputNodes)
        //    ////{
        //    ////    outNode.ResultTexture = null;
        //    ////}
        //    foreach (var node in Nodes)
        //    {
        //        foreach (var input in node.Inputs)
        //            input.IsConnected = Connections.Any(c => c.Target == input);

        //        foreach (var output in node.Outputs)
        //            output.IsConnected = Connections.Any(c => c.Source == output);
        //    }

        //    var sizeNode = Nodes.OfType<TextureSizeNodeViewModel>().FirstOrDefault();
        //    int currentSize = sizeNode?.SelectedSize ?? 512;

        //    //foreach(var node in Nodes)
        //    //{
        //    //    if (node.CurrentTexture is not null)
        //    //    {
        //    //        node.CurrentTexture.Dispose();
        //    //    }
        //    //    node.CurrentTexture = null;
        //    //}

        //    ////// clearing
        //    ////foreach (var node in Nodes.OfType<OutputNodeViewModel>()) { node.CurrentTexture = null; }
        //    ////foreach (var node in Nodes.OfType<PerlinNoiseFractalNodeViewModel>()) { node.InputTexture = null; }

        //    //foreach(var node in Nodes)
        //    //{
        //    //    node.ClearNode();
        //    //}


        //    //var sortedConnections = Connections.OrderBy(c => c.Source.Node is ColorNodeViewModel ? 0 : 1).ToList();
        //    var sortedNodes = GetTopologicallySortedNodes();
        //    //var nodeOrder = sortedNodes.Select((node, idx) => new { node, idx }).ToDictionary(x => x.node, x => x.idx);
        //    //var sortedConnections = Connections.OrderBy(c => nodeOrder.TryGetValue(c.Source.Node, out var idx) ? idx : 0).ToList();

        //    //foreach (var connection in sortedConnections)
        //    //{
        //    //    var sourceNode = connection.Source.Node;
        //    //    var targetNode = connection.Target.Node;

        //    //    if (sourceNode.CurrentTexture != null)
        //    //    {
        //    //        if (targetNode is OutputNodeViewModel targetOutput)
        //    //        {
        //    //            targetOutput.CurrentTexture = sourceNode.CurrentTexture;
        //    //            targetOutput.ProcessNode(currentSize);
        //    //        }
        //    //        else if (targetNode is PerlinNoiseFractalNodeViewModel targetPerlin)
        //    //        {
        //    //            targetPerlin.InputTexture = sourceNode.CurrentTexture;
        //    //            targetPerlin.ProcessNode(currentSize);
        //    //        }
        //    //        else if (targetNode is BlendNodeViewModel targetBlend)
        //    //        {
        //    //            if (connection.Target.Title == "A")
        //    //            {
        //    //                targetBlend.InputTextureA = sourceNode.CurrentTexture;
        //    //            }
        //    //            if (connection.Target.Title == "B")
        //    //            {
        //    //                targetBlend.InputTextureB = sourceNode.CurrentTexture;
        //    //            }
        //    //            targetBlend.ProcessNode(currentSize);
        //    //        }
        //    //    }
        //    //}
        //    foreach (var node in sortedNodes)
        //    {
        //        var incomingConnections = Connections.Where(c => c.Target.Node == node);
        //        foreach (var conn in incomingConnections)
        //        {
        //            var sourceTexture = conn.Source.Node.CurrentTexture;

        //            if (node is OutputNodeViewModel targetOutput)
        //            {
        //                targetOutput.InputTexture = sourceTexture;
        //            }
        //            else if (node is PerlinNoiseFractalNodeViewModel targetPerlin)
        //            {
        //                targetPerlin.InputTexture = sourceTexture;
        //            }
        //            else if (node is BlendNodeViewModel targetBlend)
        //            {
        //                if (conn.Target.Title == "A") targetBlend.InputTextureA = sourceTexture;
        //                if (conn.Target.Title == "B") targetBlend.InputTextureB = sourceTexture;
        //            }
        //        }
        //        node.ProcessNode(_grContext, currentSize);
        //    }
        //}
        private void RecalculateGraph()
        {
            foreach (var node in Nodes)
            {
                foreach (var input in node.Inputs)
                    input.IsConnected = Connections.Any(c => c.Target == input);

                foreach (var output in node.Outputs)
                    output.IsConnected = Connections.Any(c => c.Source == output);
            }
            foreach (var outNode in Nodes.OfType<OutputNodeViewModel>())
            {
                outNode.RequestRender?.Invoke();
            }
        }

        // called from output nodes context
        public void RenderGraphToCanvas(OutputNodeViewModel targetOutput, GRContext context, SKCanvas finalCanvas)
        {
            var sizeNode = Nodes.OfType<TextureSizeNodeViewModel>().FirstOrDefault();
            int currentSize = sizeNode?.SelectedSize ?? 512;

            var sortedNodes = GetTopologicallySortedNodes();


            foreach (var node in sortedNodes)
            {
                var incomingConnections = Connections.Where(c => c.Target.Node == node);
                foreach (var conn in incomingConnections)
                {
                    var sourceTexture = conn.Source.Node.CurrentTexture;
                    if (node is OutputNodeViewModel outNode) outNode.InputTexture = sourceTexture;
                    else if (node is PerlinNoiseFractalNodeViewModel targetPerlin) targetPerlin.InputTexture = sourceTexture;
                    else if (node is BlendNodeViewModel targetBlend)
                    {
                        if (conn.Target.Title == "A") targetBlend.InputTextureA = sourceTexture;
                        if (conn.Target.Title == "B") targetBlend.InputTextureB = sourceTexture;
                    }
                }

                if (node != targetOutput)
                {
                    node.ProcessNode(context, currentSize);
                }
            }
            if (targetOutput.InputTexture != null)
            {
                var rect = new SKRect(0, 0, 256, 256);
                finalCanvas.DrawImage(targetOutput.InputTexture, rect, new SKSamplingOptions());
            }
            foreach (var node in Nodes)
            {
                node.CurrentTexture?.Dispose();
                node.CurrentTexture = null;
            }
        }

        [RelayCommand]
        private void DeleteSelection()
        {
            var toRemove = SelectedConnections.ToList();
            foreach(var conn in toRemove) 
            {
                Connections.Remove(conn);
            }
            RecalculateGraph();
        }

        [RelayCommand]
        private void CreateNode(NodeType nodeType)
        {
            var mousePossition = Mouse.GetPosition(Application.Current.MainWindow);
            BaseNodeViewModel newNode = nodeType switch
            {
                NodeType.ColorNode => new ColorNodeViewModel(),
                NodeType.PerlinNoiseNode => new PerlinNoiseFractalNodeViewModel(),
                NodeType.OutputNode => new OutputNodeViewModel(),
                NodeType.BlendNode => new BlendNodeViewModel(),
                NodeType.TextureSizeNode => new TextureSizeNodeViewModel(),
                _ => throw new ArgumentException("Unknow node type")
            };

            newNode.Location = new Point(mousePossition.X - 100, mousePossition.Y - 50);
            newNode.PropsChanged += RecalculateGraph;
            Nodes.Add(newNode);
            RecalculateGraph();
        }

        [RelayCommand]
        private void DeleteNode(BaseNodeViewModel node)
        {
            if (node is null) return;
            if (node.GetType() == typeof(TextureSizeNodeViewModel)) return;
            var toRemove = Connections.Where(c => c.Source.Node == node || c.Target.Node == node).ToList();
            foreach(var conn in toRemove)
            {
                Connections.Remove(conn);
                SelectedConnections.Remove(conn);
            }

            node.CurrentTexture?.Dispose();
            node.CurrentTexture = null;
            if (node is OutputNodeViewModel outputNode)
            {
                outputNode.InputTexture = null;
                outputNode.RequestRender = null;
            }

            node.PropsChanged -= RecalculateGraph;
            Nodes.Remove(node);
            RecalculateGraph();
            ForceReleaseVRAM();
        }

        private List<BaseNodeViewModel> GetTopologicallySortedNodes()
        {
            var sorted = new List<BaseNodeViewModel>();
            var visited = new Dictionary<BaseNodeViewModel, bool>();
            foreach (var node in Nodes)
            {
                if (!visited.ContainsKey(node))
                {
                    VisitNode(node, visited, sorted);
                }
            }
            return sorted;
        }
        private void VisitNode(BaseNodeViewModel node, Dictionary<BaseNodeViewModel, bool> visited, List<BaseNodeViewModel> sorted)
        {
            if (visited.TryGetValue(node, out bool isFullyVisited))
            {
                if (!isFullyVisited)
                {
                    throw new InvalidOperationException("Loop in graph detected!");
                }
                return;
            }
            visited[node] = false;
            var parents = Connections.Where(c => c.Target.Node == node).Select(c => c.Source.Node);
            foreach (var parent in parents)
            {
                VisitNode(parent, visited, sorted);
            }
            visited[node] = true;
            sorted.Add(node);
        }

        [RelayCommand]
        private void CleanupGraphics()
        {
            foreach (var node in Nodes)
            {
                node.CurrentTexture?.Dispose();
                node.CurrentTexture = null;
                node.ClearNode();
            }
            if (_grContext is not null)
            {
                _grContext.Dispose();
                _grContext = null;
            }
        }

        public void ForceReleaseVRAM()
        {
            if (_grContext is not null)
            {
                _grContext.PurgeResources();
                _grContext.Flush();
            }
        }
    }
}