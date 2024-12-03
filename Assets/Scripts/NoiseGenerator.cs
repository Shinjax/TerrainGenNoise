using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class NoiseGenerator : MonoBehaviour
{
    public enum NoiseType 
    { 
        Perlin = 0,
        Value = 1,
        Worley = 2,
        Simplex = 3
    }

    public enum TerrainPreset 
    { 
        Mountains, 
        Plains, 
        Hills, 
        Coastal, 
        Combined 
    }

    [System.Serializable]
    public struct PresetParameters
    {
        public float scale;        // Overall height of the terrain
        public float noiseScale;   // How zoomed in the noise is
        public int octaves;        // Number of noise layers
        public float persistence;  // How much each octave contributes
        public float lacunarity;   // How much detail is added in each octave
        public float heightMultiplier; // more height scaling if needed
        public float heightOffset; // Base height offset
    }

    [Header("Terrain Settings")]
    [SerializeField] private int width = 256;
    [SerializeField] private int height = 256;
    [SerializeField] private float scale = 20f;

    [Header("Noise Settings")]
    [SerializeField] public NoiseType noiseType = NoiseType.Perlin;
    [SerializeField] private TMP_Dropdown noiseTypeDropdown;
    [SerializeField] private float noiseScale = 0.3f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private int seed = 0;
    [SerializeField] private float heightMultiplier = 1f;
    [SerializeField] private float heightOffset = 0f;

    [Header("Preset Settings")]
    public PresetParameters plainsPreset = new PresetParameters
    {
        scale = 50f,
        noiseScale = 70f,  // Increased for smoothness
        octaves = 2,
        persistence = 0.25f,  // Reduced for less detail variation
        lacunarity = 1.3f,  // Reduced for more gradual changes
        heightMultiplier = 0.12f,  // Slightly lower height
        heightOffset = 0f
    };

    public PresetParameters hillsPreset = new PresetParameters
    {
        scale = 50f,
        noiseScale = 200f,        // Large scale for gentle rolling hills
        octaves = 2,              // just want simple hills so not too much noise
        persistence = 0.3f,       // Lower for gentler variation
        lacunarity = 1.5f,        // Gentler frequency increase
        heightMultiplier = 0.35f, // Moderate height
        heightOffset = 0.05f      // Slight base elevation
    };

    public PresetParameters mountainsPreset = new PresetParameters
    {
        scale = 80f,
        noiseScale = 20f,
        octaves = 6,
        persistence = 0.55f,
        lacunarity = 2.5f,
        heightMultiplier = 1.0f,
        heightOffset = 0f
    };

    public PresetParameters combinedPreset = new PresetParameters
    {
        scale = 75f,
        noiseScale = 50f,
        octaves = 6,        // Increased octaves because we need multiple noise types and layers
        persistence = 0.5f,
        lacunarity = 2.0f,
        heightMultiplier = 1.0f,
        heightOffset = 0.0f
    };

    public PresetParameters coastalPreset = new PresetParameters
    {
        scale = 60f,
        noiseScale = 40f,
        octaves = 4,
        persistence = 0.6f,
        lacunarity = 1.8f,
        heightMultiplier = 0.7f,
        heightOffset = 0.2f
    };

    [Header("Falloff Settings")]
    [SerializeField] private bool useFalloff = true;
    [SerializeField] private float falloffStrength = 3f;
    [SerializeField] private float falloffScale = 2.2f;

    private float[,] falloffMap;
    private TerrainPreset currentPreset;
    private Terrain terrain;
    private TerrainData terrainData;

    private float[,] generationTimes = new float[System.Enum.GetValues(typeof(NoiseType)).Length, System.Enum.GetValues(typeof(TerrainPreset)).Length]; // [NoiseType count, TerrainPreset count]

    void Start()
    {
        terrain = GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No Terrain component found!");
            return;
        }

        terrainData = terrain.terrainData;
        GenerateFalloffMap();

        // Initialize noise type dropdown
        if (noiseTypeDropdown != null)
        {
            noiseTypeDropdown.ClearOptions();
            noiseTypeDropdown.AddOptions(new List<string> { 
                "Perlin Noise",
                "Value Noise", 
                "Worley Noise",
                "Simplex Noise"
            });
            noiseTypeDropdown.value = (int)noiseType;
            noiseTypeDropdown.onValueChanged.AddListener(OnNoiseTypeChanged);
        }

        GenerateTerrain();
    }

    public void OnNoiseTypeChanged(int index)
    {
        noiseType = (NoiseType)index;
        RegenerateWithCurrentSeed();
    }

    private void GenerateFalloffMap()
    {
        falloffMap = new float[width, height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float xv = x / (float)width * 2 - 1;
                float yv = y / (float)height * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));
                falloffMap[x, y] = Evaluate(value);
            }
        }
    }

    private float Evaluate(float value)
    {
        float a = falloffScale;
        float b = falloffStrength;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }

    public void ApplyPreset(TerrainPreset preset)
    {
        currentPreset = preset;
        PresetParameters parameters = preset switch
        {
            TerrainPreset.Plains => plainsPreset,
            TerrainPreset.Hills => hillsPreset,
            TerrainPreset.Mountains => mountainsPreset,
            TerrainPreset.Coastal => coastalPreset,
            TerrainPreset.Combined => combinedPreset,
            _ => plainsPreset
        };

        scale = parameters.scale;
        noiseScale = parameters.noiseScale;
        octaves = parameters.octaves;
        persistence = parameters.persistence;
        lacunarity = parameters.lacunarity;
        heightMultiplier = parameters.heightMultiplier;
        heightOffset = parameters.heightOffset;

        RegenerateWithCurrentSeed();
    }

    public void SetNoiseType(NoiseType type)
    {
        noiseType = type;
        RegenerateWithCurrentSeed();
    }

    public void SetScale(float newScale) { scale = newScale; RegenerateWithCurrentSeed(); }
    public void SetNoiseScale(float newNoiseScale) { noiseScale = newNoiseScale; RegenerateWithCurrentSeed(); }
    public void SetOctaves(int newOctaves) { octaves = newOctaves; RegenerateWithCurrentSeed(); }
    public void SetPersistence(float newPersistence) { persistence = newPersistence; RegenerateWithCurrentSeed(); }
    public void SetLacunarity(float newLacunarity) { lacunarity = newLacunarity; RegenerateWithCurrentSeed(); }
    public void SetRandomSeed() { seed = Random.Range(-10000, 10000); RegenerateWithCurrentSeed(); }

    public NoiseType GetNoiseType() => noiseType;
    public TerrainPreset GetCurrentPreset() => currentPreset;
    public int GetTerrainWidth() => width;
    public int GetTerrainHeight() => height;

    public void GenerateTerrain()
    {
        if (terrain == null || terrainData == null) return;

        if (terrainData.heightmapResolution != width + 1)
        {
            terrainData.heightmapResolution = width + 1;
            GenerateFalloffMap();
        }

        GenerateOctaveOffsets();
        
        float[,] heights = GenerateHeights();
        terrainData.SetHeights(0, 0, heights);
    }

    public void RegenerateWithCurrentSeed()
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, scale, height);

        float[,] heights = GenerateHeights();
        terrainData.SetHeights(0, 0, heights);
    }

    public void SetTerrainSize(int newWidth, int newHeight)
    {
        width = newWidth;
        height = newHeight;
        GenerateFalloffMap();
    }

    private void GenerateOctaveOffsets()
    {
        System.Random prng = new System.Random(seed);
        int maxOctaves = 8;  
        Vector2[] octaveOffsets = new Vector2[maxOctaves];
        
        for (int i = 0; i < maxOctaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
    }

    private float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        
        // Always generate enough offsets for maximum possible usage
        int maxOctaves = 8;  
        Vector2[] octaveOffsets = new Vector2[maxOctaves];
        
        for (int i = 0; i < maxOctaves; i++)
        {
            float offsetX = Random.Range(-100000, 100000);
            float offsetY = Random.Range(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float startTime = Time.realtimeSinceStartup;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noiseHeight = 0;
                float amplitude = 1;
                float frequency = 1;

                if (currentPreset == TerrainPreset.Coastal)
                {
                    // Generate coastline using Worley noise with larger scale
                    float coastX = (x - width / 2f) / (noiseScale * 8f);
                    float coastY = (y - height / 2f) / (noiseScale * 8f);
                    float coastNoise = WorleyNoise(coastX + octaveOffsets[0].x, coastY + octaveOffsets[0].y);
                    
                    // Add multiple layers of Perlin noise for natural variation
                    float detail1X = (x - width / 2f) / (noiseScale * 4f);
                    float detail1Y = (y - height / 2f) / (noiseScale * 4f);
                    float detail1Noise = Mathf.PerlinNoise(detail1X + octaveOffsets[1].x, detail1Y + octaveOffsets[1].y);
                    
                    float detail2X = (x - width / 2f) / (noiseScale * 2f);
                    float detail2Y = (y - height / 2f) / (noiseScale * 2f);
                    float detail2Noise = Mathf.PerlinNoise(detail2X + octaveOffsets[2].x, detail2Y + octaveOffsets[2].y);
                    
                    // Combine noises with weighted blend
                    float combined = coastNoise * 0.5f + detail1Noise * 0.3f + detail2Noise * 0.2f;
                    
                    // Smooth transition for coastline using smoothstep
                    float threshold = 0.45f;
                    if (combined < threshold)
                    {
                        // Water areas with gentle slopes
                        combined = Mathf.SmoothStep(0.1f, threshold, combined);
                        combined *= 0.3f; // Keep water low
                    }
                    else
                    {
                        // Land areas with natural elevation
                        combined = Mathf.SmoothStep(threshold, 0.9f, combined);
                        combined = Mathf.Lerp(0.3f, 1f, combined);
                    }
                    
                    noiseHeight = combined;
                }
                else if (currentPreset == TerrainPreset.Combined)
                {
                    // Generate base terrain using Perlin noise
                    float baseX = (x - width / 2f) / (noiseScale * 4f);
                    float baseY = (y - height / 2f) / (noiseScale * 4f);
                    float baseNoise = GetNoiseValue(baseX + octaveOffsets[0].x, baseY + octaveOffsets[0].y);

                    // Add Worley noise for some distinct features 
                    float worleyX = (x - width / 2f) / (noiseScale * 2f);
                    float worleyY = (y - height / 2f) / (noiseScale * 2f);
                    float worleyNoise = WorleyNoise(worleyX + octaveOffsets[1].x, worleyY + octaveOffsets[1].y);

                    // Add third noise layer for even more detail
                    float detailX = (x - width / 2f) / noiseScale;
                    float detailY = (y - height / 2f) / noiseScale;
                    float detailNoise = GetNoiseValue(detailX + octaveOffsets[2].x, detailY + octaveOffsets[2].y);

                    // Combine all noise types with different weights
                    noiseHeight = baseNoise * 0.5f + worleyNoise * 0.3f + detailNoise * 0.2f;
                    noiseHeight = Mathf.Pow(noiseHeight, 1.2f); // Add some contrast
                }
                else if (currentPreset == TerrainPreset.Hills)
                {
                    // Generate very smooth base using large scale Perlin noise because its smooth
                    float baseX = (x - width / 2f) / (noiseScale * 8f);
                    float baseY = (y - height / 2f) / (noiseScale * 8f);
                    float baseNoise = Mathf.PerlinNoise(baseX + octaveOffsets[0].x, baseY + octaveOffsets[0].y);
                    
                    // Add gentle undulations with medium scale noise
                    float medX = (x - width / 2f) / (noiseScale * 4f);
                    float medY = (y - height / 2f) / (noiseScale * 4f);
                    float medNoise = Mathf.PerlinNoise(medX + octaveOffsets[1].x, medY + octaveOffsets[1].y);
                    
                    // Very subtle high frequency detail
                    float detailX = (x - width / 2f) / (noiseScale * 2f);
                    float detailY = (y - height / 2f) / (noiseScale * 2f);
                    float detailNoise = Mathf.PerlinNoise(detailX + octaveOffsets[2].x, detailY + octaveOffsets[2].y);
                    
                    // Blend layers with strong emphasis on the smooth base
                    float combinedNoise = baseNoise * 0.7f + medNoise * 0.25f + detailNoise * 0.05f;
                    
                    // Very gentle power function to avoid sharp peaks
                    combinedNoise = Mathf.Pow(combinedNoise, 1.2f);
                    
                    // Smooth interpolation for gentler transitions
                    combinedNoise = Mathf.SmoothStep(0.2f, 0.8f, combinedNoise);
                    
                    // Lower overall height for rolling hills
                    noiseHeight = combinedNoise * 0.6f;
                }
                else
                {
                    // Original noise generation for other terrain types
                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x + octaveOffsets[i].x) / noiseScale * frequency;
                        float sampleY = (y + octaveOffsets[i].y) / noiseScale * frequency;

                        float noiseValue = GetNoiseValue(sampleX, sampleY);
                        noiseHeight += noiseValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }
                }

                heights[x, y] = noiseHeight * heightMultiplier + heightOffset;
            }
        }

        // Find min/max heights
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (heights[x, y] > maxHeight) maxHeight = heights[x, y];
                if (heights[x, y] < minHeight) minHeight = heights[x, y];
            }
        }

        // Normalize and process heights
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                heights[x, y] = Mathf.InverseLerp(minHeight, maxHeight, heights[x, y]);

                if (useFalloff && currentPreset != TerrainPreset.Hills)
                {
                    heights[x, y] = Mathf.Lerp(heights[x, y], heights[x, y] * (1 - falloffMap[x, y]), 0.5f);
                }

                switch (currentPreset)
                {
                    case TerrainPreset.Plains:
                        float plainHeight = heights[x, y];
                        plainHeight = Mathf.Pow(plainHeight, noiseType == NoiseType.Simplex ? 1.8f : 2.2f);
                        plainHeight *= noiseType == NoiseType.Simplex ? 0.5f : 0.6f;

                        if (x > 1 && x < width-2 && y > 1 && y < height-2)
                        {
                            float average = (
                                heights[x-2, y] + heights[x-1, y] + heights[x+1, y] + heights[x+2, y] +
                                heights[x, y-2] + heights[x, y-1] + heights[x, y+1] + heights[x, y+2] +
                                heights[x-1, y-1] + heights[x+1, y-1] + heights[x-1, y+1] + heights[x+1, y+1]
                            ) / 12f;
                            plainHeight = Mathf.Lerp(plainHeight, average, noiseType == NoiseType.Simplex ? 0.7f : 0.6f);
                        }
                        heights[x, y] = plainHeight;
                        break;

                    case TerrainPreset.Mountains:
                        float mountainHeight = heights[x, y];
                        float power = noiseType == NoiseType.Simplex ? 2.8f : 3f;
                        mountainHeight = Mathf.Pow(mountainHeight, power);
                        mountainHeight = Mathf.Lerp(heights[x, y], mountainHeight, 0.7f);
                        heights[x, y] = mountainHeight;
                        break;

                    case TerrainPreset.Hills:
                        if (noiseType != NoiseType.Simplex)
                        {
                            // Keep existing hills code for other noise types
                            // Generate very smooth base using large scale Perlin noise
                            float baseX = (x - width / 2f) / (noiseScale * 8f);
                            float baseY = (y - height / 2f) / (noiseScale * 8f);
                            float baseNoise = Mathf.PerlinNoise(baseX + octaveOffsets[0].x, baseY + octaveOffsets[0].y);
                            
                            // Add gentle undulations with medium scale noise
                            float medX = (x - width / 2f) / (noiseScale * 4f);
                            float medY = (y - height / 2f) / (noiseScale * 4f);
                            float medNoise = Mathf.PerlinNoise(medX + octaveOffsets[1].x, medY + octaveOffsets[1].y);
                            
                            // Very subtle high-frequency detail
                            float detailX = (x - width / 2f) / (noiseScale * 2f);
                            float detailY = (y - height / 2f) / (noiseScale * 2f);
                            float detailNoise = Mathf.PerlinNoise(detailX + octaveOffsets[2].x, detailY + octaveOffsets[2].y);
                            
                            // Blend layers with strong emphasis on the smooth base
                            float combinedNoise = baseNoise * 0.7f + medNoise * 0.25f + detailNoise * 0.05f;
                            
                            // Very gentle power function to avoid sharp peaks
                            combinedNoise = Mathf.Pow(combinedNoise, 1.2f);
                            
                            // Smooth interpolation for gentler transitions
                            combinedNoise = Mathf.SmoothStep(0.2f, 0.8f, combinedNoise);
                            
                            // Lower overall height for rolling hills
                            heights[x, y] = combinedNoise * 0.6f;
                        }
                        else
                        {
                            // Enhanced Simplex hills generation
                            float hillHeight = heights[x, y];
                            
                            // More aggressive power function for pronounced hills
                            hillHeight = Mathf.Pow(Mathf.Abs(hillHeight), 1.2f) * Mathf.Sign(hillHeight);
                            
                            // Apply ridge formation for more hilly features
                            hillHeight = 1.0f - Mathf.Abs(hillHeight);
                            hillHeight = Mathf.Pow(hillHeight, 2f);
                            
                            // Apply smoothing for natural transitions
                            if (x > 1 && x < width-2 && y > 1 && y < height-2)
                            {
                                float average = (
                                    heights[x-1, y] + heights[x+1, y] + 
                                    heights[x, y-1] + heights[x, y+1]
                                ) / 4f;
                                hillHeight = Mathf.Lerp(hillHeight, average, 0.3f);
                            }
                            
                            // Adjust final height and add variation
                            heights[x, y] = hillHeight * 1.2f; 
                        }
                        break;
                }

                heights[x, y] = heights[x, y] * heightMultiplier + heightOffset;
                heights[x, y] = Mathf.Clamp01(heights[x, y]);
            }
        }

        // Record gen time for metrics
        float endTime = Time.realtimeSinceStartup;
        generationTimes[(int)noiseType, (int)currentPreset] = endTime - startTime;

        return heights;
    }

    float GetNoiseValue(float x, float y)
    {
        switch (noiseType)
        {
            case NoiseType.Perlin:
                return Mathf.PerlinNoise(x, y) * 2 - 1;

            case NoiseType.Value:
                return ValueNoise(x, y) * 2 - 1;

            case NoiseType.Worley:
                return WorleyNoise(x, y) * 2 - 1;

            case NoiseType.Simplex:
                // Scale input coordinates for better terrain formation
                float scale = currentPreset switch
                {
                    TerrainPreset.Mountains => 2.0f,
                    TerrainPreset.Plains => 0.8f,
                    TerrainPreset.Hills => 1.5f,
                    _ => 1.0f
                };
                
                // Add multiple octaves of Simplex noise for more interesting terrain
                float noise = 0;
                float amplitude = 1;
                float frequency = 1;
                float maxValue = 0;
                
                for(int i = 0; i < 3; i++)
                {
                    noise += SimplexNoise(x * scale * frequency, y * scale * frequency) * amplitude;
                    maxValue += amplitude;
                    amplitude *= 0.5f;
                    frequency *= 2;
                }
                
                return (noise / maxValue) * 1.5f;

            default:
                return Mathf.PerlinNoise(x, y) * 2 - 1;
        }
    }

    float ValueNoise(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float fractionalX = x - x0;
        float fractionalY = y - y0;

        float v00 = Random01(x0, y0);
        float v10 = Random01(x1, y0);
        float v01 = Random01(x0, y1);
        float v11 = Random01(x1, y1);

        float vx0 = Mathf.Lerp(v00, v10, SmoothStep(fractionalX));
        float vx1 = Mathf.Lerp(v01, v11, SmoothStep(fractionalX));
        float vy = Mathf.Lerp(vx0, vx1, SmoothStep(fractionalY));

        return vy;
    }

    float WorleyNoise(float x, float y)
    {
        int xi = Mathf.FloorToInt(x);
        int yi = Mathf.FloorToInt(y);
        float xf = x - xi;
        float yf = y - yi;

        float minDist = 1.0f;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Vector2 cell = new Vector2(xi + i, yi + j);
                Vector2 point = cell + GetRandomPoint(cell);
                float dist = Vector2.Distance(new Vector2(x, y), point);
                minDist = Mathf.Min(minDist, dist);
            }
        }

        return minDist;
    }

    float Random01(int x, int y)
    {
        return (Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1;
    }

    float SmoothStep(float t)
    {
        return t * t * (3 - 2 * t);
    }

    Vector2 GetRandomPoint(Vector2 cell)
    {
        float x = Random01(Mathf.FloorToInt(cell.x), Mathf.FloorToInt(cell.y));
        float y = Random01(Mathf.FloorToInt(cell.y), Mathf.FloorToInt(cell.x));
        return new Vector2(x, y);
    }

    public float[,] GenerateNoiseMap()
    {
        float[,] noiseMap = new float[width, height];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - width / 2f + octaveOffsets[i].x) / noiseScale * frequency;
                    float sampleY = (y - height / 2f + octaveOffsets[i].y) / noiseScale * frequency;

                    float perlinValue = 0;
                    switch (noiseType)
                    {
                        case NoiseType.Perlin:
                            perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                            break;
                        case NoiseType.Value:
                            perlinValue = ValueNoise(sampleX, sampleY) * 2 - 1;
                            break;
                        case NoiseType.Worley:
                            perlinValue = WorleyNoise(sampleX, sampleY) * 2 - 1;
                            break;
                        case NoiseType.Simplex:
                            perlinValue = SimplexNoise(sampleX, sampleY) * 2 - 1;
                            break;
                    }

                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                noiseMap[x, y] = noiseMap[x, y] * heightMultiplier + heightOffset;
            }
        }

        return noiseMap;
    }

    public float[,] GenerateNoiseMap(int width, int height, int seed, float scale, float noiseScale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[width, height];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - width / 2f + octaveOffsets[i].x + offset.x) / noiseScale * frequency;
                    float sampleY = (y - height / 2f + octaveOffsets[i].y + offset.y) / noiseScale * frequency;

                    float perlinValue = 0;
                    switch (noiseType)
                    {
                        case NoiseType.Perlin:
                            perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                            break;
                        case NoiseType.Value:
                            perlinValue = ValueNoise(sampleX, sampleY) * 2 - 1;
                            break;
                        case NoiseType.Worley:
                            perlinValue = WorleyNoise(sampleX, sampleY) * 2 - 1;
                            break;
                        case NoiseType.Simplex:
                            perlinValue = SimplexNoise(sampleX, sampleY) * 2 - 1;
                            break;
                    }

                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                noiseMap[x, y] = noiseMap[x, y] * heightMultiplier + heightOffset;
            }
        }

        return noiseMap;
    }

    public void ApplyHeightMap(float[,] heightMap)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, scale, height);
        terrainData.SetHeights(0, 0, heightMap);
    }

    public float GetGenerationTime(NoiseType type, TerrainPreset preset)
    {
        return generationTimes[(int)type, (int)preset];
    }

    private float SimplexNoise(float x, float y)
    {
        const float F2 = 0.366025403f; // 0.5*(sqrt(3.0)-1.0)
        const float G2 = 0.211324865f; // (3.0-Math.sqrt(3.0))/6.0

        float n0, n1, n2; // Noise contributions from the three corners

        // Skew the input space to determine which simplex cell 
        float s = (x + y) * F2;
        float xs = x + s;
        float ys = y + s;
        int i = Mathf.FloorToInt(xs);
        int j = Mathf.FloorToInt(ys);

        float t = (i + j) * G2;
        float X0 = i - t;
        float Y0 = j - t;
        float x0 = x - X0;
        float y0 = y - Y0;

        // Determine which simplex we in 
        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1.0f + 2.0f * G2;
        float y2 = y0 - 1.0f + 2.0f * G2;

        // Calculate contributions from the three corners
        float t0 = 0.5f - x0 * x0 - y0 * y0;
        if (t0 < 0.0f)
        {
            n0 = 0.0f;
        }
        else
        {
            t0 *= t0;
            // Gradient calculation using improved hash function
            int h0 = HashCoord(i, j);
            float grad0 = GetSimplexGrad(h0, x0, y0);
            n0 = t0 * t0 * grad0;
        }

        float t1 = 0.5f - x1 * x1 - y1 * y1;
        if (t1 < 0.0f)
        {
            n1 = 0.0f;
        }
        else
        {
            t1 *= t1;
            int h1 = HashCoord(i + i1, j + j1);
            float grad1 = GetSimplexGrad(h1, x1, y1);
            n1 = t1 * t1 * grad1;
        }

        float t2 = 0.5f - x2 * x2 - y2 * y2;
        if (t2 < 0.0f)
        {
            n2 = 0.0f;
        }
        else
        {
            t2 *= t2;
            int h2 = HashCoord(i + 1, j + 1);
            float grad2 = GetSimplexGrad(h2, x2, y2);
            n2 = t2 * t2 * grad2;
        }

        // Scale to [-1, 1]
        return (32.0f * (n0 + n1 + n2));
    }

    private int HashCoord(int x, int y)
    {
        int hash = (x * 1619 + y * 31337) & 0x7fffffff;
        hash = ((hash * hash * hash * 60493) + (hash * hash * 19990303) + (hash * 1376312589)) & 0x7fffffff;
        return hash;
    }

    private float GetSimplexGrad(int hash, float x, float y)
    {
        hash = hash & 7;
        float u = (hash < 4) ? x : y;
        float v = (hash < 4) ? y : x;
        return ((hash & 1) == 0 ? u : -u) + ((hash & 2) == 0 ? v : -v);
    }
}
