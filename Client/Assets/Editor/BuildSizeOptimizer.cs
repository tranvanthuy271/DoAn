using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif
using UnityEditor.Build.Reporting;

public static class BuildSizeOptimizer
{
    private const string MenuRoot = "Tools/DoAn/Build Size/";
    private const string ResourcesRoot = "Assets/Resources";
    private const string AppIconRoot = "Assets/AppIcon";
    private const string AppStoreIconPath = AppIconRoot + "/AppStoreIcon1024.png";

    [MenuItem(MenuRoot + "Report Resources Texture Size")]
    public static void ReportResourcesTextureSize()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ResourcesRoot });

        long sourceBytes = 0;
        int textureCount = 0;
        int iconCount = 0;
        int loadingCount = 0;

        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!File.Exists(path))
            {
                continue;
            }

            textureCount++;
            sourceBytes += new FileInfo(path).Length;

            string normalized = NormalizePath(path);
            if (ContainsIgnoreCase(normalized, "/itemicons/") ||
                ContainsIgnoreCase(normalized, "/skillicons/") ||
                ContainsIgnoreCase(normalized, "/icons/"))
            {
                iconCount++;
            }

            if (ContainsIgnoreCase(normalized, "/loading/"))
            {
                loadingCount++;
            }
        }

        { /* Resources textures: {textureCount:n0} files */ }
    }

    [MenuItem(MenuRoot + "Apply Conservative Resources Texture Settings")]
    public static void ApplyConservativeResourcesTextureSettings()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Optimize Resources textures",
            "This will reimport textures under Assets/Resources with smaller per-platform formats. " +
            "Recommended before release builds. Review visual quality after it finishes.",
            "Apply",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ResourcesRoot });
        int changed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                TextureProfile profile = GetProfile(path);

                bool dirty = false;

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (importer.isReadable)
                {
                    importer.isReadable = false;
                    dirty = true;
                }

                if (importer.maxTextureSize != profile.MaxSize)
                {
                    importer.maxTextureSize = profile.MaxSize;
                    dirty = true;
                }

                if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    dirty = true;
                }

                if (importer.compressionQuality != profile.Quality)
                {
                    importer.compressionQuality = profile.Quality;
                    dirty = true;
                }

                dirty |= ApplyPlatform(importer, "iPhone", profile.MaxSize, TextureImporterFormat.ASTC_6x6, profile.Quality);
                dirty |= ApplyPlatform(importer, "Android", profile.MaxSize, TextureImporterFormat.ETC2_RGBA8, profile.Quality);
                dirty |= ApplyPlatform(importer, "Standalone", profile.MaxSize, TextureImporterFormat.DXT5, profile.Quality);

                if (!dirty)
                {
                    continue;
                }

                changed++;
                importer.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        { /* Applied texture build-size settings to {changed:n0} Resources texture(s) */ }
    }

    [MenuItem(MenuRoot + "Apply Release Player Size Settings")]
    public static void ApplyReleasePlayerSizeSettings()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Apply release player settings",
            "This enables engine/code stripping and sets Android to ARM64 only. " +
            "Use this for release builds; test networking/reflection-heavy flows after changing stripping.",
            "Apply",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        ApplyReleasePlayerSizeSettingsCore();

        AssetDatabase.SaveAssets();
        { /* Applied release player size settings */ }
    }

    [MenuItem(MenuRoot + "Fix iOS App Store Icon")]
    public static void FixIosAppStoreIcon()
    {
        Texture2D icon = EnsureAppStoreIconAsset();
        ApplyIosAppStoreIcon(icon);
        AssetDatabase.SaveAssets();
        { /* Applied iOS App Store icon */ }
    }

    internal static void EnsureAndroidBuildSettings()
    {
        if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        }

        if (PlayerSettings.Android.targetArchitectures == (AndroidArchitecture)0)
        {
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            { /* Android target architecture was empty; set to ARM64 */ }
        }
    }

    internal static void EnsureIosBuildSettings()
    {
        Texture2D icon = EnsureAppStoreIconAsset();
        ApplyIosAppStoreIcon(icon);
    }

    private static Texture2D EnsureAppStoreIconAsset()
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(AppStoreIconPath);
        if (existing != null && existing.width == 1024 && existing.height == 1024)
        {
            EnsureReadableTextureImport(AppStoreIconPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(AppStoreIconPath);
        }

        if (!AssetDatabase.IsValidFolder(AppIconRoot))
        {
            AssetDatabase.CreateFolder("Assets", "AppIcon");
        }

        Texture2D generated = GenerateAppStoreIconTexture();
        File.WriteAllBytes(AppStoreIconPath, generated.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(generated);
        AssetDatabase.ImportAsset(AppStoreIconPath, ImportAssetOptions.ForceSynchronousImport);
        EnsureReadableTextureImport(AppStoreIconPath);

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppStoreIconPath);
        if (icon == null)
        {
            throw new InvalidOperationException("Unable to create iOS App Store icon asset.");
        }

        return icon;
    }

    private static void ApplyIosAppStoreIcon(Texture2D icon)
    {
        PlayerSettings.SetIcons(NamedBuildTarget.iOS, new[] { icon }, IconKind.Store);
    }

    private static void EnsureReadableTextureImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool dirty = false;

        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            dirty = true;
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            dirty = true;
        }

        if (importer.alphaSource != TextureImporterAlphaSource.None)
        {
            importer.alphaSource = TextureImporterAlphaSource.None;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (importer.maxTextureSize < 1024)
        {
            importer.maxTextureSize = 1024;
            dirty = true;
        }

        if (!dirty)
        {
            return;
        }

        importer.SaveAndReimport();
    }

    private static Texture2D GenerateAppStoreIconTexture()
    {
        const int size = 1024;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        Color32 top = new Color32(34, 54, 84, 255);
        Color32 bottom = new Color32(21, 108, 96, 255);
        Color32 accent = new Color32(239, 190, 78, 255);
        Color32 light = new Color32(247, 250, 244, 255);

        for (int y = 0; y < size; y++)
        {
            float t = y / (float)(size - 1);
            Color32 row = Color32.Lerp(bottom, top, t);

            for (int x = 0; x < size; x++)
            {
                float cx = (x - size * 0.5f) / (size * 0.5f);
                float cy = (y - size * 0.5f) / (size * 0.5f);
                float vignette = Mathf.Clamp01(1f - (cx * cx + cy * cy) * 0.35f);
                pixels[y * size + x] = Color32.Lerp(new Color32(12, 28, 44, 255), row, vignette);
            }
        }

        texture.SetPixels32(pixels);

        DrawCircle(texture, size / 2, size / 2, 315, new Color32(17, 30, 48, 255));
        DrawCircle(texture, size / 2, size / 2, 265, new Color32(47, 140, 116, 255));
        DrawCircle(texture, size / 2, size / 2, 210, new Color32(28, 75, 91, 255));

        DrawDiamond(texture, size / 2, 330, 230, accent);
        DrawDiamond(texture, size / 2, 505, 165, light);
        DrawDiamond(texture, size / 2, 648, 115, new Color32(97, 195, 159, 255));

        texture.Apply(false, false);
        return texture;
    }

    private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
    {
        int minX = Mathf.Max(0, centerX - radius);
        int maxX = Mathf.Min(texture.width - 1, centerX + radius);
        int minY = Mathf.Max(0, centerY - radius);
        int maxY = Mathf.Min(texture.height - 1, centerY + radius);
        int radiusSquared = radius * radius;

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - centerY;

            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - centerX;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void DrawDiamond(Texture2D texture, int centerX, int centerY, int radius, Color32 color)
    {
        int minX = Mathf.Max(0, centerX - radius);
        int maxX = Mathf.Min(texture.width - 1, centerX + radius);
        int minY = Mathf.Max(0, centerY - radius);
        int maxY = Mathf.Min(texture.height - 1, centerY + radius);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = Mathf.Abs(y - centerY);

            for (int x = minX; x <= maxX; x++)
            {
                int dx = Mathf.Abs(x - centerX);
                if (dx + dy <= radius)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void ApplyReleasePlayerSizeSettingsCore()
    {
        PlayerSettings.stripEngineCode = true;

#if UNITY_2021_2_OR_NEWER
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Medium);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Medium);
#else
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Medium);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Medium);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.Medium);
#endif

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    }

    private static bool ApplyPlatform(
        TextureImporter importer,
        string platformName,
        int maxSize,
        TextureImporterFormat format,
        int quality)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        bool dirty =
            settings.overridden != true ||
            settings.maxTextureSize != maxSize ||
            settings.format != format ||
            settings.compressionQuality != quality ||
            settings.crunchedCompression;

        if (!dirty)
        {
            return false;
        }

        settings.overridden = true;
        settings.maxTextureSize = maxSize;
        settings.format = format;
        settings.textureCompression = TextureImporterCompression.CompressedHQ;
        settings.compressionQuality = quality;
        settings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(settings);
        return true;
    }

    private static TextureProfile GetProfile(string path)
    {
        string normalized = NormalizePath(path);

        if (ContainsIgnoreCase(normalized, "/itemicons/") ||
            ContainsIgnoreCase(normalized, "/skillicons/") ||
            ContainsIgnoreCase(normalized, "/icons/"))
        {
            return new TextureProfile(512, 60);
        }

        if (ContainsIgnoreCase(normalized, "/loading/"))
        {
            return new TextureProfile(1024, 60);
        }

        return new TextureProfile(1024, 70);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static double ToMb(long bytes)
    {
        return bytes / (1024.0 * 1024.0);
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private readonly struct TextureProfile
    {
        public TextureProfile(int maxSize, int quality)
        {
            MaxSize = maxSize;
            Quality = quality;
        }

        public int MaxSize { get; }
        public int Quality { get; }
    }
}

public sealed class AndroidBuildSettingsPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
        {
            return;
        }

        BuildSizeOptimizer.EnsureAndroidBuildSettings();
    }
}

public sealed class IosBuildSettingsPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
        {
            return;
        }

        BuildSizeOptimizer.EnsureIosBuildSettings();
    }
}

public static class IosBuildPostprocessor
{
    [PostProcessBuild(0)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        BuildSizeOptimizer.EnsureIosBuildSettings();
    }
}
