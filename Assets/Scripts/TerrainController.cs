using UnityEngine;
using UnityEngine.UI;

public class TerrainController : MonoBehaviour
{
    [Header("Terrain Reference")]
    [SerializeField] private NoiseGenerator noiseGenerator;

    [Header("UI References")]
    public Slider scaleSlider;
    public Slider noiseScaleSlider;
    public Slider octavesSlider;
    public Slider persistenceSlider;
    public Slider lacunaritySlider;
    public Button regenerateButton;
    public Button randomSeedButton;

    void Start()
    {
        if (noiseGenerator == null)
        {
            noiseGenerator = GameObject.FindFirstObjectByType<NoiseGenerator>();
        }

        // Setup UI listeners
        if (scaleSlider != null)
            scaleSlider.onValueChanged.AddListener(OnScaleChanged);
        if (noiseScaleSlider != null)
            noiseScaleSlider.onValueChanged.AddListener(OnNoiseScaleChanged);
        if (octavesSlider != null)
            octavesSlider.onValueChanged.AddListener(OnOctavesChanged);
        if (persistenceSlider != null)
            persistenceSlider.onValueChanged.AddListener(OnPersistenceChanged);
        if (lacunaritySlider != null)
            lacunaritySlider.onValueChanged.AddListener(OnLacunarityChanged);
        if (regenerateButton != null)
            regenerateButton.onClick.AddListener(OnRegenerateClicked);
        if (randomSeedButton != null)
            randomSeedButton.onClick.AddListener(OnRandomSeedClicked);
    }

    void OnScaleChanged(float value)
    {
        if (noiseGenerator != null)
            noiseGenerator.SetScale(value);
    }

    void OnNoiseScaleChanged(float value)
    {
        if (noiseGenerator != null)
            noiseGenerator.SetNoiseScale(value);
    }

    void OnOctavesChanged(float value)
    {
        if (noiseGenerator != null)
            noiseGenerator.SetOctaves(Mathf.RoundToInt(value));
    }

    void OnPersistenceChanged(float value)
    {
        if (noiseGenerator != null)
            noiseGenerator.SetPersistence(value);
    }

    void OnLacunarityChanged(float value)
    {
        if (noiseGenerator != null)
            noiseGenerator.SetLacunarity(value);
    }

    void OnRegenerateClicked()
    {
        if (noiseGenerator != null)
            noiseGenerator.GenerateTerrain();
    }

    void OnRandomSeedClicked()
    {
        if (noiseGenerator != null)
        {
            noiseGenerator.SetRandomSeed();
            noiseGenerator.GenerateTerrain();
        }
    }
}