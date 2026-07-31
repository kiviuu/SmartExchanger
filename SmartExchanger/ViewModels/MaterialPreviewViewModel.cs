using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Geometry;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using Microsoft.Extensions.Options;
using SmartExchanger.Models;
using SmartExchanger.Options;
using SmartExchanger.Rendering.MaterialPreview.Triplanar;
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
        private const bool ForceOitPreviewPass = true;

        private const double MinimumTriplanarScale = 0.1;
        private const double MaximumTriplanarScale = 16.0;
        private const double MinimumBlendSharpness = 1.0;
        private const double MaximumBlendSharpness = 16.0;
        private const double MinimumNormalStrength = 0.0;
        private const double MaximumNormalStrength = 4.0;
        public IReadOnlyList<MaterialMappingMode> AvailableMappingModes { get; } = Enum.GetValues<MaterialMappingMode>();
        public bool IsTriplanarMapping => SelectedMappingMode == MaterialMappingMode.Triplanar;

        [ObservableProperty]
        private MaterialMappingMode _selectedMappingMode;

        [ObservableProperty]
        private double _triplanarScale;

        [ObservableProperty]
        private double _triplanarBlendSharpness;

        [ObservableProperty]
        private double _triplanarNormalStrength;


        private bool _isDisposed;
        private readonly MaterialPreviewOptions _options;
        private readonly string _environmentMapsDirectory;

        // division around the sphere and between poles
        private const int SphereThetaDivisions = 128;
        private const int SpherePhiDivisions = 64;

        public Transform3D SphereTransform { get; } = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1.0, 0.0, 0.0), -90.0));
        public DefaultEffectsManager EffectsManager { get; }
        public PerspectiveCamera Camera { get; }
        public MeshGeometry3D SphereGeometry { get; }
        public PBRMaterial SphereMaterial { get; }

        [ObservableProperty]
        private TextureModel? _environmentTexture;

        [ObservableProperty]
        private bool _isSphereTransparent;

        //private string _ddsEnvironmentMapName = "Cubemap_Grandcanyon.dds";
        public ObservableCollection<EnvironmentMapItem> AvailableEnvironmentMaps { get; } = new();
        [ObservableProperty]
        private EnvironmentMapItem? _selectedEnvironmentMap;
        public MaterialPreviewViewModel(IOptions<MaterialPreviewOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            this._options = options.Value;
            this._environmentMapsDirectory = Path.IsPathRooted(_options.EnvironmentMapsDirectory) ? _options.EnvironmentMapsDirectory :
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _options.EnvironmentMapsDirectory));

            this.EffectsManager = new TriplanarEffectsManager();

            this.Camera = new PerspectiveCamera
            {
                Position = new Point3D(0, 0, 5),
                LookDirection = new Vector3D(0, 0, -5),
                UpDirection = new Vector3D(0, 1, 0),
                NearPlaneDistance = 0.1,
                FarPlaneDistance = 100
            };

            var sphereBuilder = new MeshBuilder();
            sphereBuilder.AddSphere(
                    center: Vector3.Zero,
                    radius: 1.0f,
                    thetaDiv: SphereThetaDivisions,
                    phiDiv: SpherePhiDivisions
                );
            SphereGeometry = sphereBuilder.ToMeshGeometry3D();


            _selectedMappingMode = _options.DefaultMappingMode;

            _triplanarScale = ClampTriplanarScale(_options.TriplanarScale);

            _triplanarBlendSharpness = ClampBlendSharpness(_options.TriplanarBlendSharpness);

            _triplanarNormalStrength = ClampNormalStrength(_options.TriplanarNormalStrength);

            SphereMaterial = new TriplanarPBRMaterial()
            {
                AlbedoColor = new HelixToolkit.Maths.Color4(1f, 1f, 1f, 1f),
                RoughnessFactor = _options.DefaultRoughness,
                MetallicFactor = _options.DefaultMetallic,
                AmbientOcclusionFactor = _options.AmbientOcclusion,
                ReflectanceFactor = 0.5,
                RenderAlbedoMap = false,
                RenderNormalMap = false,
                RenderRoughnessMetallicMap = false,
                RenderEnvironmentMap = true,

                EnableAutoTangent = false,
                RenderDisplacementMap = false,
                DisplacementMapScaleMask = CreateMappingSettingsVector()
            };

            DiscoverEnvironmentMaps();
            SelectedEnvironmentMap = FindDefaultEnvironmentMap() ?? AvailableEnvironmentMaps.FirstOrDefault();

            IsSphereTransparent = ForceOitPreviewPass;
        }

        public void ApplyPreview(MaterialPreviewFrame frame)
        {
            if (_isDisposed)
            {
                return;
            }
            // actualization on UI thread
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => ApplyPreview(frame)));
                return;
            }


            TextureModel? albedoMap = CreateTexture(frame.BaseColorPng);
            SphereMaterial.AlbedoMap = albedoMap;
            SphereMaterial.RenderAlbedoMap = albedoMap is not null;
            IsSphereTransparent = ForceOitPreviewPass || frame.IsTransparent;

            TextureModel? normalMap = CreateTexture(frame.NormalPng);
            SphereMaterial.NormalMap = normalMap;
            SphereMaterial.RenderNormalMap = normalMap is not null;

            TextureModel? roughnessMetallicMap = CreateTexture(frame.RoughnessMetallicPng);
            SphereMaterial.RoughnessMetallicMap = roughnessMetallicMap;
            SphereMaterial.RenderRoughnessMetallicMap = roughnessMetallicMap is not null;

            if (roughnessMetallicMap is null)
            {
                SphereMaterial.RoughnessFactor = _options.DefaultRoughness;
                SphereMaterial.MetallicFactor = _options.DefaultMetallic;
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
            if (!Directory.Exists(_environmentMapsDirectory))
            {
                Debug.WriteLine($"[Environment Maps] Directory doeas not exists: {_environmentMapsDirectory}");
                return;
            }

            IEnumerable<string> mapFiles = Directory.EnumerateFiles(_environmentMapsDirectory, "*.dds", SearchOption.TopDirectoryOnly)
                .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase);

            foreach (string mapFilePath in mapFiles)
            {
                string displayName = CreateEnvironmentMapDisplayName(mapFilePath);
                AvailableEnvironmentMaps.Add(new EnvironmentMapItem(displayName, mapFilePath));
            }
        }
        private EnvironmentMapItem? FindDefaultEnvironmentMap()
        {
            if (string.IsNullOrWhiteSpace(_options.DefaultEnvironmentMap))
            {
                return null;
            }
            return AvailableEnvironmentMaps.FirstOrDefault(item => string.Equals(Path.GetFileName(item.FilePath),
                _options.DefaultEnvironmentMap,
                StringComparison.OrdinalIgnoreCase));
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
            catch (Exception ex)
            {
                EnvironmentTexture = null;
                SphereMaterial.RenderEnvironmentMap = false;
                Debug.WriteLine($"[Environment Maps] Could not load '{value.DisplayName}'. {ex}");
            }
        }

        private void ApplyMappingSettings()
        {
            if (_isDisposed)
            {
                return;
            }
            SphereMaterial.DisplacementMapScaleMask = CreateMappingSettingsVector();
        }

        partial void OnSelectedMappingModeChanged(MaterialMappingMode value)
        {
            OnPropertyChanged(nameof(IsTriplanarMapping));
            ApplyMappingSettings();
        }

        partial void OnTriplanarScaleChanged(double value)
        {
            ApplyMappingSettings();
        }

        partial void OnTriplanarBlendSharpnessChanged(double value)
        {
            ApplyMappingSettings();
        }

        partial void OnTriplanarNormalStrengthChanged(double value)
        {
            ApplyMappingSettings();
        }

        [RelayCommand]
        private void ResetMappingSettings()
        {
            SelectedMappingMode = _options.DefaultMappingMode;
            TriplanarScale = ClampTriplanarScale(_options.TriplanarScale);
            TriplanarBlendSharpness = ClampBlendSharpness(_options.TriplanarBlendSharpness);
            TriplanarNormalStrength = ClampNormalStrength(_options.TriplanarNormalStrength);
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

            IsSphereTransparent = false;

            AvailableEnvironmentMaps.Clear();

            if (EffectsManager is IDisposable disposable)
            {
                disposable.Dispose();
            }
            GC.SuppressFinalize(this);
        }


        private static double ClampTriplanarScale(double value)
        {
            return Math.Clamp(value, MinimumTriplanarScale, MaximumTriplanarScale);
        }

        private static double ClampBlendSharpness(double value)
        {
            return Math.Clamp(value, MinimumBlendSharpness, MaximumBlendSharpness);
        }

        private static double ClampNormalStrength(double value)
        {
            return Math.Clamp(value, MinimumNormalStrength, MaximumNormalStrength);
        }

        private Vector4 CreateMappingSettingsVector()
        {
            float mappingMode = SelectedMappingMode == MaterialMappingMode.Triplanar ? 1.0f : 0.0f;

            return new Vector4(
                (float)ClampTriplanarScale(TriplanarScale),
                (float)ClampBlendSharpness(TriplanarBlendSharpness),
                (float)ClampNormalStrength(TriplanarNormalStrength),
                mappingMode);
        }
    }
}
