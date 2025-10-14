using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управление кнопками Build и панелями BuildMenu (Ship и Room)
/// Одновременно может быть открыта только одна панель
/// </summary>
public class BuildMenuManager : MonoBehaviour
{
    [Header("Ship Build UI")]
    [Tooltip("Кнопка ShipBuildButton в Canvas_MainUI")]
    public Button shipBuildButton;

    [Tooltip("Панель ShipBuildMenuPanel в Windows")]
    public GameObject shipBuildMenuPanel;

    [Tooltip("Кнопка Close внутри ShipBuildMenuPanel")]
    public Button shipCloseButton;

    [Header("Room Build UI")]
    [Tooltip("Кнопка RoomBuildButton в Canvas_MainUI")]
    public Button roomBuildButton;

    [Tooltip("Панель RoomBuildMenuPanel в Windows")]
    public GameObject roomBuildMenuPanel;

    [Tooltip("Кнопка Close внутри RoomBuildMenuPanel")]
    public Button roomCloseButton;

    void Start()
    {
        if (shipBuildButton == null)
        {
            GameObject buttonObj = FindInactiveObject("ShipBuildButton");
            if (buttonObj != null)
            {
                shipBuildButton = buttonObj.GetComponent<Button>();
            }
            else
            {
                Debug.LogError("[BuildMenuManager] ShipBuildButton not found in scene!");
            }
        }

        if (shipBuildMenuPanel == null)
        {
            shipBuildMenuPanel = FindInactiveObject("ShipBuildMenuPanel");
            if (shipBuildMenuPanel == null)
            {
                Debug.LogError("[BuildMenuManager] ShipBuildMenuPanel not found in scene!");
            }
        }

        if (shipCloseButton == null && shipBuildMenuPanel != null)
        {
            Transform closeTransform = shipBuildMenuPanel.transform.Find("CloseButton");
            if (closeTransform != null)
            {
                shipCloseButton = closeTransform.GetComponent<Button>();
            }
            else
            {
                Debug.LogWarning("[BuildMenuManager] CloseButton not found in ShipBuildMenuPanel!");
            }
        }

        if (roomBuildButton == null)
        {
            GameObject buttonObj = FindInactiveObject("RoomBuildButton");
            if (buttonObj != null)
            {
                roomBuildButton = buttonObj.GetComponent<Button>();
            }
            else
            {
                Debug.LogError("[BuildMenuManager] RoomBuildButton not found in scene!");
            }
        }

        if (roomBuildMenuPanel == null)
        {
            roomBuildMenuPanel = FindInactiveObject("RoomBuildMenuPanel");
            if (roomBuildMenuPanel == null)
            {
                Debug.LogError("[BuildMenuManager] RoomBuildMenuPanel not found in scene!");
            }
        }

        if (roomCloseButton == null && roomBuildMenuPanel != null)
        {
            Transform closeTransform = roomBuildMenuPanel.transform.Find("CloseButton");
            if (closeTransform != null)
            {
                roomCloseButton = closeTransform.GetComponent<Button>();
            }
            else
            {
                Debug.LogWarning("[BuildMenuManager] CloseButton not found in RoomBuildMenuPanel!");
            }
        }

        if (shipBuildButton == null)
        {
            Debug.LogError("[BuildMenuManager] ShipBuildButton is null - cannot continue!");
        }

        if (shipBuildMenuPanel == null)
        {
            Debug.LogError("[BuildMenuManager] ShipBuildMenuPanel is null - cannot continue!");
        }

        if (roomBuildButton == null)
        {
            Debug.LogError("[BuildMenuManager] RoomBuildButton is null - cannot continue!");
        }

        if (roomBuildMenuPanel == null)
        {
            Debug.LogError("[BuildMenuManager] RoomBuildMenuPanel is null - cannot continue!");
        }

        if (shipBuildButton != null)
        {
            shipBuildButton.onClick.AddListener(OnShipBuildButtonClicked);
        }

        if (shipCloseButton != null)
        {
            shipCloseButton.onClick.AddListener(OnShipCloseButtonClicked);
        }

        if (roomBuildButton != null)
        {
            roomBuildButton.onClick.AddListener(OnRoomBuildButtonClicked);
        }

        if (roomCloseButton != null)
        {
            roomCloseButton.onClick.AddListener(OnRoomCloseButtonClicked);
        }

        if (shipBuildButton != null) shipBuildButton.gameObject.SetActive(true);
        if (shipBuildMenuPanel != null) shipBuildMenuPanel.SetActive(false);
        if (roomBuildButton != null) roomBuildButton.gameObject.SetActive(true);
        if (roomBuildMenuPanel != null) roomBuildMenuPanel.SetActive(false);
    }

