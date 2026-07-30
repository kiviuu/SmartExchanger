using HelixToolkit.SharpDX.Shaders;
using System.IO;

namespace SmartExchanger.Rendering.MaterialPreview.Triplanar;

internal static class TriplanarShaderDescription
{
    private const string RelativeShaderDirectory =
        @"Shaders\MaterialPreview\Helix\PS";

    public static readonly ShaderDescription Pbr =
        CreatePixelShaderDescription(
            "SmartExchanger.PS.TriplanarPBR",
            "psMeshPBRTriplanar.cso");

    public static readonly ShaderDescription PbrOit =
        CreatePixelShaderDescription(
            "SmartExchanger.PS.TriplanarPBROIT",
            "psMeshPBRTriplanarOIT.cso");

    public static readonly ShaderDescription PbrOitDepthPeeling =
        CreatePixelShaderDescription(
            "SmartExchanger.PS.TriplanarPBROITDP",
            "psMeshPBRTriplanarOITDP.cso");

    private static ShaderDescription CreatePixelShaderDescription(
        string name,
        string fileName)
    {
        byte[] byteCode = LoadCompiledShader(fileName);

        return new ShaderDescription(
            name,
            HelixToolkit.SharpDX.ShaderStage.Pixel,
            new ShaderReflector(),
            byteCode);
    }

    private static byte[] LoadCompiledShader(string fileName)
    {
        string fullPath = Path.Combine(
            AppContext.BaseDirectory,
            RelativeShaderDirectory,
            fileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Compiled triplanar shader was not found: '{fullPath}'.",
                fullPath);
        }

        return File.ReadAllBytes(fullPath);
    }
}
