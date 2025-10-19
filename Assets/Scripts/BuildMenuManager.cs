using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// РЈРїСЂР°РІР»РµРЅРёРµ РєРЅРѕРїРєР°РјРё Build Рё РїР°РЅРµР»СЏРјРё BuildMenu (Ship Рё Room)
/// РћРґРЅРѕРІСЂРµРјРµРЅРЅРѕ РјРѕР¶РµС‚ Р±С‹С‚СЊ РѕС‚РєСЂС‹С‚Р° С‚РѕР»СЊРєРѕ РѕРґРЅР° РїР°РЅРµР»СЊ
/// </summary>
public class BuildMenuManager : MonoBehaviour
{
    [Header("Ship Build UI")]
    [Tooltip("РљРЅРѕРїРєР° ShipBuildButton РІ Canvas_MainUI")]
    public Button shipBuildButton;

    [Tooltip("РџР°РЅРµР»СЊ ShipBuildMenuPanel РІ Windows")]
    public GameObject shipBuildMenuPanel;

    [Tooltip("РљРЅРѕРїРєР° Close РІРЅСѓС‚СЂРё ShipBuildMenuPanel")]
    public Button shipCloseButton;

    [Header("Room Build UI")]
    [Tooltip("РљРЅРѕРїРєР° RoomBuildButton РІ Canvas_MainUI")]
    public Button roomBuildButton;

    [Tooltip("РџР°РЅРµР»СЊ RoomBuildMenuPanel РІ Windows")]
    public GameObject roomBuildMenuPanel;

    [Tooltip("РљРЅРѕРїРєР° Close РІРЅСѓС‚СЂРё RoomBuildMenuPanel")]
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
            }
        }

        if (shipBuildMenuPanel == null)
        {
            shipBuildMenuPanel = FindInactiveObject("ShipBuildMenuPanel");
            if (shipBuildMenuPanel == null)
            {
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
            }
        }

        if (roomBuildMenuPanel == null)
        {
            roomBuildMenuPanel = FindInactiveObject("RoomBuildMenuPanel");
            if (roomBuildMenuPanel == null)
            {
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
            }
        }

        if (shipBuildButton == null)
        {
        }

        if (shipBuildMenuPanel == null)
        {
        }

        if (roomBuildButton == null)
        {
        }

        if (roomBuildMenuPanel == null)
        {
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
