using CommunityToolkit.Mvvm.ComponentModel;
using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using SmartExchanger.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Windows.Media.Media3D;
using Material = HelixToolkit.Wpf.SharpDX.Material;
using MeshGeometry3D = HelixToolkit.SharpDX.MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace SmartExchanger.ViewModels
{
    public partial class MaterialPreviewViewModel : ObservableObject, IDisposable
    {
        private bool _isDisposed;
        public DefaultEffectsManager EffectsManager { get; }
        public PerspectiveCamera Camera { get; }
        public MeshGeometry3D SphereGeometry { get; }
        public PBRMaterial SphereMaterial { get; }

        [ObservableProperty]
        private TextureModel? _environmentTexture;

        //private string _ddsEnvironmentMapName = "Cubemap_Grandcanyon.dds";
        public ObservableCollection<EnvironmentMapItem> AvailableEnvironmentMaps { get; } = new();
        [ObservableProperty]
        private EnvironmentMapItem? _selectedEnvironmentMap;
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

                RenderEnvironmentMap = true,
                EnableAutoTangent = true
            };

            DiscoverEnvironmentMaps();
            SelectedEnvironmentMap = AvailableEnvironmentMaps.FirstOrDefault();
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


        private void DiscoverEnvironmentMaps()
        {
            AvailableEnvironmentMaps.Clear();
            string directoryPath = Path.Combine(AppContext.BaseDirectory, "Assets", "EnvironmentMaps");
            if (!Directory.Exists(directoryPath))
            {
                Debug.WriteLine($"[Environment Maps] Directory doeas not exists: {directoryPath}");
                return;
            }

            IEnumerable<string> mapFiles = Directory.EnumerateFiles(directoryPath, "*.dds", SearchOption.TopDirectoryOnly)
                .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase);

            foreach(string mapFilePath in mapFiles)
            {
                string displayName = CreateEnvironmentMapDisplayName(mapFilePath);
                AvailableEnvironmentMaps.Add(new EnvironmentMapItem(displayName, mapFilePath));
            }
        }
        private static string CreateEnvironmentMapDisplayName(string filePath)
        {
            string name = Path.GetFileNameWithoutExtension(filePath);
            name = name.Replace('_', ' ').Replace('-', ' ');
            const string cubemapPrefix = "Cubemap";
            if (name.StartsWith(cubemapPrefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name[cubemapPrefix.Length..];
            }
            return name.Trim();
        }

        partial void OnSelectedEnvironmentMapChanged(EnvironmentMapItem? value)
        {
            if (_isDisposed)
            {
                return;
            }
            if (value is null)
            {
                EnvironmentTexture = null;
                SphereMaterial.RenderEnvironmentMap = false;
                return;
            }
            try
            {
                if (!File.Exists(value.FilePath))
                {
                    throw new FileNotFoundException("Selected environment map does not exist.", value.FilePath);
                }
                TextureModel nextTexture = TextureModel.Create(value.FilePath) ?? throw new InvalidOperationException("HelixToolkit could not create the environment TextureModel.");
                EnvironmentTexture = nextTexture;
                SphereMaterial.RenderEnvironmentMap = true;
                
            }
            catch(Exception ex)
            {
                EnvironmentTexture = null;
                SphereMaterial.RenderEnvironmentMap = false;
                Debug.WriteLine($"[Environment Maps] Could not load '{value.DisplayName}'. {ex}");
            }
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

            SphereMaterial.RenderEnvironmentMap = false;
            EnvironmentTexture = null;

            AvailableEnvironmentMaps.Clear();

            if (EffectsManager is IDisposable disposable)
            {
                disposable.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}
