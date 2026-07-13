using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace SmartExchanger.Helpers
{
    /// <summary>
    /// One persistent SKGElement - responsible for entire GPU rendering
    /// NOTE: Output Nodes do not create their own OpenGL elements
    /// </summary>
    public static class SkiaExtensions
    {
        public static readonly DependencyProperty EditorProperty =
            DependencyProperty.RegisterAttached(
                "Editor",
                typeof(EditorViewModel),
                typeof(SkiaExtensions),
                new PropertyMetadata(null, OnEditorChanged));

        private static readonly DependencyProperty IsHookedProperty =
            DependencyProperty.RegisterAttached(
                "IsHooked",
                typeof(bool),
                typeof(SkiaExtensions),
                new PropertyMetadata(false));

        private static readonly DependencyProperty LastContextProperty =
            DependencyProperty.RegisterAttached(
                "LastContext",
                typeof(GRContext),
                typeof(SkiaExtensions),
                new PropertyMetadata(null));

        public static void SetEditor(UIElement element, EditorViewModel? value) =>
            element.SetValue(EditorProperty, value);

        public static EditorViewModel? GetEditor(UIElement element) =>
            element.GetValue(EditorProperty) as EditorViewModel;

        private static void OnEditorChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is not SKGLElement glElement)
            {
                return;
            }

            EnsureEventHandlers(glElement);

            if (args.OldValue is EditorViewModel oldEditor)
            {
                oldEditor.SetGpuRenderRequest(null);
            }

            if (args.NewValue is EditorViewModel newEditor)
            {
                AssignWeakRenderCallback(glElement, newEditor);
                glElement.InvalidateVisual();
            }
        }

        private static void EnsureEventHandlers(SKGLElement glElement)
        {
            if ((bool)glElement.GetValue(IsHookedProperty))
            {
                return;
            }

            glElement.SetValue(IsHookedProperty, true);
            glElement.PaintSurface += GlElement_PaintSurface;
            glElement.Loaded += GlElement_Loaded;
            glElement.Unloaded += GlElement_Unloaded;
        }

        private static void AssignWeakRenderCallback(SKGLElement glElement,EditorViewModel editor)
        {
            var weakElement = new WeakReference<SKGLElement>(glElement);

            editor.SetGpuRenderRequest(() =>
            {
                if (weakElement.TryGetTarget(out var element) && element.IsLoaded)
                {
                    element.InvalidateVisual();
                }
            });
        }

        private static void GlElement_Loaded(object sender, RoutedEventArgs args)
        {
            if (sender is not SKGLElement glElement)
            {
                return;
            }

            var editor = GetEditor(glElement);
            if (editor is not null)
            {
                AssignWeakRenderCallback(glElement, editor);
            }

            glElement.InvalidateVisual();
        }

        private static void GlElement_PaintSurface(object? sender, SKPaintGLSurfaceEventArgs args)
        {
            if (sender is not SKGLElement glElement)
            {
                return;
            }

            var editor = GetEditor(glElement);
            var context = glElement.GRContext;

            args.Surface.Canvas.Clear(SKColors.Transparent);

            if (editor is null || context is null)
            {
                return;
            }

            glElement.SetValue(LastContextProperty, context);
            editor.SetGraphicsContext(context);
            editor.RenderPendingOutputs(context, args.Surface.Canvas);
        }

        private static void GlElement_Unloaded(object sender, RoutedEventArgs args)
        {
            if (sender is not SKGLElement glElement)
            {
                return;
            }

            var editor = GetEditor(glElement);
            editor?.SetGpuRenderRequest(null);

            var context = glElement.GetValue(LastContextProperty) as GRContext;
            glElement.ClearValue(LastContextProperty);

            if (editor is not null && context is not null)
            {
                editor.ClearGraphicsContext(context);
            }

            // This object will be destoryed with app closing event
        }
    }
}
