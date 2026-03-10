using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.UI;


using GEDIGlobals;

public class BinaryParser : MonoBehaviour
{
    public string filePath = "Assets/Data/Ardeche.bin";
    public List<Footprint> dataPoints;
    // void Start()
    // {
    //     string path = Application.streamingAssetsPath + "/gedi_data.bin";
    //     Load(path);
    // }

    public void Load()
    {
        string path = this.filePath;
        using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            // ---- read footprint count ----
            int N = br.ReadInt32();
            this.dataPoints = new List<Footprint>();

            for (int i = 0; i < N; i++)
            {
                Footprint fp = new Footprint(br.ReadInt32());
                this.dataPoints.Add(fp);
            }

            // ---- waveform values ----
            // data.downsampled = new float[N][];

            for (int i = 0; i < N; i++)
            {
                Footprint fp = this.dataPoints[i];
                int count = fp.N;

                float[] waveforms = new float[count];
                for (int j = 0; j < count; j++) waveforms[j] = br.ReadSingle();
                fp.LoadValues(waveforms);
            }

            // ---- waveform positions ----
            for (int i = 0; i < N; i++)
            {
                Footprint fp = this.dataPoints[i];
                int count = fp.N;

                float[] positions = new float[count];
                for (int j = 0; j < count; j++) positions[j] = br.ReadSingle();
                fp.LoadPositions(positions);
            }

            // ---- ground geolocation ----
            for (int i = 0; i < N; i++) this.dataPoints[i].longitude = br.ReadSingle();
            for (int i = 0; i < N; i++) this.dataPoints[i].latitude = br.ReadSingle();
            for (int i = 0; i < N; i++) this.dataPoints[i].elevation = br.ReadSingle();
            
            for (int i = 0; i < N; i++) this.dataPoints[i].instrumentLon = br.ReadSingle();
            for (int i = 0; i < N; i++) this.dataPoints[i].instrumentLat = br.ReadSingle();
            for (int i = 0; i < N; i++) this.dataPoints[i].instrumentAlt = br.ReadSingle();
        }
    }

    public List<Footprint> GetDataPoints()
    {
        return this.dataPoints;
    }
}