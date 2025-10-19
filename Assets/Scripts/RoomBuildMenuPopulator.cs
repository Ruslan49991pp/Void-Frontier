using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Р Р°Р·РјРµС‰Р°РµС‚ 2 РїСЂРµС„Р°Р±Р° РІ RoomBuildMenuPanel: Wall_Int_Slot Рё Door_Slot
/// Р’РђР–РќРћ: Р­С‚РѕС‚ СЃРєСЂРёРїС‚ РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїСЂРёРєСЂРµРїР»РµРЅ РўРћР›Р¬РљРћ Рє RoomBuildMenuPanel!
/// </summary>
public class RoomBuildMenuPopulator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("РџСЂРµС„Р°Р± Wall_Int_Slot")]
    public GameObject wallIntSlotPrefab;

    [Tooltip("РџСЂРµС„Р°Р± Door_Slot")]
    public GameObject doorSlotPrefab;

    private GameObject wallIntSlotInstance;
    private GameObject doorSlotInstance;

    void Start()
    {
        if (gameObject.name != "RoomBuildMenuPanel")
        {
            return;
        }

        Transform contentTransform = transform.Find("ObjectsGrid/Viewport/Content");
        if (contentTransform == null)
        {
            return;
        }

        #if UNITY_EDITOR
        if (wallIntSlotPrefab == null)
        {
            wallIntSlotPrefab = LoadPrefabByName("Wall_Int_Slot", "Assets/Prefabs/UI/Interior/");
        }

        if (doorSlotPrefab == null)
        {
            doorSlotPrefab = LoadPrefabByName("Door_Slot", "Assets/Prefabs/UI/Interior/");
        }
        #endif

        if (wallIntSlotPrefab == null)
        {
            return;
        }

        if (doorSlotPrefab == null)
        {
            return;
        }

        wallIntSlotInstance = Instantiate(wallIntSlotPrefab, contentTransform);
        wallIntSlotInstance.name = "Wall_Int_Slot";

        Button wallIntSlotButton = wallIntSlotInstance.GetComponentInChildren<Button>(true);
        if (wallIntSlotButton != null)
        {
            wallIntSlotButton.onClick.AddListener(OnWallIntSlotClicked);
        }
        else
        {
        }

        doorSlotInstance = Instantiate(doorSlotPrefab, contentTransform);
        doorSlotInstance.name = "Door_Slot";

        Button doorSlotButton = doorSlotInstance.GetComponentInChildren<Button>(true);
        if (doorSlotButton != null)
        {
            doorSlotButton.onClick.AddListener(OnDoorSlotClicked);
        }
        else
        {
        }
    }

    void OnWallIntSlotClicked()
    {


        // РђРєС‚РёРІРёСЂСѓРµРј СЂРµР¶РёРј СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР° РІРЅСѓС‚СЂРµРЅРЅРёС… СЃС‚РµРЅ (РЅРѕРІР°СЏ СЃРёСЃС‚РµРјР° СЃ drag)
        if (InteriorWallDragBuilder.Instance != null)
        {
            InteriorWallDragBuilder.Instance.ActivateDragMode();
        }
        else
        {
        }
    }

    void OnDoorSlotClicked()
    {

        // TODO: Р”РѕР±Р°РІРёС‚СЊ Р»РѕРіРёРєСѓ РґР»СЏ СѓСЃС‚Р°РЅРѕРІРєРё РґРІРµСЂРµР№
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
