using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Р Р°Р·РјРµС‰Р°РµС‚ 2 РїСЂРµС„Р°Р±Р° РІ ShipBuildMenuPanel: BuildSlot Рё Del_BuildSlot
/// Р”РѕР±Р°РІР»СЏРµС‚ РєРЅРѕРїРєСѓ AddBuild РґР»СЏ РїРѕРґС‚РІРµСЂР¶РґРµРЅРёСЏ РїРѕСЃС‚СЂРѕР№РєРё
/// Р’РђР–РќРћ: Р­С‚РѕС‚ СЃРєСЂРёРїС‚ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїСЂРёРєСЂРµРїР»РµРЅ РўРћР›Р¬РљРћ Рє ShipBuildMenuPanel!
/// Р”Р»СЏ RoomBuildMenuPanel РёСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ РѕС‚РґРµР»СЊРЅР°СЏ Р»РѕРіРёРєР° СЃ РґСЂСѓРіРёРјРё РєРЅРѕРїРєР°РјРё.
/// </summary>
public class BuildMenuPopulator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("РџСЂРµС„Р°Р± BuildSlot")]
    public GameObject buildSlotPrefab;

    [Tooltip("РџСЂРµС„Р°Р± Del_BuildSlot")]
    public GameObject delBuildSlotPrefab;

    [Header("References")]
    [Tooltip("RoomDragBuilder РєРѕРјРїРѕРЅРµРЅС‚")]
    public RoomDragBuilder roomDragBuilder;

    [Header("UI")]
    [Tooltip("РљРЅРѕРїРєР° AddBuild РґР»СЏ РїРѕРґС‚РІРµСЂР¶РґРµРЅРёСЏ РїРѕСЃС‚СЂРѕР№РєРё")]
    public Button addBuildButton;

    private GameObject buildSlotInstance;
    private GameObject delBuildSlotInstance;
    private bool isInitialized = false;

    void Awake()
    {
        if (roomDragBuilder == null)
        {
            roomDragBuilder = FindObjectOfType<RoomDragBuilder>();
            if (roomDragBuilder == null)
            {
                GameObject rbObj = new GameObject("RoomDragBuilder");
                roomDragBuilder = rbObj.AddComponent<RoomDragBuilder>();
            }
        }
    }

    void Start()
    {
        if (gameObject.name != "ShipBuildMenuPanel")
        {
            return;
        }

        Transform contentTransform = transform.Find("ObjectsGrid/Viewport/Content");
        if (contentTransform == null)
        {
            return;
        }

        #if UNITY_EDITOR
        if (buildSlotPrefab == null)
        {
            buildSlotPrefab = LoadPrefabByName("BuildSlot", "Assets/Prefabs/UI/");
        }

        if (delBuildSlotPrefab == null)
        {
            delBuildSlotPrefab = LoadPrefabByName("Del_BuildSlot", "Assets/Prefabs/UI/Buildings/");
        }
        #endif

        if (buildSlotPrefab == null)
        {
            return;
        }

        if (delBuildSlotPrefab == null)
        {
            return;
        }

        buildSlotInstance = Instantiate(buildSlotPrefab, contentTransform);
        buildSlotInstance.name = "BuildSlot";

        Button buildSlotButton = buildSlotInstance.GetComponentInChildren<Button>(true);
        if (buildSlotButton != null)
        {
            buildSlotButton.onClick.AddListener(OnBuildSlotClicked);
        }
        else
        {
        }

        delBuildSlotInstance = Instantiate(delBuildSlotPrefab, contentTransform);
        delBuildSlotInstance.name = "Del_BuildSlot";

        Button delBuildSlotButton = delBuildSlotInstance.GetComponentInChildren<Button>(true);
        if (delBuildSlotButton != null)
        {
            delBuildSlotButton.onClick.AddListener(OnDelBuildSlotClicked);
        }
        else
        {
        }

        SetupAddBuildButton();
        isInitialized = true;
    }

    void SetupAddBuildButton()
    {
        if (addBuildButton == null)
        {
            GameObject canvasMainUI = GameObject.Find("Canvas_MainUI");
            if (canvasMainUI != null)
            {
                Button[] allButtons = canvasMainUI.GetComponentsInChildren<Button>(true);
                foreach (Button btn in allButtons)
                {
                    if (btn.gameObject.name == "AddBuild")
                    {
                        addBuildButton = btn;
                        break;
                    }
                }
            }

            if (addBuildButton == null)
            {
            }
        }

        if (addBuildButton != null)
        {
            addBuildButton.onClick.AddListener(OnAddBuildClicked);
            addBuildButton.gameObject.SetActive(true);
            addBuildButton.interactable = false;
        }
    }

    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    void OnBuildSlotClicked()
    {
        if (roomDragBuilder == null)
        {
            return;
        }

        roomDragBuilder.ActivateDragMode();
    }

    void OnAddBuildClicked()
    {
        // РўРµРїРµСЂСЊ РІС‹Р·С‹РІР°РµРј ConfirmBuild() РІРјРµСЃС‚Рѕ FinalizeBuild()
        // ConfirmBuild() РїСЂРёРјРµРЅСЏРµС‚ РјР°С‚РµСЂРёР°Р» M_Add_Build_Ghost Рё Р·Р°РїСѓСЃРєР°РµС‚ СЃРёСЃС‚РµРјСѓ СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° РїРµСЂСЃРѕРЅР°Р¶Р°РјРё
        roomDragBuilder.ConfirmBuild();
    }

    void OnDelBuildSlotClicked()
    {
        if (roomDragBuilder == null)
        {
            return;
        }

        if (!roomDragBuilder.IsDeletionModeActive())
        {
            roomDragBuilder.DeactivateDragMode();
        }

        roomDragBuilder.ActivateDeletionMode();
    }

    void Update()
    {
        if (!isInitialized) return;

        if (addBuildButton != null && roomDragBuilder != null)
        {
            bool shouldBeInteractable = roomDragBuilder.CanConfirmBuild();
            if (addBuildButton.interactable != shouldBeInteractable)
            {
                addBuildButton.interactable = shouldBeInteractable;
            }
        }
    }

    void OnDisable()
    {
        if (!isInitialized) return;

        if (roomDragBuilder != null)
        {
            roomDragBuilder.DeactivateDragMode();
            roomDragBuilder.DeactivateDeletionMode();
        }
    }

    #if UNITY_EDITOR
    GameObject LoadPrefabByName(string prefabName, string searchPath)
    {
        string[] guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { searchPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && prefab.name == prefabName)
            {
                return prefab;
            }
        }

        return null;
    }
    #endif
}
