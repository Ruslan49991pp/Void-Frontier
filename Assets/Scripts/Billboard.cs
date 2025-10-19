using UnityEngine;

/// <summary>
/// РљРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ РїРѕРІРѕСЂРѕС‚Р° РѕР±СЉРµРєС‚Р° Р»РёС†РѕРј Рє РєР°РјРµСЂРµ (billboard СЌС„С„РµРєС‚)
/// РСЃРїРѕР»СЊР·СѓРµС‚СЃСЏ РґР»СЏ РїРѕР»РѕСЃ РїСЂРѕРіСЂРµСЃСЃР° СЃС‚СЂРѕРёС‚РµР»СЊСЃС‚РІР°
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // РќР°С…РѕРґРёРј РѕСЃРЅРѕРІРЅСѓСЋ РєР°РјРµСЂСѓ
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            // РџС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё РєР°РјРµСЂСѓ СЃРЅРѕРІР° РµСЃР»Рё РѕРЅР° Р±С‹Р»Р° null
            mainCamera = Camera.main;
            if (mainCamera == null)
                return;
        }

        // РџРѕРІРѕСЂР°С‡РёРІР°РµРј РѕР±СЉРµРєС‚ Р»РёС†РѕРј Рє РєР°РјРµСЂРµ
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                        mainCamera.transform.rotation * Vector3.up);
    }
}
