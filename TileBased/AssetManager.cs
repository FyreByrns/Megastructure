namespace TileBased;

static class AssetManager {
    public static string BasePath => AppDomain.CurrentDomain.BaseDirectory;
    public static string AssetPath => $"{BasePath}/assets";

    public static void EnsureDirectoryExists(string path, string directory) {
        if(!Directory.Exists($"{path}/{directory}")) {
            Directory.CreateDirectory(path);
        }
    }

    public static bool FileExists(string path) {
        return File.Exists(path);
    }
    public static string GetAssetPath(string path, string name) {
        return $"{AssetPath}/{path}/{name}";
    }

    public static string GetTilePath(Tile tile) {
        return GetAssetPath("tiles", $"{tile}.png");
    }
} 