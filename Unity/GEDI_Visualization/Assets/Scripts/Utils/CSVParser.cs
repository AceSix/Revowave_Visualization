// SLANTED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.Networking;
using System;

public class CSVParser : MonoBehaviour
{
    public string filePath = "Assets/Data/gedi_dataframe.csv";
    public List<GEDIDataPoint> dataPoints;

    public class GEDIDataPoint
    {
        public float latitude;
        public float longitude;
        public float elevation;
        public float instrumentLat;
        public float instrumentLon;
        public float instrumentAlt;
        public float lowestLat;
        public float lowestLon;
        public float lowestElev;
        public float wgs84Elevation;
        public float rh2;
        public float rh50;
        public float rh98;
        public string rhWaveform;
        public float[] rawWaveformValues;
        public int[] rawWaveformLengths;
        public float[] rawWaveformPositions;


        public GEDIDataPoint(float latitude, float longitude, float elevation,
                            float instrumentLat, float instrumentLon, float instrumentAlt,
                            float lowestLat, float lowestLon, float lowestElev,
                            float wgs84Elevation, float rh2, float rh50, float rh98,
                            string rhWaveform, string rawWaveformValues, string rawWaveformLengths,
                            string rawWaveformPositions)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            this.elevation = elevation;
            this.instrumentLat = instrumentLat;
            this.instrumentLon = instrumentLon;
            this.instrumentAlt = instrumentAlt;
            this.lowestLat = lowestLat;
            this.lowestLon = lowestLon;
            this.lowestElev = lowestElev;
            this.wgs84Elevation = wgs84Elevation;
            this.rh2 = rh2;
            this.rh50 = rh50;
            this.rh98 = rh98;
            this.rhWaveform = rhWaveform;


            // adaptive downsampled waveform data
            string[] valueStrings = rawWaveformValues.Split(',');
            string[] lengthStrings = rawWaveformLengths.Split(',');
            string[] positionStrings = rawWaveformPositions.Split(',');

            this.rawWaveformValues = new float[valueStrings.Length];
            this.rawWaveformLengths = new int[lengthStrings.Length];
            this.rawWaveformPositions = new float[positionStrings.Length];

            for (int i = 0; i < valueStrings.Length; i++)
            {
                float.TryParse(valueStrings[i], out this.rawWaveformValues[i]);
            }

            for (int i = 0; i < lengthStrings.Length; i++)
            {
                int.TryParse(lengthStrings[i], out this.rawWaveformLengths[i]);
            }
            for (int i = 0; i < positionStrings.Length; i++) // Parse position values
            {
                float.TryParse(positionStrings[i], out this.rawWaveformPositions[i]);
            }
        }
    }
    public void loadCSV()
    {
        this.dataPoints = new List<GEDIDataPoint>();

        string dataPath = Path.Combine(Application.streamingAssetsPath, filePath);
        string[] dataLines = File.ReadAllLines(dataPath);

        Debug.Log(dataLines.Length);
        for (int i = 1; i < dataLines.Length; i++)
        {
            string line = dataLines[i];
            List<string> fields = ParseCSVLine(line);

            if (fields.Count < 16)
            {
                // Debug.LogError($"Incorrect number of fields in line {i} : {line}");
                continue;
            }

            try
            {
                // Parse the fields
                float latitude = float.Parse(fields[0]);
                float longitude = float.Parse(fields[1]);
                float elevation = float.Parse(fields[2]);
                float instrumentLat = float.Parse(fields[3]);
                float instrumentLon = float.Parse(fields[4]);
                float instrumentAlt = float.Parse(fields[5]);
                float lowestLat = float.Parse(fields[6]);
                float lowestLon = float.Parse(fields[7]);
                float lowestElev = float.Parse(fields[8]);
                float wgs84Elevation = float.Parse(fields[9]);
                float rh2 = float.Parse(fields[10]);
                float rh50 = float.Parse(fields[11]);
                float rh98 = float.Parse(fields[12]);
                string rhWaveform = fields[13];
                string rawWaveformValues = fields[14];
                string rawWaveformLengths = fields[15];
                string rawWaveformPositions = fields[16];

                // new data point and add it to the list
                GEDIDataPoint dataPoint = new GEDIDataPoint(
                    latitude, longitude, elevation,
                    instrumentLat, instrumentLon, instrumentAlt,
                    lowestLat, lowestLon, lowestElev,
                    wgs84Elevation, rh2, rh50, rh98,
                    rhWaveform, rawWaveformValues, rawWaveformLengths,
                    rawWaveformPositions
                );

                dataPoints.Add(dataPoint);
            }
            catch (System.Exception e)
            {
                Debug.Log($"Error parsing line : {e.Message}");
            }
        }


        // Debug.Log($"Loaded {dataPoints.Count} data points from CSV.");
    }

    public static List<string> ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        StringBuilder value = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(c);
            }
        }

        result.Add(value.ToString()); // add the last field
        return result;
    }

    public List<GEDIDataPoint> getDataPoints()
    {
        return this.dataPoints;
    }

    void Start()
    {
        loadCSV();
    }

    void Update()
    {

    }
}
