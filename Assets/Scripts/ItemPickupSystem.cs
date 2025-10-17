using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Система подбора предметов по нажатию кнопки
/// УСТАРЕЛО: Теперь подбор предметов работает через ПКМ (SelectionManager.HandleRightClick)
/// Ищет ближайший предмет в радиусе и подбирает его при нажатии E
/// </summary>
public class ItemPickupSystem : MonoBehaviour
{
    [Header("Enable/Disable")]
    [Tooltip("ВАЖНО: Система отключена по умолчанию. Используйте ПКМ для подбора предметов.")]
    public bool enablePickupByKey = false;

    [Header("Settings")]
    [Tooltip("Радиус поиска предметов вокруг персонажа")]
    public float pickupRadius = 3f;

    [Tooltip("Кнопка для подбора предметов")]
    public KeyCode pickupKey = KeyCode.E;

    [Tooltip("Показывать подсказку UI над ближайшим предметом")]
    public bool showPickupHint = true;

    [Header("Visual Settings")]
    [Tooltip("Цвет подсветки ближайшего предмета")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.3f);

    [Tooltip("Префаб UI подсказки (опционально)")]
    public GameObject pickupHintPrefab;

    // Внутренние переменные
    private Character character;
    private Item nearestItem;
    private Material originalMaterial;
    private Material highlightMaterial;
    private GameObject currentHintUI;

    void Start()
    {
        // Если система отключена - отключаем весь компонент
        if (!enablePickupByKey)
        {

            enabled = false;
            return;
        }

        character = GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError("[ItemPickupSystem] Character component not found!");
            enabled = false;
            return;
        }

        // Создаем материал для подсветки
        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = highlightColor;
        highlightMaterial.SetFloat("_Mode", 3); // Transparent
        highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        highlightMaterial.SetInt("_ZWrite", 0);
        highlightMaterial.DisableKeyword("_ALPHATEST_ON");
        highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
        highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        highlightMaterial.renderQueue = 3000;
    }

    void Update()
    {
        // Проверяем, включена ли система
        if (!enablePickupByKey)
        {
            // Убираем подсветку если система отключена
            if (nearestItem != null)
            {
                RemoveHighlight();
                nearestItem = null;
            }
            HidePickupHint();
            return;
        }

        // Ищем ближайший предмет
        FindNearestItem();

        // Подбираем при нажатии клавиши
        if (Input.GetKeyDown(pickupKey) && nearestItem != null)
        {
            PickupNearestItem();
        }
    }

    /// <summary>
    /// Найти ближайший предмет в радиусе
    /// </summary>
    void FindNearestItem()
    {
        // ЗАЩИТА: Проверяем что nearestItem не был уничтожен перед обращением
        if (nearestItem != null && ReferenceEquals(nearestItem, null))
        {

            nearestItem = null;
            originalMaterial = null;
        }

        // Убираем подсветку с предыдущего предмета
        if (nearestItem != null)
        {
            RemoveHighlight();
        }

        nearestItem = null;
        float nearestDistance = float.MaxValue;

        // Ищем все предметы в радиусе
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius);

        foreach (Collider col in colliders)
        {
            if (col == null || ReferenceEquals(col, null))
                continue;

            Item item = null;
            try
            {
                item = col.GetComponent<Item>();
                // Дополнительная проверка что Item не уничтожен
                if (item != null && ReferenceEquals(item, null))
                {
                    item = null;
                }
            }
            catch (System.Exception)
            {
                continue;
            }

            if (item != null && item.canBePickedUp)
            {
                float distance = Vector3.Distance(transform.position, item.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestItem = item;
                }
            }
        }

        // Подсвечиваем ближайший предмет
        if (nearestItem != null && !ReferenceEquals(nearestItem, null))
        {
            HighlightItem(nearestItem);

            if (showPickupHint)
            {
                ShowPickupHint();
            }
        }
        else
        {
            HidePickupHint();
        }
    }

    /// <summary>
    /// Подсветить предмет
    /// </summary>
    void HighlightItem(Item item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Сохраняем оригинальный материал
            if (originalMaterial == null)
            {
                originalMaterial = renderer.material;
            }

            // Меняем цвет для подсветки (не меняя материал полностью)
            Color originalColor = renderer.material.color;
            Color highlightedColor = Color.Lerp(originalColor, highlightColor, 0.5f);
            renderer.material.color = highlightedColor;
        }
    }

    /// <summary>
    /// Убрать подсветку с предмета
    /// </summary>
    void RemoveHighlight()
    {
        // КРИТИЧЕСКИ ВАЖНО: Проверяем что Item не был уничтожен
        if (nearestItem != null && !ReferenceEquals(nearestItem, null))
        {
            try
            {
                Renderer renderer = nearestItem.GetComponent<Renderer>();
                if (renderer != null && originalMaterial != null)
                {
                    renderer.material = originalMaterial;
                    originalMaterial = null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemPickupSystem] [RemoveHighlight] Exception: {ex.Message}");
                // Очищаем ссылки чтобы избежать повторных ошибок
                nearestItem = null;
                originalMaterial = null;
            }
        }
        else if (nearestItem != null && ReferenceEquals(nearestItem, null))
        {
            // Item был уничтожен - очищаем ссылку

            nearestItem = null;
            originalMaterial = null;
        }
    }

    /// <summary>
    /// Показать подсказку UI
    /// </summary>
    void ShowPickupHint()
    {
        // ЗАЩИТА: Проверяем что nearestItem не был уничтожен
        if (nearestItem != null && ReferenceEquals(nearestItem, null))
        {

            HidePickupHint();
            nearestItem = null;
            return;
        }

        if (currentHintUI == null && pickupHintPrefab != null)
        {
            currentHintUI = Instantiate(pickupHintPrefab);
        }

        // Позиционируем подсказку над предметом
        if (currentHintUI != null && nearestItem != null && !ReferenceEquals(nearestItem, null))
        {
            try
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(nearestItem.transform.position + Vector3.up * 0.5f);
                currentHintUI.transform.position = screenPos;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemPickupSystem] [ShowPickupHint] Exception: {ex.Message}");
                HidePickupHint();
                nearestItem = null;
            }
        }
    }

    /// <summary>
    /// Скрыть подсказку UI
    /// </summary>
    void HidePickupHint()
    {
        if (currentHintUI != null)
        {
            Destroy(currentHintUI);
            currentHintUI = null;
        }
    }

    /// <summary>
    /// Подобрать ближайший предмет
    /// </summary>
    void PickupNearestItem()
    {
        if (nearestItem == null || character == null)
            return;



        // Проверяем, может ли персонаж поднять предмет
        if (nearestItem.CanBePickedUpBy(character))
        {
            // Освобождаем клетку в GridManager если предмет занимал её
            GridManager gridManager = FindObjectOfType<GridManager>();
            if (gridManager != null)
            {
                Vector2Int gridPos = gridManager.WorldToGrid(nearestItem.transform.position);
                gridManager.FreeCell(gridPos);
            }

            // Подбираем предмет
            nearestItem.PickUp(character);



            // Убираем подсказку
            HidePickupHint();

            nearestItem = null;
            originalMaterial = null;
        }
        else
        {
            Debug.LogWarning($"[ItemPickupSystem] Cannot pick up item: too far or inventory full");
        }
    }

    void OnDisable()
    {
        // Убираем подсветку при отключении
        RemoveHighlight();
        HidePickupHint();
    }

    void OnDrawGizmosSelected()
    {
        // Показываем радиус поиска предметов
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Показываем линию к ближайшему предмету
        if (nearestItem != null && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, nearestItem.transform.position);
        }
    }
}
