using CommunityToolkit.Mvvm.ComponentModel;
using Nodify;
using SmartExchanger.ViewModels.Nodes;
using System.Collections.ObjectModel;

namespace SmartExchanger.ViewModels
{
    public partial class EditorViewModel : ObservableObject
    {
        public ObservableCollection<BaseNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

        public EditorViewModel()
        {
            SetupMvpScene();
        }

        private void SetupMvpScene()
        {
            var colorNode = new ColorNodeViewModel { Location = new System.Windows.Point(100, 100) };
            var outputNode = new OutputNodeViewModel { Location = new System.Windows.Point(400, 100)};

            outputNode.InputNode = colorNode;
            outputNode.Process();

            var wire = new ConnectionViewModel
            {
                Source = colorNode,
                Target = outputNode
            };

            Nodes.Add(colorNode);
            Nodes.Add(outputNode);
            Connections.Add(wire);
        }
    }
}
