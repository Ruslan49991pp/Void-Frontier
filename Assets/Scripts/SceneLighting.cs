using UnityEngine;

/// <summary>
/// Автоматическое создание базового освещения для сцены
/// </summary>
public class SceneLighting : MonoBehaviour
{
    [Header("Lighting Settings")]
    public bool createDirectionalLight = true;
    public bool createAmbientLight = true;
    public Color directionalLightColor = Color.white;
    public float directionalLightIntensity = 1.0f;
    public Vector3 directionalLightRotation = new Vector3(50f, -30f, 0f);

    [Header("Ambient Settings")]
    public Color ambientColor = new Color(0.4f, 0.4f, 0.5f, 1f);
    public float ambientIntensity = 0.3f;

    void Start()
    {
        SetupSceneLighting();
    }

    [ContextMenu("Setup Scene Lighting")]
    public void SetupSceneLighting()
    {
        if (createDirectionalLight)
        {
            CreateDirectionalLight();
        }

        if (createAmbientLight)
        {
            SetupAmbientLighting();
        }
    }

    void CreateDirectionalLight()
    {
        // Проверяем, есть ли уже основной свет в сцене
        Light existingLight = FindObjectOfType<Light>();
        if (existingLight != null && existingLight.type == LightType.Directional)
        {
            return;
        }

        // Создаем новый направленный свет
        GameObject lightGO = new GameObject("Main Directional Light");
        Light directionalLight = lightGO.AddComponent<Light>();

        directionalLight.type = LightType.Directional;
        directionalLight.color = directionalLightColor;
        directionalLight.intensity = directionalLightIntensity;
        directionalLight.shadows = LightShadows.Soft;

        // Устанавливаем поворот света
        lightGO.transform.rotation = Quaternion.Euler(directionalLightRotation);
    }

    void SetupAmbientLighting()
    {
        // Настраиваем окружающий свет
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;
    }

    /// <summary>
    /// Автоматическая инициализация при загрузке сцены
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSetupLighting()
    {
        // Проверяем, есть ли уже SceneLighting в сцене
        SceneLighting existing = FindObjectOfType<SceneLighting>();
        if (existing == null)
        {
            // Создаем автоматически
            GameObject lightingGO = new GameObject("SceneLighting");
            SceneLighting lighting = lightingGO.AddComponent<SceneLighting>();
        }
    }
}