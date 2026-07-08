using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartExchanger.ViewModels.Nodes;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SmartExchanger.ViewModels
{
    public partial class EditorViewModel : ObservableObject
    {
        public ObservableCollection<BaseNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
        public ObservableCollection<ConnectionViewModel> SelectedConnections { get; } = new();

        private ConnectorViewModel? _pendingSourceConnector;

        public EditorViewModel()
        {
            SetupDefaultScene();
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
                foreach (var conn in toRemove) Connections.Remove(conn);
                RecalculateGraph();
            }
        }

        [RelayCommand]
        private void RemoveConnection(object? parameter)
        {
            if (parameter is ConnectionViewModel connection)
            {
                Connections.Remove(connection);
                RecalculateGraph();
            }
        }


        private void RecalculateGraph()
        {
            //var outputNodes = Nodes.OfType<OutputNodeViewModel>();
            //foreach (var outNode in outputNodes)
            //{
            //    outNode.ResultTexture = null;
            //}
            foreach (var node in Nodes)
            {
                foreach (var input in node.Inputs)
                    input.IsConnected = Connections.Any(c => c.Target == input);

                foreach (var output in node.Outputs)
                    output.IsConnected = Connections.Any(c => c.Source == output);
            }

            //// clearing
            //foreach (var node in Nodes.OfType<OutputNodeViewModel>()) { node.CurrentTexture = null; }
            //foreach (var node in Nodes.OfType<PerlinNoiseFractalNodeViewModel>()) { node.InputTexture = null; }

            foreach(var node in Nodes)
            {
                node.ClearNode();
            }


            var sortedConnections = Connections.OrderBy(c => c.Source.Node is ColorNodeViewModel ? 0 : 1).ToList();

            foreach (var connection in sortedConnections)
            {
                var sourceNode = connection.Source.Node;
                var targetNode = connection.Target.Node;

                if (sourceNode.CurrentTexture != null)
                {
                    if (targetNode is OutputNodeViewModel targetOutput)
                    {
                        targetOutput.CurrentTexture = sourceNode.CurrentTexture;
                        targetOutput.ProcessNode();
                    }
                    else if (targetNode is PerlinNoiseFractalNodeViewModel targetPerlin)
                    {
                        targetPerlin.InputTexture = sourceNode.CurrentTexture;
                        targetPerlin.ProcessNode();
                    }
                }
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
            var toRemove = Connections.Where(c => c.Source.Node == node || c.Target.Node == node).ToList();
            foreach(var conn in toRemove)
            {
                Connections.Remove(conn);
            }
            node.PropsChanged -= RecalculateGraph;
            Nodes.Remove(node);
            RecalculateGraph();
        }
    }
}