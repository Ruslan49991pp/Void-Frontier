using UnityEngine;

/// <summary>
/// Типы главных объектов модуля
/// </summary>
public enum MainObjectType
{
    None,                    // Нет главного объекта
    LifeSupport,            // Система жизнеобеспечения (жилой модуль)
    ManipulatorArm,         // Рука-манипулятор (склад)
    ReactorInstallation     // Реакторная установка (реактор)
}

/// <summary>
/// Данные о типе главного объекта
/// </summary>
[System.Serializable]
public class MainObjectData
{
    public string objectName;
    public MainObjectType objectType;
    public float maxHealth;
    public int cost;
    public GameObject prefab;
    public Sprite icon;

    public MainObjectData(string name, MainObjectType type, float health, int cost)
    {
        this.objectName = name;
        this.objectType = type;
        this.maxHealth = health;
        this.cost = cost;
    }
}

/// <summary>
/// Компонент главного объекта в модуле
/// </summary>
public class ModuleMainObject : MonoBehaviour
{
    [Header("Object Info")]
    public MainObjectType objectType = MainObjectType.None;
    public string objectName;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Room Reference")]
    public GameObject parentRoom;
    public Vector2Int roomGridPosition;

    private bool isDestroyed = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Нанести урон главному объекту
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        FileLogger.Log($"[ModuleMainObject] {objectName} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            DestroyMainObject();
        }
    }

    /// <summary>
    /// Уничтожить главный объект
    /// </summary>
    void DestroyMainObject()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        FileLogger.Log($"[ModuleMainObject] {objectName} destroyed!");

        // Уведомляем комнату о разрушении главного объекта
        if (parentRoom != null)
        {
            RoomInfo roomInfo = parentRoom.GetComponent<RoomInfo>();
            if (roomInfo != null)
            {
                roomInfo.mainObject = null;
                roomInfo.currentMainObjectType = MainObjectType.None;
                FileLogger.Log($"[ModuleMainObject] Room {roomInfo.roomName} lost its main object");
            }
        }

        // Уничтожаем объект
        Destroy(gameObject);
    }

    /// <summary>
    /// Восстановить здоровье
    /// </summary>
    public void Repair(float amount)
    {
        if (isDestroyed) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        FileLogger.Log($"[ModuleMainObject] {objectName} repaired by {amount}. Health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Получить процент здоровья
    /// </summary>
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}
