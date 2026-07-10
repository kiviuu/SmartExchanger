using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Windows;
using SmartExchanger.ViewModels;
using SmartExchanger.ViewModels.Nodes;
using System;

namespace SmartExchanger.Helpers
{
    public static class SkiaExtensions
    {
        public static readonly DependencyProperty EditorProperty = DependencyProperty.RegisterAttached(
            "Editor", typeof(EditorViewModel), typeof(SkiaExtensions), new PropertyMetadata(null));

        public static readonly DependencyProperty OutputNodeProperty = DependencyProperty.RegisterAttached(
            "OutputNode", typeof(OutputNodeViewModel), typeof(SkiaExtensions), new PropertyMetadata(null, OnNodeChanged));

        public static void SetEditor(UIElement element, EditorViewModel value) => element.SetValue(EditorProperty, value);
        public static EditorViewModel GetEditor(UIElement element) => (EditorViewModel)element.GetValue(EditorProperty);

        public static void SetOutputNode(UIElement element, OutputNodeViewModel value) => element.SetValue(OutputNodeProperty, value);
        public static OutputNodeViewModel GetOutputNode(UIElement element) => (OutputNodeViewModel)element.GetValue(OutputNodeProperty);

        private static void OnNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is SKGLElement glElement)
            {
                // old node disconnecions
                if (args.OldValue is OutputNodeViewModel oldNode)
                {
                    glElement.PaintSurface -= GlControl_PaintSurface;
                    glElement.Unloaded -= GlElement_Unloaded;
                    oldNode.RequestRender = null;
                }

                // new node connection
                if (args.NewValue is OutputNodeViewModel newNode)
                {
                    glElement.PaintSurface += GlControl_PaintSurface;
                    glElement.Unloaded += GlElement_Unloaded;
                    newNode.RequestRender = glElement.InvalidateVisual;
                    glElement.InvalidateVisual();
                }
            }
        }

        private static void GlControl_PaintSurface(object sender, SKPaintGLSurfaceEventArgs args)
        {
            if (sender is SKGLElement glElement)
            {
                var canvas = args.Surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                var editor = GetEditor(glElement);
                var outputNode = GetOutputNode(glElement);

                if (editor != null && outputNode != null)
                {
                    editor.RenderGraphToCanvas(outputNode, glElement.GRContext, canvas);
                }
            }
        }

        private static void GlElement_Unloaded(object sender, RoutedEventArgs args)
        {
            if (sender is SKGLElement glElement)
            {
                glElement.PaintSurface -= GlControl_PaintSurface;
                var node = GetOutputNode(glElement);
                if (node is not null)
                {
                    node.RequestRender = null;
                }
            }
        }
    }
}