using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using System.Windows.Media.Media3D;
using System.Numerics;
using SmartExchanger.Models;
using System.IO;

namespace SmartExchanger.ViewModels
{
    public partial class MaterialPreviewViewModel : ObservableObject, IDisposable
    {
        private bool _isDisposed;
        public DefaultEffectsManager EffectsManager { get; }
        public PerspectiveCamera Camera { get; }
        public MeshGeometry3D SphereGeometry { get; }
        public PBRMaterial SphereMaterial { get; }

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

            SphereMaterial = new PBRMaterial
            {
                AlbedoColor = new HelixToolkit.Maths.Color4(1f, 1f, 1f, 1f),
                RoughnessFactor = 0.5,
                MetallicFactor = 0.0,
                AmbientOcclusionFactor = 1.0,
                RenderAlbedoMap = false,
                RenderNormalMap = false,
                RenderRoughnessMetallicMap = false,

                RenderEnvironmentMap = false,
                EnableAutoTangent = true
            };
        }

        public void ApplyPreview(MaterialPreviewFrame frame)
        {
            if (_isDisposed)
            {
                return;
            }
            // actualization on UI thread
            var dispatcher = Application.Current?.Dispatcher;
            if ( dispatcher is not null && !dispatcher.CheckAccess() )
            {
                dispatcher.BeginInvoke(new Action(() => ApplyPreview(frame)));
                return;
            }

            TextureModel? albedoMap = CreateTexture(frame.BaseColorPng);
            SphereMaterial.AlbedoMap = albedoMap;
            SphereMaterial.RenderAlbedoMap = albedoMap is not null;

            TextureModel? normalMap = CreateTexture(frame.NormalPng);
            SphereMaterial.NormalMap = normalMap;
            SphereMaterial.RenderNormalMap = normalMap is not null;

            TextureModel? roughnessMetallicMap = CreateTexture(frame.RoughnessMetallicPng);
            SphereMaterial.RoughnessMetallicMap = roughnessMetallicMap;
            SphereMaterial.RenderRoughnessMetallicMap = roughnessMetallicMap is not null;

            if (roughnessMetallicMap is null)
            {
                SphereMaterial.RoughnessFactor = 0.5;
                SphereMaterial.MetallicFactor = 0.0;
            }
            else
            {
                SphereMaterial.RoughnessFactor = 1.0;
                SphereMaterial.MetallicFactor = 1.0;
            }

        }

        private static TextureModel? CreateTexture(byte[]? pngData)
        {
            if (pngData is null)
            {
                return null;
            }
            var stream = new MemoryStream(pngData, writable: false);
            return new TextureModel(stream, autoCloseStream: true);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            SphereMaterial.AlbedoMap = null;
            SphereMaterial.NormalMap = null;
            SphereMaterial.RoughnessMetallicMap = null;

            SphereMaterial.RenderAlbedoMap = false;
            SphereMaterial.RenderNormalMap = false;
            SphereMaterial.RenderRoughnessMetallicMap = false;

            if (EffectsManager is IDisposable disposable)
            {
                disposable.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
