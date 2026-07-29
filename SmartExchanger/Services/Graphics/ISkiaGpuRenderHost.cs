using SkiaSharp;

namespace SmartExchanger.Services.Graphics
{
    public interface ISkiaGpuRenderHost : IDisposable
    {
        GRContext Context { get; }
        void SetRenderCallback(Action<GRContext>? renderCallback);
        void RequestRender();
    }
}