    GameObject FindInactiveObject(string name)
    {
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

    void OnShipBuildButtonClicked()
    {
        OpenShipBuildMenu();
    }

    void OnRoomBuildButtonClicked()
    {
        OpenRoomBuildMenu();
    }

    void OnShipCloseButtonClicked()
    {
        CloseShipBuildMenu();
    }

    void OnRoomCloseButtonClicked()
    {
        CloseRoomBuildMenu();
    }

    public void OpenShipBuildMenu()
    {
        if (roomBuildMenuPanel != null && roomBuildMenuPanel.activeSelf)
        {
            CloseRoomBuildMenu();
        }

        if (shipBuildMenuPanel != null)
        {
            shipBuildMenuPanel.SetActive(true);
        }

        if (shipBuildButton != null) shipBuildButton.gameObject.SetActive(false);
        if (roomBuildButton != null) roomBuildButton.gameObject.SetActive(false);

        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.SetBuildModePause(true);
        }
        else
        {
            Debug.LogWarning("[BuildMenuManager] GamePauseManager not found!");
        }
    }

    public void OpenRoomBuildMenu()
    {
        if (shipBuildMenuPanel != null && shipBuildMenuPanel.activeSelf)
        {
            CloseShipBuildMenu();
        }

        if (roomBuildMenuPanel != null)
        {
            roomBuildMenuPanel.SetActive(true);
        }

        if (shipBuildButton != null) shipBuildButton.gameObject.SetActive(false);
        if (roomBuildButton != null) roomBuildButton.gameObject.SetActive(false);

        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.SetBuildModePause(true);
        }
        else
        {
            Debug.LogWarning("[BuildMenuManager] GamePauseManager not found!");
        }
    }

    public void CloseShipBuildMenu()
    {
        if (shipBuildMenuPanel != null)
        {
            shipBuildMenuPanel.SetActive(false);
        }

        if (shipBuildButton != null) shipBuildButton.gameObject.SetActive(true);
        if (roomBuildButton != null) roomBuildButton.gameObject.SetActive(true);

        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.SetBuildModePause(false);
        }
        else
        {
            Debug.LogWarning("[BuildMenuManager] GamePauseManager not found!");
        }
    }

    public void CloseRoomBuildMenu()
    {
        if (roomBuildMenuPanel != null)
        {
            roomBuildMenuPanel.SetActive(false);
        }

        if (shipBuildButton != null) shipBuildButton.gameObject.SetActive(true);
        if (roomBuildButton != null) roomBuildButton.gameObject.SetActive(true);

        if (GamePauseManager.Instance != null)
        {
            GamePauseManager.Instance.SetBuildModePause(false);
        }
        else
        {
            Debug.LogWarning("[BuildMenuManager] GamePauseManager not found!");
        }
    }

    public void CloseAllBuildMenus()
    {
        if (shipBuildMenuPanel != null && shipBuildMenuPanel.activeSelf)
        {
            CloseShipBuildMenu();
        }

        if (roomBuildMenuPanel != null && roomBuildMenuPanel.activeSelf)
        {
            CloseRoomBuildMenu();
        }
    }

    void OnDestroy()
    {
        if (shipBuildButton != null)
        {
            shipBuildButton.onClick.RemoveListener(OnShipBuildButtonClicked);
        }

        if (shipCloseButton != null)
        {
            shipCloseButton.onClick.RemoveListener(OnShipCloseButtonClicked);
        }

        if (roomBuildButton != null)
        {
            roomBuildButton.onClick.RemoveListener(OnRoomBuildButtonClicked);
        }

        if (roomCloseButton != null)
        {
            roomCloseButton.onClick.RemoveListener(OnRoomCloseButtonClicked);
        }
    }
}
