using UnityEngine;
using UnityEngine.UI; // Required for UI elements

using System;
using System.IO;
using System.Collections.Generic;

using SFB;

using GEDIGlobals;


public partial class WaveformVisualizer : MonoBehaviour
{
    private string dataFolder;
    private AppConfig LoadConfigFromChosenFolder()
    {
        this.dataFolder = AskUserForDataFolder();

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

    private string AskUserForDataFolder()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel(
            "Select the data folder containing config.json",
            "",
            false
        );
    
        if (paths == null || paths.Length == 0)
            return null;
    
        return paths[0];
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