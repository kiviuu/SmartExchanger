using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.ViewModels.Nodes
{
    public enum NodeType
    {
        ColorNode,
        ValueNode,
        TextureSizeNode,


        PerlinNoiseNode,
        PerlinTurbulenceNode,
        BlendNode,
        RerouteNode,
        ThresholdNode,
        InvertNode,
        WorleyNoiseNode,
        HeightToNormalNode,


        MaterialOutputNode,
        OutputNode
    }
}
