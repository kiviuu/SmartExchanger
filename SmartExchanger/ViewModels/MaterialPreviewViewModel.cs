using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using System.Windows.Media.Media3D;
using System.Numerics;

namespace SmartExchanger.ViewModels
{
    public partial class MaterialPreviewViewModel : ObservableObject, IDisposable
    {
        private bool _isDisposed;
        public DefaultEffectsManager EffectsManager { get; }
        public PerspectiveCamera Camera { get; }
        public MeshGeometry3D SphereGeometry { get; }
        public Material SphereMaterial { get; }

        public MaterialPreviewViewModel()
        {
            this.EffectsManager = new DefaultEffectsManager();
            this.Camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 0, 5),
                LookDirection = new Vector3D(0, 0, -5),
                UpDirection = new Vector3D(0, 1, 0),
                NearPlaneDistance = 0.1,
                FarPlaneDistance = 100
            };

            var sphereBuilder = new MeshBuilder();
            sphereBuilder.AddSphere(Vector3.Zero, 1.0f);
            SphereGeometry = sphereBuilder.ToMeshGeometry3D();

            SphereMaterial = PhongMaterials.White;
        }
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            if (EffectsManager is IDisposable disposable)
            {
                disposable.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
