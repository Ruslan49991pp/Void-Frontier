using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управление кнопкой Build и панелью BuildMenu
/// </summary>
public class BuildMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Кнопка Build в Canvas_MainUI")]
    public Button buildButton;

    [Tooltip("Панель BuildMenuPanel в Windows")]
    public GameObject buildMenuPanel;

    [Tooltip("Кнопка Close внутри BuildMenuPanel")]
    public Button closeButton;

    void Start()
    {
        Debug.Log("[BuildMenuManager] Start called");

        // Ищем элементы автоматически если не назначены
        if (buildButton == null)
        {
            Debug.Log("[BuildMenuManager] Searching for BuildButton...");
            GameObject buttonObj = FindInactiveObject("BuildButton");
            if (buttonObj != null)
            {
                buildButton = buttonObj.GetComponent<Button>();
                Debug.Log($"[BuildMenuManager] Found BuildButton: {buttonObj.name}");
            }
            else
            {
                Debug.LogError("[BuildMenuManager] BuildButton not found in scene!");
            }
        }

        if (buildMenuPanel == null)
        {
            Debug.Log("[BuildMenuManager] Searching for BuildMenuPanel...");
            buildMenuPanel = FindInactiveObject("BuildMenuPanel");
            if (buildMenuPanel != null)
            {
                Debug.Log($"[BuildMenuManager] Found BuildMenuPanel: {buildMenuPanel.name}, initial active state: {buildMenuPanel.activeSelf}");
            }
            else
            {
                Debug.LogError("[BuildMenuManager] BuildMenuPanel not found in scene!");
            }
        }

        if (closeButton == null && buildMenuPanel != null)
        {
            Debug.Log("[BuildMenuManager] Searching for CloseButton...");
            Transform closeTransform = buildMenuPanel.transform.Find("CloseButton");
            if (closeTransform != null)
            {
                closeButton = closeTransform.GetComponent<Button>();
                Debug.Log($"[BuildMenuManager] Found CloseButton: {closeTransform.name}");
            }
            else
            {
                Debug.LogWarning("[BuildMenuManager] CloseButton not found in BuildMenuPanel!");
            }
        }

        // Проверяем, что все элементы найдены
        if (buildButton == null)
        {
            Debug.LogError("[BuildMenuManager] BuildButton is null - cannot continue!");
            return;
        }

        if (buildMenuPanel == null)
        {
            Debug.LogError("[BuildMenuManager] BuildMenuPanel is null - cannot continue!");
            return;
        }

        if (closeButton == null)
        {
            Debug.LogWarning("[BuildMenuManager] CloseButton is null - close functionality will not work!");
        }

        // Привязываем обработчики
        buildButton.onClick.AddListener(OnBuildButtonClicked);
        Debug.Log("[BuildMenuManager] BuildButton onClick listener added");

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            Debug.Log("[BuildMenuManager] CloseButton onClick listener added");
        }

        // Изначально показываем BuildButton и скрываем BuildMenuPanel
        buildButton.gameObject.SetActive(true);
        buildMenuPanel.SetActive(false);
        Debug.Log($"[BuildMenuManager] Initial state set - BuildButton: visible, BuildMenuPanel: hidden");

        Debug.Log("[BuildMenuManager] Initialized successfully!");
    }

    /// <summary>
    /// Найти объект даже если он неактивен
    /// </summary>
    GameObject FindInactiveObject(string name)
    {
        // Ищем среди всех объектов в сцене, включая неактивные
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == name && obj.scene.IsValid())
            {
                return obj;
            }
        }
        return null;
    }

    /// <summary>
    /// Обработчик клика по BuildButton
    /// </summary>
    void OnBuildButtonClicked()
    {
        Debug.Log("[BuildMenuManager] BuildButton clicked");

        // Показываем панель меню строительства
        buildMenuPanel.SetActive(true);

        // Скрываем кнопку Build
        buildButton.gameObject.SetActive(false);

        // Ставим игру на паузу (это также заблокирует движение камеры)
        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.SetBuildModePause(true);
            Debug.Log("[BuildMenuManager] Game paused and camera blocked");
        }
        else
        {
            Debug.LogWarning("[BuildMenuManager] GamePauseManager not found!");
        }
    }

    /// <summary>
    /// Обработчик клика по CloseButton
    /// </summary>
    void OnCloseButtonClicked()
    {
        CloseBuildMenu();
    }

    /// <summary>
    /// Закрыть меню строительства (публичный метод)
    /// </summary>
    public void CloseBuildMenu()
    {
        Debug.Log("[BuildMenuManager] Closing build menu");

        // Скрываем панель меню строительства
        buildMenuPanel.SetActive(false);

        // Показываем кнопку Build
        buildButton.gameObject.SetActive(true);

        // Снимаем паузу (это также разблокирует движение камеры)
        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.SetBuildModePause(false);
            Debug.Log("[BuildMenuManager] Game unpaused and camera unblocked");
        }
        else
        {
            Debug.LogWarning("[BuildMenuManager] GamePauseManager not found!");
        }
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (buildButton != null)
        {
            buildButton.onClick.RemoveListener(OnBuildButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }
}
