using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// РљРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ РїРѕСЂС‚СЂРµС‚Р° РїРµСЂСЃРѕРЅР°Р¶Р° - СЃР»РµРґРѕРІР°РЅРёРµ РєР°РјРµСЂС‹ РїСЂРё СѓРґРµСЂР¶Р°РЅРёРё Р›РљРњ
/// </summary>
public class PortraitCameraFollow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Character character;
    private CameraController cameraController;
    private Coroutine followCoroutine;

    [Tooltip("Р’СЂРµРјСЏ СѓРґРµСЂР¶Р°РЅРёСЏ РєРЅРѕРїРєРё (РІ СЃРµРєСѓРЅРґР°С…) РїРµСЂРµРґ РЅР°С‡Р°Р»РѕРј СЃР»РµРґРѕРІР°РЅРёСЏ")]
    public float holdThreshold = 0.1f;

    public void Initialize(Character character)
    {
        this.character = character;
    }

    void Start()
    {
        // РќР°С…РѕРґРёРј CameraController
        cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null)
        {
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // РџСЂРё РЅР°Р¶Р°С‚РёРё Р›РљРњ Р·Р°РїСѓСЃРєР°РµРј РєРѕСЂСѓС‚РёРЅСѓ СЃ Р·Р°РґРµСЂР¶РєРѕР№
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (followCoroutine != null)
            {
                StopCoroutine(followCoroutine);
            }
            followCoroutine = StartCoroutine(StartFollowingAfterDelay());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // РџСЂРё РѕС‚РїСѓСЃРєР°РЅРёРё Р›РљРњ РѕСЃС‚Р°РЅР°РІР»РёРІР°РµРј РєРѕСЂСѓС‚РёРЅСѓ Рё СЃР»РµРґРѕРІР°РЅРёРµ
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // РћС‚РјРµРЅСЏРµРј Р·Р°РїСѓСЃРє СЃР»РµРґРѕРІР°РЅРёСЏ РµСЃР»Рё РєРЅРѕРїРєР° РѕС‚РїСѓС‰РµРЅР° РґРѕ РёСЃС‚РµС‡РµРЅРёСЏ Р·Р°РґРµСЂР¶РєРё
            if (followCoroutine != null)
            {
                StopCoroutine(followCoroutine);
                followCoroutine = null;
            }

            // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЃР»РµРґРѕРІР°РЅРёРµ РµСЃР»Рё РѕРЅРѕ Р±С‹Р»Рѕ Р°РєС‚РёРІРЅРѕ
            if (cameraController != null)
            {
                cameraController.StopFollowingTarget();
            }
        }
    }

    /// <summary>
    /// РљРѕСЂСѓС‚РёРЅР° РґР»СЏ Р·Р°РїСѓСЃРєР° СЃР»РµРґРѕРІР°РЅРёСЏ СЃ Р·Р°РґРµСЂР¶РєРѕР№
    /// </summary>
    IEnumerator StartFollowingAfterDelay()
    {
        // Р–РґРµРј СѓРєР°Р·Р°РЅРЅРѕРµ РІСЂРµРјСЏ
        yield return new WaitForSeconds(holdThreshold);

        // Р•СЃР»Рё РґРѕС€Р»Рё СЃСЋРґР°, Р·РЅР°С‡РёС‚ РєРЅРѕРїРєР° СѓРґРµСЂР¶РёРІР°РµС‚СЃСЏ РґРѕСЃС‚Р°С‚РѕС‡РЅРѕ РґРѕР»РіРѕ
        if (character != null && cameraController != null)
        {
            cameraController.StartFollowingTarget(character.transform);
        }

        followCoroutine = null;
    }

    void OnDisable()
    {
        // РћСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЃР»РµРґРѕРІР°РЅРёРµ РїСЂРё РѕС‚РєР»СЋС‡РµРЅРёРё РєРѕРјРїРѕРЅРµРЅС‚Р°
        if (cameraController != null)
        {
            cameraController.StopFollowingTarget();
        }

        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
    }
}
