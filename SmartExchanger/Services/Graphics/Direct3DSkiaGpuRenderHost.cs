using SkiaSharp;
using System.Diagnostics;
using System.Windows.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using static Vortice.Direct3D12.D3D12;
using static Vortice.DXGI.DXGI;

namespace SmartExchanger.Services.Graphics
{
    public sealed class Direct3DSkiaGpuRenderHost : ISkiaGpuRenderHost
    {
        //public GRContext context => throw new NotImplementedException();

        private readonly Dispatcher _dispatcher;
        private readonly IDXGIFactory4 _factory;
        private readonly IDXGIAdapter1 _adapter;
        private readonly ID3D12Device2 _device;
        private readonly ID3D12CommandQueue _commandQueue;

        private readonly GRVorticeD3DBackendContext _backendContext;
        private readonly GRContext _context;

        private Action<GRContext>? _renderCallback;

        private int _renderQueued;
        private int _disposeState;

        private bool _isDisposed => Volatile.Read(ref _disposeState) != 0;


        public Direct3DSkiaGpuRenderHost()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _factory = CreateDXGIFactory2<IDXGIFactory4>(debug: false);
            (_adapter, _device) = CreateHardwareDevice(_factory);
            _commandQueue = _device.CreateCommandQueue(CommandListType.Direct);
            _commandQueue.Name = "SmartExchanger Skia D3D12 Queue";
            _backendContext = new GRVorticeD3DBackendContext
            {
                Adapter = _adapter,
                Device = _device,
                Queue = _commandQueue
            };

            _context = GRContext.CreateDirect3D(_backendContext) ?? throw new InvalidOperationException("SkiaSharp could not create a Direct3D 12 GrContext");

            if (_context.Backend != GRBackend.Direct3D)
            {
                throw new InvalidOperationException($"Unexpected Skia GPU backend {_context.Backend}");
            }
            Debug.WriteLine( $"[Skia GPU] Backend: {_context.Backend}");

            Debug.WriteLine( $"[Skia GPU] Adapter: {_adapter.Description1.Description}");
        }

        public GRContext Context
        {
            get
            {
                ThrowIfDisposed();
                return _context;
            }
        }

        public void SetRenderCallback(Action<GRContext>? renderCallback)
        {
            ThrowIfDisposed();
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke(
                    () => SetRenderCallback(
                        renderCallback));

                return;
            }
            _renderCallback = renderCallback;
            if (renderCallback is not null)
            {
                RequestRender();
            }
        }

        public void RequestRender()
        {
            if(_isDisposed || _dispatcher.HasShutdownStarted)
            {
                return;
            }
            if (Interlocked.Exchange(ref _renderQueued, 1) != 0)
            {
                return;
            }

            _ = _dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(RenderQueued));
        }

        private void RenderQueued()
        {
            Interlocked.Exchange(ref _renderQueued, 0);
            if (_isDisposed)
            {
                return;
            }
            Action<GRContext>? callback = _renderCallback;
            if (callback is null)
            {
                return;
            }
            callback(_context);
        }

        private static (IDXGIAdapter1 Adapter, ID3D12Device2 Device) CreateHardwareDevice(IDXGIFactory4 factory)
        {
            for(uint adapterIdx = 0; factory.EnumAdapters1(adapterIdx, out IDXGIAdapter1? candidate).Success; adapterIdx++)
            {
                if (candidate is null)
                {
                    continue;
                }
                AdapterDescription1 description = candidate.Description1;

                // Do not use Microsoft Basic Render Driver!
                if ((description.Flags & AdapterFlags.Software) != AdapterFlags.None)
                {
                    candidate.Dispose();
                    continue;
                }

                if (D3D12CreateDevice(candidate, FeatureLevel.Level_11_0, out ID3D12Device2? device).Success && device is not null)
                {
                    return (candidate, device);
                }
                candidate.Dispose();
            }
            throw new PlatformNotSupportedException("No hardware adapter supporting Direct3D 12 was found.");
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            if (!_dispatcher.CheckAccess() && !_dispatcher.HasShutdownStarted)
            {
                _dispatcher.Invoke(Dispose);
                return;
            }

            if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            {
                return;
            }
            _renderCallback = null;
            try
            {
                if (!_context.IsAbandoned)
                {
                    _context.Flush(submit: true, synchronous: true);
                    _context.PurgeUnlockedResources(scratchResourcesOnly: false);
                }
            }
            catch(System.Exception ex)
            {
                Debug.WriteLine($"[Skia GPU] Final flush failed: {ex}");
            }

            _context.Dispose();
            _backendContext.Dispose();

            _commandQueue.Dispose();
            _device.Dispose();
            _adapter.Dispose();
            _factory.Dispose();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }
    }
}
