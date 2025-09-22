using UnityEngine;

/// <summary>
/// Менеджер первоначальной загрузки игры - выполняется самым первым
/// </summary>
public class BootstrapManager : MonoBehaviour
{
    [Header("Bootstrap Settings")]
    [SerializeField] private bool setResolutionImmediately = true;
    [SerializeField] private int targetWidth = 1920;
    [SerializeField] private int targetHeight = 1080;
    [SerializeField] private bool targetFullscreen = false;

    void Awake()
    {
        // Устанавливаем разрешение максимально рано в процессе загрузки
        if (setResolutionImmediately)
        {
            SetTargetResolution();
        }

        // Убеждаемся что этот объект не будет уничтожен при загрузке новых сцен
        DontDestroyOnLoad(gameObject);
    }

    void SetTargetResolution()
    {
        // Проверяем, нужно ли менять разрешение
        if (Screen.width != targetWidth || Screen.height != targetHeight || Screen.fullScreen != targetFullscreen)
        {
            Screen.SetResolution(targetWidth, targetHeight, targetFullscreen);


            // Логируем через FileLogger если он доступен
            if (FileLogger.Instance != null)
            {
                FileLogger.Log($"Bootstrap: Resolution set to {targetWidth}x{targetHeight}, Fullscreen: {targetFullscreen}");
            }
        }
        else
        {

        }
    }

    /// <summary>
    /// Изменить целевое разрешение (может быть вызвано из настроек)
    /// </summary>
    public void SetTargetResolution(int width, int height, bool fullscreen = false)
    {
        targetWidth = width;
        targetHeight = height;
        targetFullscreen = fullscreen;

        Screen.SetResolution(targetWidth, targetHeight, targetFullscreen);



        if (FileLogger.Instance != null)
        {
            FileLogger.Log($"Bootstrap: Target resolution updated to {targetWidth}x{targetHeight}, Fullscreen: {targetFullscreen}");
        }
    }

    /// <summary>
    /// Получить текущие настройки разрешения
    /// </summary>
    public Vector2Int GetTargetResolution()
    {
        return new Vector2Int(targetWidth, targetHeight);
    }

    /// <summary>
    /// Получить настройку полноэкранного режима
    /// </summary>
    public bool GetTargetFullscreen()
    {
        return targetFullscreen;
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Target Resolution")]
    void EditorApplyResolution()
    {
        SetTargetResolution();
    }

    [ContextMenu("Reset to 1920x1080")]
    void EditorResetTo1080p()
    {
        targetWidth = 1920;
        targetHeight = 1080;
        targetFullscreen = false;
        SetTargetResolution();
    }

    [ContextMenu("Log Current Resolution")]
    void EditorLogResolution()
    {

    }
#endif
}