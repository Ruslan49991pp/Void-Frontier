using UnityEngine;

/// <summary>
/// Простой тестер разрешения экрана
/// </summary>
public class ResolutionTester : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode toggleFullscreenKey = KeyCode.F11;
    public KeyCode set1080pKey = KeyCode.F1;
    public KeyCode set1440pKey = KeyCode.F2;
    public KeyCode set4KKey = KeyCode.F3;

    void Start()
    {
        LogCurrentResolution();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleFullscreenKey))
        {
            ToggleFullscreen();
        }

        if (Input.GetKeyDown(set1080pKey))
        {
            SetResolution(1920, 1080);
        }

        if (Input.GetKeyDown(set1440pKey))
        {
            SetResolution(2560, 1440);
        }

        if (Input.GetKeyDown(set4KKey))
        {
            SetResolution(3840, 2160);
        }
    }

    void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, Screen.fullScreen);

        FileLogger.Log($"Resolution changed to {width}x{height}");
    }

    void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;

        FileLogger.Log($"Fullscreen toggled to: {Screen.fullScreen}");
    }

    void LogCurrentResolution()
    {

        FileLogger.Log($"Current resolution: {Screen.width}x{Screen.height}, Fullscreen: {Screen.fullScreen}");
    }

    void OnGUI()
    {
        // Показываем информацию на экране
        int yOffset = 120;
        GUI.Label(new Rect(10, yOffset, 400, 20), $"Current Resolution: {Screen.width} x {Screen.height}");
        GUI.Label(new Rect(10, yOffset + 20, 400, 20), $"Fullscreen: {Screen.fullScreen}");
        GUI.Label(new Rect(10, yOffset + 50, 400, 20), $"Press {toggleFullscreenKey} - Toggle Fullscreen");
        GUI.Label(new Rect(10, yOffset + 70, 400, 20), $"Press {set1080pKey} - Set 1920x1080");
        GUI.Label(new Rect(10, yOffset + 90, 400, 20), $"Press {set1440pKey} - Set 2560x1440");
        GUI.Label(new Rect(10, yOffset + 110, 400, 20), $"Press {set4KKey} - Set 3840x2160");
    }
}