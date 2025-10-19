using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls game speed similar to RimWorld
/// Manages pause and speed multipliers (1x, 2x, 4x)
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    [Header("Speed Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button speed2xButton;
    [SerializeField] private Button speed4xButton;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private float currentTimeScale = 1f;

    private void Start()
    {
        // Wire up button listeners
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (speed2xButton != null)
            speed2xButton.onClick.AddListener(OnSpeed2xClicked);

        if (speed4xButton != null)
            speed4xButton.onClick.AddListener(OnSpeed4xClicked);

        // Start at normal speed
        SetGameSpeed(1f);
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPauseClicked);

        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (speed2xButton != null)
            speed2xButton.onClick.RemoveListener(OnSpeed2xClicked);

        if (speed4xButton != null)
            speed4xButton.onClick.RemoveListener(OnSpeed4xClicked);
    }

    /// <summary>
    /// Pause the game (timeScale = 0)
    /// </summary>
    private void OnPauseClicked()
    {
        SetGameSpeed(0f);
        UpdateButtonVisuals(pauseButton);
    }

    /// <summary>
    /// Normal game speed (timeScale = 1)
    /// </summary>
    private void OnPlayClicked()
    {
        SetGameSpeed(1f);
        UpdateButtonVisuals(playButton);
    }

    /// <summary>
    /// 2x game speed (timeScale = 2)
    /// </summary>
    private void OnSpeed2xClicked()
    {
        SetGameSpeed(2f);
        UpdateButtonVisuals(speed2xButton);
    }

    /// <summary>
    /// 4x game speed (timeScale = 4)
    /// </summary>
    private void OnSpeed4xClicked()
    {
        SetGameSpeed(4f);
        UpdateButtonVisuals(speed4xButton);
    }

    /// <summary>
    /// Sets the game time scale
    /// </summary>
    private void SetGameSpeed(float speed)
    {
        currentTimeScale = speed;
        Time.timeScale = speed;

        Debug.Log($"Game speed set to: {speed}x");
    }

    /// <summary>
    /// Updates button visual feedback to show which speed is active
    /// </summary>
    private void UpdateButtonVisuals(Button activeButton)
    {
        // Reset all buttons to normal color
        SetButtonColor(pauseButton, normalColor);
        SetButtonColor(playButton, normalColor);
        SetButtonColor(speed2xButton, normalColor);
        SetButtonColor(speed4xButton, normalColor);

        // Highlight the active button
        SetButtonColor(activeButton, selectedColor);
    }

    /// <summary>
    /// Helper method to set button color
    /// </summary>
    private void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            button.colors = colors;
        }
    }

    /// <summary>
    /// Public method to get current game speed
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentTimeScale;
    }

    /// <summary>
    /// Toggle pause (useful for keyboard shortcut)
    /// </summary>
    public void TogglePause()
    {
        if (currentTimeScale == 0f)
        {
            OnPlayClicked();
        }
        else
        {
            OnPauseClicked();
        }
    }

    /// <summary>
    /// Handle keyboard shortcuts
    /// </summary>
    private void Update()
    {
        // Space - toggle pause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }

        // 1 - normal speed
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            OnPlayClicked();
        }

        // 2 - 2x speed
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            OnSpeed2xClicked();
        }

        // 4 - 4x speed
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            OnSpeed4xClicked();
        }
    }
}
