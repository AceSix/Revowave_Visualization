using UnityEngine;
using UnityEngine.UI; // Required for UI elements

using System;
using System.IO;
using System.Collections.Generic;


using GEDIGlobals;


public partial class WaveformVisualizer : MonoBehaviour
{
    private string dataFolder;
    private AppConfig LoadConfigFromChosenFolder()
    {
        this.dataFolder = LoadDataFolderFromPathJson();

        string configPath = Path.Combine(dataFolder, "config.json");
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("config.json not found in selected folder: " + configPath);
        }

        string json = File.ReadAllText(configPath);
        AppConfig cfg = JsonUtility.FromJson<AppConfig>(json);

        if (cfg == null)
        {
            throw new Exception("Failed to parse config.json");
        }

        // Resolve all file paths relative to the chosen folder
        cfg.footprints_bin  = Path.Combine(dataFolder, cfg.footprints_bin);
        cfg.subclusters_bin = Path.Combine(dataFolder, cfg.subclusters_bin);
        cfg.clusters_bin    = Path.Combine(dataFolder, cfg.clusters_bin);
        cfg.terrain_texture = Path.Combine(dataFolder, cfg.terrain_texture);
        cfg.dem_file        = Path.Combine(dataFolder, cfg.dem_file);

        ValidateRequiredFiles(cfg);

        return cfg;
    }

    private string LoadDataFolderFromPathJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "path.json");

        if (!File.Exists(path))
            throw new FileNotFoundException("Missing path.json: " + path);

        string json = File.ReadAllText(path);
        PathConfig cfg = JsonUtility.FromJson<PathConfig>(json);

        if (cfg == null || string.IsNullOrEmpty(cfg.dataFolder))
            throw new Exception("Invalid path.json");

        if (!Directory.Exists(cfg.dataFolder))
            throw new Exception("Data folder does not exist: " + cfg.dataFolder);

        return cfg.dataFolder;
    }
    private void ValidateRequiredFiles(AppConfig cfg)
    {
        CheckFile(cfg.footprints_bin, "footprints_bin");
        CheckFile(cfg.subclusters_bin, "subclusters_bin");
        CheckFile(cfg.clusters_bin, "clusters_bin");
        CheckFile(cfg.terrain_texture, "terrain_texture");
        CheckFile(cfg.dem_file, "dem_file");
    }

    private void CheckFile(string path, string fieldName)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Missing file for {fieldName}: {path}");
        }
    }
}