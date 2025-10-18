using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент снаряда (пули) для огнестрельного оружия
/// Реализует полет снарядов как в RimWorld
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Bullet Properties")]
    public float damage = 20f;              // Урон снаряда
    public float speed = 50f;               // Скорость полета
    public float maxDistance = 20f;         // Максимальная дистанция полета
    public float accuracy = 1f;             // Точность (влияет на разброс)
    public LayerMask hitLayers = -1;        // Слои с которыми взаимодействует пуля

    [Header("Visual")]
    public float bulletSize = 0.1f;         // Размер пули
    public Color bulletColor = Color.yellow; // Цвет пули
    public bool showTrail = true;           // Показывать след

    [Header("Hit Effects")]
    public bool penetrateTargets = false;   // Может ли пуля пробивать цели
    public int maxPenetrations = 1;         // Максимальное количество пробитий

    // Внутренние переменные
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
    /// Инициализация пули
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

        // Рассчитываем направление с учетом разброса
        direction = CalculateDirectionWithSpread();

        // Рассчитываем максимальную дистанцию полета
        traveledDistance = 0f;

        // Устанавливаем позицию и поворот
        transform.position = startPosition;
        transform.LookAt(startPosition + direction);

        // Настраиваем визуал
        SetupVisuals();

        // Запускаем полет
        StartCoroutine(BulletFlight());
    }

    /// <summary>
    /// Рассчитать направление с учетом разброса
    /// </summary>
    private Vector3 CalculateDirectionWithSpread()
    {
        Vector3 baseDirection = (targetPosition - startPosition).normalized;

        // Рассчитываем разброс на основе точности
        float spreadAngle = (1f - accuracy) * 15f; // Максимальный разброс 15 градусов

        if (spreadAngle > 0f)
        {
            // Добавляем случайный разброс
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spread = Quaternion.Euler(randomX, randomY, 0f);
            baseDirection = spread * baseDirection;
        }

        return baseDirection;
    }

    /// <summary>
    /// Настройка визуального представления пули
    /// </summary>
    private void SetupVisuals()
    {
        // Создаем простую геометрию пули
        GameObject bulletMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletMesh.transform.SetParent(transform);
        bulletMesh.transform.localPosition = Vector3.zero;
        bulletMesh.transform.localScale = Vector3.one * bulletSize;

        // Убираем коллайдер от примитива (у нас есть свой)
        Collider meshCollider = bulletMesh.GetComponent<Collider>();
        if (meshCollider != null)
        {
            DestroyImmediate(meshCollider);
        }

        // Настраиваем материал
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

        // Добавляем след если нужно
        if (showTrail)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.material = bulletRenderer.material;
            trailRenderer.startWidth = bulletSize * 0.5f;
            trailRenderer.endWidth = 0f;
            trailRenderer.time = 0.5f;
        }

        // Настраиваем коллайдер для обнаружения попаданий
        SphereCollider bulletCollider = gameObject.AddComponent<SphereCollider>();
        bulletCollider.radius = bulletSize * 0.5f;
        bulletCollider.isTrigger = true;

        // Добавляем Rigidbody для физики
        bulletRigidbody = gameObject.AddComponent<Rigidbody>();
        bulletRigidbody.useGravity = false;
        bulletRigidbody.isKinematic = true;
    }

    /// <summary>
    /// Корутина полета пули
    /// </summary>
    private IEnumerator BulletFlight()
    {
        while (traveledDistance < maxDistance && !hasHitTarget)
        {
            float deltaTime = Time.deltaTime;
            float distanceThisFrame = speed * deltaTime;

            // Проверяем попадания с помощью raycast, игнорируя триггеры (предметы на земле)
            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, distanceThisFrame, hitLayers, QueryTriggerInteraction.Ignore))
            {
                // Перемещаем пулю в точку попадания
                transform.position = hit.point;

                // Обрабатываем попадание
                ProcessHit(hit);

                // Проверяем, нужно ли продолжить полет (пробитие)
                if (!penetrateTargets || penetrationCount >= maxPenetrations)
                {
                    break;
                }
            }
            else
            {
                // Продолжаем полет
                transform.position += direction * distanceThisFrame;
                traveledDistance += distanceThisFrame;
            }

            yield return null;
        }

        // Уничтожаем пулю после завершения полета
        StartCoroutine(DestroyBulletWithDelay(0.5f));
    }

    /// <summary>
    /// Обработка попадания пули
    /// </summary>
    private void ProcessHit(RaycastHit hit)
    {
        GameObject hitObject = hit.collider.gameObject;

        // Проверяем, попали ли в персонажа
        Character hitCharacter = hitObject.GetComponent<Character>();
        if (hitCharacter == null)
        {
            hitCharacter = hitObject.GetComponentInParent<Character>();
        }

        if (hitCharacter != null && hitCharacter != shooter)
        {
            // Попали в персонажа
            ProcessCharacterHit(hitCharacter, hit);
        }
        else
        {
            // Попали в препятствие или другой объект
            ProcessObstacleHit(hit);
        }

        penetrationCount++;
    }

    /// <summary>
    /// Обработка попадания в персонажа
    /// </summary>
    private void ProcessCharacterHit(Character target, RaycastHit hit)
    {
        // Проверяем friendly fire (попадание по союзнику)
        bool isFriendlyFire = false;
        if (shooter != null && target != null)
        {
            bool shooterIsPlayer = shooter.IsPlayerCharacter();
            bool targetIsPlayer = target.IsPlayerCharacter();

            // Friendly fire: оба игроки или оба враги
            if (shooterIsPlayer == targetIsPlayer)
            {
                isFriendlyFire = true;
                Debug.LogWarning($"[FRIENDLY FIRE] {shooter.GetFullName()} accidentally hit ally {target.GetFullName()} for {damage:F0} damage!");
            }
        }

        // Наносим урон (независимо от фракции - friendly fire работает!)
        target.TakeDamage(damage);

        // Показываем эффект попадания (разный цвет для friendly fire)
        ShowHitEffect(hit.point, target, isFriendlyFire);

        hasHitTarget = true;
    }

    /// <summary>
    /// Обработка попадания в препятствие
    /// </summary>
    private void ProcessObstacleHit(RaycastHit hit)
    {
        // Показываем эффект попадания в препятствие
        ShowHitEffect(hit.point, null, false);

        // Пуля останавливается при попадании в препятствие
        if (!penetrateTargets)
        {
            hasHitTarget = true;
        }
    }

    /// <summary>
    /// Показать эффект попадания
    /// </summary>
    private void ShowHitEffect(Vector3 hitPoint, Character hitTarget, bool isFriendlyFire = false)
    {
        // Создаем простой эффект попадания
        GameObject hitEffect = new GameObject("BulletHitEffect");
        hitEffect.transform.position = hitPoint;

        // Если попали в персонажа, показываем урон
        if (hitTarget != null)
        {
            // Создаем текст урона
            GameObject damageTextObj = new GameObject("BulletDamageText");
            damageTextObj.transform.position = hitPoint + Vector3.up * 1.8f;

            TextMesh damageText = damageTextObj.AddComponent<TextMesh>();
            damageText.text = $"-{damage:F0}";
            damageText.fontSize = 8;
            damageText.color = Color.white;
            damageText.anchor = TextAnchor.MiddleCenter;

            // Добавляем компонент для поворота к камере
            damageTextObj.AddComponent<LookAtCamera>();

            // Анимация текста урона - СТРОГО 1 секунда через DamageTextManager чтобы избежать прерывания при уничтожении пули
            DamageTextManager.Instance.StartDamageTextAnimation(damageTextObj, 1.0f);
        }

        // Визуальный эффект (можно заменить на партиклы)
        GameObject effectSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effectSphere.transform.SetParent(hitEffect.transform);
        effectSphere.transform.localPosition = Vector3.zero;
        effectSphere.transform.localScale = Vector3.one * 0.3f;

        // Удаляем коллайдер
        Collider effectCollider = effectSphere.GetComponent<Collider>();
        if (effectCollider != null)
        {
            DestroyImmediate(effectCollider);
        }

        // Настраиваем материал эффекта
        Renderer effectRenderer = effectSphere.GetComponent<Renderer>();
        if (effectRenderer != null)
        {
            Material effectMaterial = new Material(Shader.Find("Standard"));

            // Разный цвет в зависимости от типа попадания
            if (hitTarget != null)
            {
                effectMaterial.color = isFriendlyFire ? Color.yellow : Color.red; // Желтый для friendly fire
            }
            else
            {
                effectMaterial.color = Color.gray; // Серый для препятствий
            }

            effectMaterial.SetFloat("_Mode", 0);
            effectMaterial.EnableKeyword("_EMISSION");
            effectMaterial.SetColor("_EmissionColor", effectMaterial.color);
            effectRenderer.material = effectMaterial;
        }

        // Анимация эффекта
        StartCoroutine(AnimateHitEffect(hitEffect));
    }

    /// <summary>
    /// Анимация эффекта попадания
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

            // Увеличиваем размер
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            // Уменьшаем прозрачность
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
    /// Уничтожить пулю с задержкой
    /// </summary>
    private IEnumerator DestroyBulletWithDelay(float delay)
    {
        // Отключаем коллайдер чтобы избежать дополнительных попаданий
        Collider bulletCollider = GetComponent<Collider>();
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }

        yield return new WaitForSeconds(delay);

        Destroy(gameObject);
    }

    /// <summary>
    /// Получить стрелка
    /// </summary>
    public Character GetShooter()
    {
        return shooter;
    }

    /// <summary>
    /// Получить пройденную дистанцию
    /// </summary>
    public float GetTraveledDistance()
    {
        return traveledDistance;
    }

    void OnDrawGizmos()
    {
        // Показываем траекторию полета в Scene view
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPosition, transform.position);

            // Показываем направление
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}