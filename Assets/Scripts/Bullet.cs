using UnityEngine;
using System.Collections;

/// <summary>
/// РљРѕРјРїРѕРЅРµРЅС‚ СЃРЅР°СЂСЏРґР° (РїСѓР»Рё) РґР»СЏ РѕРіРЅРµСЃС‚СЂРµР»СЊРЅРѕРіРѕ РѕСЂСѓР¶РёСЏ
/// Р РµР°Р»РёР·СѓРµС‚ РїРѕР»РµС‚ СЃРЅР°СЂСЏРґРѕРІ РєР°Рє РІ RimWorld
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Properties")]
    public float damage = 20f;              // РЈСЂРѕРЅ СЃРЅР°СЂСЏРґР°
    public float speed = 50f;               // РЎРєРѕСЂРѕСЃС‚СЊ РїРѕР»РµС‚Р°
    public float maxDistance = 20f;         // РњР°РєСЃРёРјР°Р»СЊРЅР°СЏ РґРёСЃС‚Р°РЅС†РёСЏ РїРѕР»РµС‚Р°
    public float accuracy = 1f;             // РўРѕС‡РЅРѕСЃС‚СЊ (РІР»РёСЏРµС‚ РЅР° СЂР°Р·Р±СЂРѕСЃ)
    public LayerMask hitLayers = -1;        // РЎР»РѕРё СЃ РєРѕС‚РѕСЂС‹РјРё РІР·Р°РёРјРѕРґРµР№СЃС‚РІСѓРµС‚ РїСѓР»СЏ

    [Header("Visual")]
    public float bulletSize = 0.1f;         // Р Р°Р·РјРµСЂ РїСѓР»Рё
    public Color bulletColor = Color.yellow; // Р¦РІРµС‚ РїСѓР»Рё
    public bool showTrail = true;           // РџРѕРєР°Р·С‹РІР°С‚СЊ СЃР»РµРґ

    [Header("Hit Effects")]
    public bool penetrateTargets = false;   // РњРѕР¶РµС‚ Р»Рё РїСѓР»СЏ РїСЂРѕР±РёРІР°С‚СЊ С†РµР»Рё
    public int maxPenetrations = 1;         // РњР°РєСЃРёРјР°Р»СЊРЅРѕРµ РєРѕР»РёС‡РµСЃС‚РІРѕ РїСЂРѕР±РёС‚РёР№

    [Header("Friendly Fire Settings")]
    [Tooltip("РЁР°РЅСЃ РїРѕРїР°РґР°РЅРёСЏ РїРѕ СЃРѕСЋР·РЅРёРєСѓ РЅР° Р»РёРЅРёРё РѕРіРЅСЏ (0-1). Р РµРєРѕРјРµРЅРґСѓРµС‚СЃСЏ 0.2 (20%)")]
    public float friendlyFireChance = 0.2f;
    [Tooltip("РњРЅРѕР¶РёС‚РµР»СЊ СѓСЂРѕРЅР° РїСЂРё РїРѕРїР°РґР°РЅРёРё РїРѕ СЃРѕСЋР·РЅРёРєСѓ (0-1). Р РµРєРѕРјРµРЅРґСѓРµС‚СЃСЏ 0.6 (60% СѓСЂРѕРЅР°)")]
    public float friendlyFireDamageMultiplier = 0.6f;

    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 direction;
    private float traveledDistance = 0f;
    private Character shooter;
    private Renderer bulletRenderer;
    private TrailRenderer trailRenderer;
    private Rigidbody bulletRigidbody;
    private int penetrationCount = 0;
    private bool hasHitTarget = false;

    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ РїСѓР»Рё
    /// </summary>
    public void Initialize(Vector3 startPos, Vector3 targetPos, Character shooterCharacter,
                          float bulletDamage, float bulletSpeed, float bulletAccuracy)
    {
        startPosition = startPos;
        targetPosition = targetPos;
        shooter = shooterCharacter;
        damage = bulletDamage;
        speed = bulletSpeed;
        accuracy = bulletAccuracy;

        // Р Р°СЃСЃС‡РёС‚С‹РІР°РµРј РЅР°РїСЂР°РІР»РµРЅРёРµ СЃ СѓС‡РµС‚РѕРј СЂР°Р·Р±СЂРѕСЃР°
        direction = CalculateDirectionWithSpread();

        // Р Р°СЃСЃС‡РёС‚С‹РІР°РµРј РјР°РєСЃРёРјР°Р»СЊРЅСѓСЋ РґРёСЃС‚Р°РЅС†РёСЋ РїРѕР»РµС‚Р°
        traveledDistance = 0f;

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РїРѕР·РёС†РёСЋ Рё РїРѕРІРѕСЂРѕС‚
        transform.position = startPosition;
        transform.LookAt(startPosition + direction);

        // РќР°СЃС‚СЂР°РёРІР°РµРј РІРёР·СѓР°Р»
        SetupVisuals();

        // Р—Р°РїСѓСЃРєР°РµРј РїРѕР»РµС‚
        StartCoroutine(BulletFlight());
    }

    /// <summary>
    /// Р Р°СЃСЃС‡РёС‚Р°С‚СЊ РЅР°РїСЂР°РІР»РµРЅРёРµ СЃ СѓС‡РµС‚РѕРј СЂР°Р·Р±СЂРѕСЃР°
    /// </summary>
    private Vector3 CalculateDirectionWithSpread()
    {
        Vector3 baseDirection = (targetPosition - startPosition).normalized;

        // Р Р°СЃСЃС‡РёС‚С‹РІР°РµРј СЂР°Р·Р±СЂРѕСЃ РЅР° РѕСЃРЅРѕРІРµ С‚РѕС‡РЅРѕСЃС‚Рё
        float spreadAngle = (1f - accuracy) * 15f; // РњР°РєСЃРёРјР°Р»СЊРЅС‹Р№ СЂР°Р·Р±СЂРѕСЃ 15 РіСЂР°РґСѓСЃРѕРІ

        if (spreadAngle > 0f)
        {
            // Р”РѕР±Р°РІР»СЏРµРј СЃР»СѓС‡Р°Р№РЅС‹Р№ СЂР°Р·Р±СЂРѕСЃ
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spread = Quaternion.Euler(randomX, randomY, 0f);
            baseDirection = spread * baseDirection;
        }

        return baseDirection;
    }

    /// <summary>
    /// РќР°СЃС‚СЂРѕР№РєР° РІРёР·СѓР°Р»СЊРЅРѕРіРѕ РїСЂРµРґСЃС‚Р°РІР»РµРЅРёСЏ РїСѓР»Рё
    /// </summary>
    private void SetupVisuals()
    {
        // РЎРѕР·РґР°РµРј РїСЂРѕСЃС‚СѓСЋ РіРµРѕРјРµС‚СЂРёСЋ РїСѓР»Рё
        GameObject bulletMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletMesh.transform.SetParent(transform);
        bulletMesh.transform.localPosition = Vector3.zero;
        bulletMesh.transform.localScale = Vector3.one * bulletSize;

        // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂ РѕС‚ РїСЂРёРјРёС‚РёРІР° (Сѓ РЅР°СЃ РµСЃС‚СЊ СЃРІРѕР№)
        Collider meshCollider = bulletMesh.GetComponent<Collider>();
        if (meshCollider != null)
        {
            DestroyImmediate(meshCollider);
        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј РјР°С‚РµСЂРёР°Р»
        bulletRenderer = bulletMesh.GetComponent<Renderer>();
        if (bulletRenderer != null)
        {
            Material bulletMaterial = new Material(Shader.Find("Standard"));
            bulletMaterial.color = bulletColor;
            bulletMaterial.SetFloat("_Mode", 0); // Opaque
            bulletMaterial.EnableKeyword("_EMISSION");
            bulletMaterial.SetColor("_EmissionColor", bulletColor * 0.5f);
            bulletRenderer.material = bulletMaterial;
        }

        // Р”РѕР±Р°РІР»СЏРµРј СЃР»РµРґ РµСЃР»Рё РЅСѓР¶РЅРѕ
        if (showTrail)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.material = bulletRenderer.material;
            trailRenderer.startWidth = bulletSize * 0.5f;
            trailRenderer.endWidth = 0f;
            trailRenderer.time = 0.5f;
        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј РєРѕР»Р»Р°Р№РґРµСЂ РґР»СЏ РѕР±РЅР°СЂСѓР¶РµРЅРёСЏ РїРѕРїР°РґР°РЅРёР№
        SphereCollider bulletCollider = gameObject.AddComponent<SphereCollider>();
        bulletCollider.radius = bulletSize * 0.5f;
        bulletCollider.isTrigger = true;

        // Р”РѕР±Р°РІР»СЏРµРј Rigidbody РґР»СЏ С„РёР·РёРєРё
        bulletRigidbody = gameObject.AddComponent<Rigidbody>();
        bulletRigidbody.useGravity = false;
        bulletRigidbody.isKinematic = true;
    }

    /// <summary>
    /// РљРѕСЂСѓС‚РёРЅР° РїРѕР»РµС‚Р° РїСѓР»Рё
    /// </summary>
    private IEnumerator BulletFlight()
    {
        while (traveledDistance < maxDistance && !hasHitTarget)
        {
            float deltaTime = Time.deltaTime;
            float distanceThisFrame = speed * deltaTime;

            // РџСЂРѕРІРµСЂСЏРµРј РїРѕРїР°РґР°РЅРёСЏ СЃ РїРѕРјРѕС‰СЊСЋ raycast, РёРіРЅРѕСЂРёСЂСѓСЏ С‚СЂРёРіРіРµСЂС‹ (РїСЂРµРґРјРµС‚С‹ РЅР° Р·РµРјР»Рµ)
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, distanceThisFrame, hitLayers, QueryTriggerInteraction.Ignore))
            {
                // РџРµСЂРµРјРµС‰Р°РµРј РїСѓР»СЋ РІ С‚РѕС‡РєСѓ РїРѕРїР°РґР°РЅРёСЏ
                transform.position = hit.point;

                // РћР±СЂР°Р±Р°С‚С‹РІР°РµРј РїРѕРїР°РґР°РЅРёРµ
                ProcessHit(hit);

                // РџСЂРѕРІРµСЂСЏРµРј, РЅСѓР¶РЅРѕ Р»Рё РїСЂРѕРґРѕР»Р¶РёС‚СЊ РїРѕР»РµС‚ (РїСЂРѕР±РёС‚РёРµ)
                if (!penetrateTargets || penetrationCount >= maxPenetrations)
                {
                    break;
                }
            }
            else
            {
                // РџСЂРѕРґРѕР»Р¶Р°РµРј РїРѕР»РµС‚
                transform.position += direction * distanceThisFrame;
                traveledDistance += distanceThisFrame;
            }

            yield return null;
        }

        // РЈРЅРёС‡С‚РѕР¶Р°РµРј РїСѓР»СЋ РїРѕСЃР»Рµ Р·Р°РІРµСЂС€РµРЅРёСЏ РїРѕР»РµС‚Р°
        StartCoroutine(DestroyBulletWithDelay(0.5f));
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРѕРїР°РґР°РЅРёСЏ РїСѓР»Рё
    /// </summary>
    private void ProcessHit(RaycastHit hit)
    {
        GameObject hitObject = hit.collider.gameObject;

        // РџСЂРѕРІРµСЂСЏРµРј, РїРѕРїР°Р»Рё Р»Рё РІ РїРµСЂСЃРѕРЅР°Р¶Р°
        Character hitCharacter = hitObject.GetComponent<Character>();
        if (hitCharacter == null)
        {
            hitCharacter = hitObject.GetComponentInParent<Character>();
        }

        if (hitCharacter != null && hitCharacter != shooter)
        {
            // РџРѕРїР°Р»Рё РІ РїРµСЂСЃРѕРЅР°Р¶Р°
            ProcessCharacterHit(hitCharacter, hit);
        }
        else
        {
            // РџРѕРїР°Р»Рё РІ РїСЂРµРїСЏС‚СЃС‚РІРёРµ РёР»Рё РґСЂСѓРіРѕР№ РѕР±СЉРµРєС‚
            ProcessObstacleHit(hit);
        }

        penetrationCount++;
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРѕРїР°РґР°РЅРёСЏ РІ РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    private void ProcessCharacterHit(Character target, RaycastHit hit)
    {
        // РџСЂРѕРІРµСЂСЏРµРј friendly fire (РїРѕРїР°РґР°РЅРёРµ РїРѕ СЃРѕСЋР·РЅРёРєСѓ)
        bool isFriendlyFire = false;
        if (shooter != null && target != null)
        {
            bool shooterIsPlayer = shooter.IsPlayerCharacter();
            bool targetIsPlayer = target.IsPlayerCharacter();

            // Friendly fire: РѕР±Р° РёРіСЂРѕРєРё РёР»Рё РѕР±Р° РІСЂР°РіРё
            if (shooterIsPlayer == targetIsPlayer)
            {
                isFriendlyFire = true;
            }
        }

        // Р•СЃР»Рё СЌС‚Рѕ friendly fire - РїСЂРѕРІРµСЂСЏРµРј С€Р°РЅСЃ РїРѕРїР°РґР°РЅРёСЏ
        if (isFriendlyFire)
        {
            float hitRoll = Random.value; // РЎР»СѓС‡Р°Р№РЅРѕРµ С‡РёСЃР»Рѕ РѕС‚ 0 РґРѕ 1

            if (hitRoll > friendlyFireChance)
            {
                // РќР• РџРћРџРђР›Р РІ СЃРѕСЋР·РЅРёРєР° (80% СЃР»СѓС‡Р°РµРІ РїСЂРё Р±Р°Р·РѕРІРѕРј С€Р°РЅСЃРµ 0.2)
                // РџСѓР»СЏ РїСЂРѕС…РѕРґРёС‚ СЃРєРІРѕР·СЊ Рё РїСЂРѕРґРѕР»Р¶Р°РµС‚ Р»РµС‚РµС‚СЊ Рє С†РµР»Рё

                // РќР• СѓСЃС‚Р°РЅР°РІР»РёРІР°РµРј hasHitTarget = true, С‡С‚РѕР±С‹ РїСѓР»СЏ РїСЂРѕРґРѕР»Р¶РёР»Р° РїРѕР»РµС‚
                // РќР• РЅР°РЅРѕСЃРёРј СѓСЂРѕРЅ
                // РќР• РїРѕРєР°Р·С‹РІР°РµРј СЌС„С„РµРєС‚ РїРѕРїР°РґР°РЅРёСЏ
                return; // Р’С‹С…РѕРґРёРј РёР· РјРµС‚РѕРґР°, РїСѓР»СЏ РїСЂРѕРґРѕР»Р¶Р°РµС‚ Р»РµС‚РµС‚СЊ
            }

            // РџРћРџРђР›Р РІ СЃРѕСЋР·РЅРёРєР° (20% СЃР»СѓС‡Р°РµРІ РїСЂРё Р±Р°Р·РѕРІРѕРј С€Р°РЅСЃРµ 0.2)
            float reducedDamage = damage * friendlyFireDamageMultiplier;

            // РќР°РЅРѕСЃРёРј СѓРјРµРЅСЊС€РµРЅРЅС‹Р№ СѓСЂРѕРЅ Рё РїРµСЂРµРґР°РµРј СЃС‚СЂРµР»РєР° РґР»СЏ СЃРёСЃС‚РµРјС‹ РєРѕРЅС‚СЂР°С‚Р°РєРё
            target.TakeDamage(reducedDamage, shooter);

            // РџРѕРєР°Р·С‹РІР°РµРј СЌС„С„РµРєС‚ РїРѕРїР°РґР°РЅРёСЏ СЃ СѓРјРµРЅСЊС€РµРЅРЅС‹Рј СѓСЂРѕРЅРѕРј
            ShowHitEffect(hit.point, target, isFriendlyFire, reducedDamage);
        }
        else
        {
            // РџРѕРїР°РґР°РЅРёРµ РІРѕ РІСЂР°РіР° - РїРѕР»РЅС‹Р№ СѓСЂРѕРЅ Рё РїРµСЂРµРґР°РµРј СЃС‚СЂРµР»РєР° РґР»СЏ СЃРёСЃС‚РµРјС‹ РєРѕРЅС‚СЂР°С‚Р°РєРё
            target.TakeDamage(damage, shooter);

            // РџРѕРєР°Р·С‹РІР°РµРј СЌС„С„РµРєС‚ РїРѕРїР°РґР°РЅРёСЏ
            ShowHitEffect(hit.point, target, isFriendlyFire, damage);
        }

        hasHitTarget = true;
    }

    /// <summary>
    /// РћР±СЂР°Р±РѕС‚РєР° РїРѕРїР°РґР°РЅРёСЏ РІ РїСЂРµРїСЏС‚СЃС‚РІРёРµ
    /// </summary>
    private void ProcessObstacleHit(RaycastHit hit)
    {
        // РџРѕРєР°Р·С‹РІР°РµРј СЌС„С„РµРєС‚ РїРѕРїР°РґР°РЅРёСЏ РІ РїСЂРµРїСЏС‚СЃС‚РІРёРµ
        ShowHitEffect(hit.point, null, false);

        // РџСѓР»СЏ РѕСЃС‚Р°РЅР°РІР»РёРІР°РµС‚СЃСЏ РїСЂРё РїРѕРїР°РґР°РЅРёРё РІ РїСЂРµРїСЏС‚СЃС‚РІРёРµ
        if (!penetrateTargets)
        {
            hasHitTarget = true;
        }
    }

    /// <summary>
    /// РџРѕРєР°Р·Р°С‚СЊ СЌС„С„РµРєС‚ РїРѕРїР°РґР°РЅРёСЏ
    /// </summary>
    private void ShowHitEffect(Vector3 hitPoint, Character hitTarget, bool isFriendlyFire = false, float actualDamage = 0f)
    {
        // РЎРѕР·РґР°РµРј РїСЂРѕСЃС‚РѕР№ СЌС„С„РµРєС‚ РїРѕРїР°РґР°РЅРёСЏ
        GameObject hitEffect = new GameObject("BulletHitEffect");
        hitEffect.transform.position = hitPoint;

        // Р•СЃР»Рё РїРѕРїР°Р»Рё РІ РїРµСЂСЃРѕРЅР°Р¶Р°, РїРѕРєР°Р·С‹РІР°РµРј СѓСЂРѕРЅ
        if (hitTarget != null)
        {
            // РСЃРїРѕР»СЊР·СѓРµРј actualDamage РµСЃР»Рё Р·Р°РґР°РЅ, РёРЅР°С‡Рµ Р±Р°Р·РѕРІС‹Р№ damage
            float displayDamage = actualDamage > 0f ? actualDamage : damage;

            // РЎРѕР·РґР°РµРј С‚РµРєСЃС‚ СѓСЂРѕРЅР°
            GameObject damageTextObj = new GameObject("BulletDamageText");
            damageTextObj.transform.position = hitPoint + Vector3.up * 1.8f;

            TextMesh damageText = damageTextObj.AddComponent<TextMesh>();
            damageText.text = $"-{displayDamage:F0}";
            damageText.fontSize = 8;
            damageText.color = isFriendlyFire ? new Color(1f, 0.8f, 0f) : Color.white; // РћСЂР°РЅР¶РµРІС‹Р№ РґР»СЏ FF
            damageText.anchor = TextAnchor.MiddleCenter;

            // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ РїРѕРІРѕСЂРѕС‚Р° Рє РєР°РјРµСЂРµ
            damageTextObj.AddComponent<LookAtCamera>();

            // РђРЅРёРјР°С†РёСЏ С‚РµРєСЃС‚Р° СѓСЂРѕРЅР° - РЎРўР РћР“Рћ 1 СЃРµРєСѓРЅРґР° С‡РµСЂРµР· DamageTextManager С‡С‚РѕР±С‹ РёР·Р±РµР¶Р°С‚СЊ РїСЂРµСЂС‹РІР°РЅРёСЏ РїСЂРё СѓРЅРёС‡С‚РѕР¶РµРЅРёРё РїСѓР»Рё
            DamageTextManager.Instance.StartDamageTextAnimation(damageTextObj, 1.0f);
        }

        // Р’РёР·СѓР°Р»СЊРЅС‹Р№ СЌС„С„РµРєС‚ (РјРѕР¶РЅРѕ Р·Р°РјРµРЅРёС‚СЊ РЅР° РїР°СЂС‚РёРєР»С‹)
        GameObject effectSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effectSphere.transform.SetParent(hitEffect.transform);
        effectSphere.transform.localPosition = Vector3.zero;
        effectSphere.transform.localScale = Vector3.one * 0.3f;

        // РЈРґР°Р»СЏРµРј РєРѕР»Р»Р°Р№РґРµСЂ
        Collider effectCollider = effectSphere.GetComponent<Collider>();
        if (effectCollider != null)
        {
            DestroyImmediate(effectCollider);
        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј РјР°С‚РµСЂРёР°Р» СЌС„С„РµРєС‚Р°
        Renderer effectRenderer = effectSphere.GetComponent<Renderer>();
        if (effectRenderer != null)
        {
            Material effectMaterial = new Material(Shader.Find("Standard"));

            // Р Р°Р·РЅС‹Р№ С†РІРµС‚ РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ С‚РёРїР° РїРѕРїР°РґР°РЅРёСЏ
            if (hitTarget != null)
            {
                effectMaterial.color = isFriendlyFire ? Color.yellow : Color.red; // Р–РµР»С‚С‹Р№ РґР»СЏ friendly fire
            }
            else
            {
                effectMaterial.color = Color.gray; // РЎРµСЂС‹Р№ РґР»СЏ РїСЂРµРїСЏС‚СЃС‚РІРёР№
            }

            effectMaterial.SetFloat("_Mode", 0);
            effectMaterial.EnableKeyword("_EMISSION");
            effectMaterial.SetColor("_EmissionColor", effectMaterial.color);
            effectRenderer.material = effectMaterial;
        }

        // РђРЅРёРјР°С†РёСЏ СЌС„С„РµРєС‚Р°
        StartCoroutine(AnimateHitEffect(hitEffect));
    }

    /// <summary>
    /// РђРЅРёРјР°С†РёСЏ СЌС„С„РµРєС‚Р° РїРѕРїР°РґР°РЅРёСЏ
    /// </summary>
    private IEnumerator AnimateHitEffect(GameObject effect)
    {
        float duration = 0.5f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 endScale = startScale * 2f;

        Renderer effectRenderer = effect.GetComponentInChildren<Renderer>();
        Material effectMaterial = effectRenderer?.material;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // РЈРІРµР»РёС‡РёРІР°РµРј СЂР°Р·РјРµСЂ
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            // РЈРјРµРЅСЊС€Р°РµРј РїСЂРѕР·СЂР°С‡РЅРѕСЃС‚СЊ
            if (effectMaterial != null)
            {
                Color color = effectMaterial.color;
                color.a = 1f - t;
                effectMaterial.color = color;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(effect);
    }


    /// <summary>
    /// РЈРЅРёС‡С‚РѕР¶РёС‚СЊ РїСѓР»СЋ СЃ Р·Р°РґРµСЂР¶РєРѕР№
    /// </summary>
    private IEnumerator DestroyBulletWithDelay(float delay)
    {
        // РћС‚РєР»СЋС‡Р°РµРј РєРѕР»Р»Р°Р№РґРµСЂ С‡С‚РѕР±С‹ РёР·Р±РµР¶Р°С‚СЊ РґРѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹С… РїРѕРїР°РґР°РЅРёР№
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }

        yield return new WaitForSeconds(delay);

        Destroy(gameObject);
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃС‚СЂРµР»РєР°
    /// </summary>
    public Character GetShooter()
    {
        return shooter;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ РїСЂРѕР№РґРµРЅРЅСѓСЋ РґРёСЃС‚Р°РЅС†РёСЋ
    /// </summary>
    public float GetTraveledDistance()
    {
        return traveledDistance;
    }

    void OnDrawGizmos()
    {
        // РџРѕРєР°Р·С‹РІР°РµРј С‚СЂР°РµРєС‚РѕСЂРёСЋ РїРѕР»РµС‚Р° РІ Scene view
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPosition, transform.position);

            // РџРѕРєР°Р·С‹РІР°РµРј РЅР°РїСЂР°РІР»РµРЅРёРµ
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}
