using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NoiseController : MonoBehaviour
{
    public NoiseGenerator noiseGenerator;
    public TMP_Dropdown noiseTypeDropdown;
    public TMP_Dropdown presetDropdown;
    public Button analyzeButton;
    public Button regenerateButton;
    public Button randomSeedButton;
    public TextMeshProUGUI analysisText;

    [System.Serializable]
    private struct TerrainMetrics
    {
        public float generationTime;
        public float averageHeight;
        public float heightVariance;
        public int peakCount;
    }

    void Start()
    {
        // Setup noise type dropdown
        if (noiseTypeDropdown != null)
        {
            noiseTypeDropdown.ClearOptions();
            List<string> options = new List<string>()
            {
                "Perlin Noise",
                "Value Noise",
                "Worley Noise",
                "Simplex Noise"
            };
            noiseTypeDropdown.AddOptions(options);
            noiseTypeDropdown.onValueChanged.AddListener(OnNoiseTypeChanged);
        }

        // Setup preset dropdown
        if (presetDropdown != null)
        {
            presetDropdown.ClearOptions();
            List<string> presetOptions = new List<string>()
            {
                "Mountains",
                "Plains",
                "Hills",
                "Coastal",
                "Combined"
            };
            presetDropdown.AddOptions(presetOptions);
            presetDropdown.onValueChanged.AddListener(OnPresetChanged);
        }

        if (analyzeButton != null)
        {
            analyzeButton.onClick.AddListener(AnalyzeTerrain);
        }

        if (regenerateButton != null)
        {
            regenerateButton.onClick.AddListener(() => noiseGenerator.GenerateTerrain());
        }

        if (randomSeedButton != null)
        {
            randomSeedButton.onClick.AddListener(() => noiseGenerator.SetRandomSeed());
        }
    }

    void OnNoiseTypeChanged(int index)
    {
        if (noiseGenerator != null)
        {
            noiseGenerator.SetNoiseType((NoiseGenerator.NoiseType)index);
            AnalyzeTerrain();
        }
    }

    void OnPresetChanged(int index)
    {
        if (noiseGenerator != null)
        {
            noiseGenerator.ApplyPreset((NoiseGenerator.TerrainPreset)index);
            AnalyzeTerrain();
        }
    }

    TerrainMetrics AnalyzeCurrentTerrain()
    {
        TerrainMetrics metrics = new TerrainMetrics();
        
        // Get current terrain data
        var terrainData = noiseGenerator.GetComponent<Terrain>().terrainData;
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        float sum = 0;
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;
        int peakCount = 0;

        // Calculate metrics
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float currentHeight = heights[x, y];
                sum += currentHeight;
                
                if (currentHeight > maxHeight) maxHeight = currentHeight;
                if (currentHeight < minHeight) minHeight = currentHeight;

                // Count peaks (local maxima)
                if (x > 0 && x < width - 1 && y > 0 && y < height - 1)
                {
                    bool isPeak = true;
                    for (int dy = -1; dy <= 1 && isPeak; dy++)
                    {
                        for (int dx = -1; dx <= 1 && isPeak; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (heights[x + dx, y + dy] >= currentHeight)
                            {
                                isPeak = false;
                            }
                        }
                    }
                    if (isPeak && currentHeight > 0.5f) // Only count significant peaks
                    {
                        peakCount++;
                    }
                }
            }
        }

        // Calculate final metrics
        metrics.averageHeight = sum / (width * height);
        metrics.generationTime = noiseGenerator.GetGenerationTime(noiseGenerator.GetNoiseType(), noiseGenerator.GetCurrentPreset());
        
        // Calculate variance
        float sumSquareDiff = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float diff = heights[x, y] - metrics.averageHeight;
                sumSquareDiff += diff * diff;
            }
        }
        metrics.heightVariance = sumSquareDiff / (width * height);
        metrics.peakCount = peakCount;

        return metrics;
    }

    void AnalyzeTerrain()
    {
        if (noiseGenerator == null || analysisText == null) return;

        TerrainMetrics metrics = AnalyzeCurrentTerrain();
        var currentNoiseType = noiseGenerator.GetNoiseType();
        var currentPreset = noiseGenerator.GetCurrentPreset();

        string analysis = $"Terrain Analysis:\n";
        analysis += $"Noise Type: {currentNoiseType}\n";
        analysis += $"Preset: {currentPreset}\n";
        analysis += $"Generation Time: {metrics.generationTime:F3}s\n";
        analysis += $"Average Height: {metrics.averageHeight:F3}\n";
        analysis += $"Height Variance: {metrics.heightVariance:F3}\n";
        analysis += $"Peak Count: {metrics.peakCount}\n";

        // Add performance comparison
        float bestTime = float.MaxValue;
        NoiseGenerator.NoiseType bestNoiseType = currentNoiseType;

        for (int i = 0; i < 4; i++)
        {
            float time = noiseGenerator.GetGenerationTime((NoiseGenerator.NoiseType)i, currentPreset);
            if (time > 0 && time < bestTime)
            {
                bestTime = time;
                bestNoiseType = (NoiseGenerator.NoiseType)i;
            }
        }

        if (bestTime < float.MaxValue)
        {
            analysis += $"\nPerformance:\n";
            analysis += $"Best performing noise for {currentPreset}: {bestNoiseType}\n";
            analysis += $"Time difference: {(metrics.generationTime - bestTime) * 1000:F2}ms slower than best\n";
        }

        analysisText.text = analysis;
    }
}