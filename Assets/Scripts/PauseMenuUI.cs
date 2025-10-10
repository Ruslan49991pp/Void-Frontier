using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// UI компонент для меню паузы
/// Привязывает кнопки к действиям
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Button Names")]
    public string resumeButtonName = "Button_Resume";
    public string quitButtonName = "Button_Quit";
    public string mainMenuButtonName = "Button_MainMenu";

    private Button resumeButton;
    private Button quitButton;
    private Button mainMenuButton;

    void Start()
    {
        FindAndBindButtons();
    }

    /// <summary>
    /// Найти и привязать кнопки
    /// </summary>
    void FindAndBindButtons()
    {
        // Ищем кнопку Resume в дочерних объектах Canvas_Popup
        Transform resumeTransform = transform.Find(resumeButtonName);

        if (resumeTransform == null)
        {
            // Ищем рекурсивно если не нашли напрямую
            resumeTransform = FindChildRecursive(transform, resumeButtonName);
        }

        if (resumeTransform != null)
        {
            resumeButton = resumeTransform.GetComponent<Button>();
            if (resumeButton != null)
            {
                // Добавляем обработчик
                resumeButton.onClick.AddListener(OnResumeButtonClick);
                FileLogger.Log("[PauseMenuUI] Button_Resume found and bound");
            }
            else
            {
                FileLogger.Log("[PauseMenuUI] Button_Resume found but has no Button component");
            }
        }
        else
        {
            FileLogger.Log("[PauseMenuUI] Button_Resume not found in Canvas_Popup");
        }

        // Ищем кнопку Quit
        Transform quitTransform = transform.Find(quitButtonName);

        if (quitTransform == null)
        {
            // Ищем рекурсивно если не нашли напрямую
            quitTransform = FindChildRecursive(transform, quitButtonName);
        }

        if (quitTransform != null)
        {
            quitButton = quitTransform.GetComponent<Button>();
            if (quitButton != null)
            {
                // Добавляем обработчик
                quitButton.onClick.AddListener(OnQuitButtonClick);
                FileLogger.Log("[PauseMenuUI] Button_Quit found and bound");
            }
            else
            {
                FileLogger.Log("[PauseMenuUI] Button_Quit found but has no Button component");
            }
        }
        else
        {
            FileLogger.Log("[PauseMenuUI] Button_Quit not found in Canvas_Popup");
        }

        // Ищем кнопку Main Menu
        Transform mainMenuTransform = transform.Find(mainMenuButtonName);

        if (mainMenuTransform == null)
        {
            // Ищем рекурсивно если не нашли напрямую
            mainMenuTransform = FindChildRecursive(transform, mainMenuButtonName);
        }

        if (mainMenuTransform != null)
        {
            mainMenuButton = mainMenuTransform.GetComponent<Button>();
            if (mainMenuButton != null)
            {
                // Добавляем обработчик
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClick);
                FileLogger.Log("[PauseMenuUI] Button_MainMenu found and bound");
            }
            else
            {
                FileLogger.Log("[PauseMenuUI] Button_MainMenu found but has no Button component");
            }
        }
        else
        {
            FileLogger.Log("[PauseMenuUI] Button_MainMenu not found in Canvas_Popup");
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки Resume
    /// </summary>
    void OnResumeButtonClick()
    {
        FileLogger.Log("[PauseMenuUI] Resume button clicked");

        // Снимаем паузу через PauseMenuManager
        if (PauseMenuManager.Instance != null)
        {
            // Используем тот же метод что и ESC
            if (GamePauseManager.Instance != null && GamePauseManager.Instance.IsPaused())
            {
                GamePauseManager.Instance.SetPaused(false, "Resume Button");
            }
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки Quit
    /// </summary>
    void OnQuitButtonClick()
    {
        FileLogger.Log("[PauseMenuUI] Quit button clicked");

        // Выходим из игры
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Обработчик нажатия кнопки Main Menu
    /// </summary>
    void OnMainMenuButtonClick()
    {
        FileLogger.Log("[PauseMenuUI] Main Menu button clicked");

        // Восстанавливаем нормальное время перед загрузкой сцены
        Time.timeScale = 1f;

        // Переходим в главное меню
        SceneManager.LoadScene("MenuScene");
    }

    /// <summary>
    /// Рекурсивный поиск дочернего объекта по имени
    /// </summary>
    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    void OnDestroy()
    {
        // Отписываемся от событий кнопок
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(OnResumeButtonClick);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitButtonClick);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClick);
        }
    }
}
