using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.UI;


using GEDIGlobals;

public class BinaryParser : MonoBehaviour
{
    public static List<Footprint> Load(string path)
    {
        List<Footprint> dataPoints = new List<Footprint>();

        using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            // ---- read footprint count ----
            int N = br.ReadInt32();
            for (int i = 0; i < N; i++)
            {
                Footprint fp = new Footprint(br.ReadInt32());
                dataPoints.Add(fp);
            }

            // ---- waveform values ----
            for (int i = 0; i < N; i++)
            {
                Footprint fp = dataPoints[i];
                int count = fp.N;

                float[] waveforms = new float[count];
                for (int j = 0; j < count; j++) waveforms[j] = br.ReadSingle();
                fp.LoadValues(waveforms);
            }

            // ---- waveform positions ----
            for (int i = 0; i < N; i++)
            {
                Footprint fp = dataPoints[i];
                int count = fp.N;

                float[] positions = new float[count];
                for (int j = 0; j < count; j++) positions[j] = br.ReadSingle();
                fp.LoadPositions(positions);
            }

            // ---- ground geolocation ----
            for (int i = 0; i < N; i++) dataPoints[i].longitude = br.ReadSingle();
            for (int i = 0; i < N; i++) dataPoints[i].latitude = br.ReadSingle();
            for (int i = 0; i < N; i++) dataPoints[i].elevation = br.ReadSingle();
            
            for (int i = 0; i < N; i++) dataPoints[i].instrumentLon = br.ReadSingle();
            for (int i = 0; i < N; i++) dataPoints[i].instrumentLat = br.ReadSingle();
            for (int i = 0; i < N; i++) dataPoints[i].instrumentAlt = br.ReadSingle();
        }

        return dataPoints;
    }

}