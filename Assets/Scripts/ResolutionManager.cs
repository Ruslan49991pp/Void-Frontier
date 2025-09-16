using UnityEngine;

/// <summary>
/// Управление разрешением экрана игры
/// </summary>
public class ResolutionManager : MonoBehaviour
{
    [Header("Default Resolution Settings")]
    public int defaultWidth = 1920;
    public int defaultHeight = 1080;
    public bool fullScreen = false;

    [Header("Settings")]
    public bool setResolutionOnStart = true;
    public bool forceResolution = true;

    void Awake()
    {
        // Устанавливаем разрешение как можно раньше
        if (setResolutionOnStart)
        {
            SetDefaultResolution();
        }
    }

    void Start()
    {
        // Дополнительная проверка в Start если нужно принудительно установить
        if (forceResolution && (Screen.width != defaultWidth || Screen.height != defaultHeight))
        {
            SetDefaultResolution();
        }

        LogCurrentResolution();
    }

    /// <summary>
    /// Установить разрешение по умолчанию
    /// </summary>
    public void SetDefaultResolution()
    {
        Screen.SetResolution(defaultWidth, defaultHeight, fullScreen);
        FileLogger.Log($"Resolution set to {defaultWidth}x{defaultHeight}, Fullscreen: {fullScreen}");
    }

    /// <summary>
    /// Установить пользовательское разрешение
    /// </summary>
    public void SetResolution(int width, int height, bool isFullScreen = false)
    {
        Screen.SetResolution(width, height, isFullScreen);
        FileLogger.Log($"Custom resolution set to {width}x{height}, Fullscreen: {isFullScreen}");
    }

    /// <summary>
    /// Переключить в полноэкранный режим
    /// </summary>
    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        FileLogger.Log($"Fullscreen toggled to: {Screen.fullScreen}");
    }

    /// <summary>
    /// Вывести текущее разрешение в лог
    /// </summary>
    void LogCurrentResolution()
    {
        FileLogger.Log($"Current screen resolution: {Screen.width}x{Screen.height}, Fullscreen: {Screen.fullScreen}");
    }

    void OnValidate()
    {
        // Проверяем разумные значения в инспекторе
        if (defaultWidth < 800) defaultWidth = 800;
        if (defaultHeight < 600) defaultHeight = 600;
    }

#if UNITY_EDITOR
    [ContextMenu("Set Resolution Now")]
    void EditorSetResolution()
    {
        SetDefaultResolution();
    }

    [ContextMenu("Log Current Resolution")]
    void EditorLogResolution()
    {
        LogCurrentResolution();
    }
#endif
}