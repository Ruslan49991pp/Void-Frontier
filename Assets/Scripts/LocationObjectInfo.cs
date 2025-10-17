using UnityEngine;

public class LocationObjectInfo : MonoBehaviour
{
    [Header("Object Information")]
    public string objectType;
    public string objectName;
    public bool isPointOfInterest = false;
    
    [Header("Additional Data")]
    public float health = 100f;
    public bool isDestructible = true;
    public bool canBeScavenged = false;

    [Header("Resource Data")]
    public int metalAmount = 0; // Количество металла в астероиде
    public int maxMetalAmount = 0; // Максимальное количество металла (для определения истощения)

    [Header("Grid Data")]
    [Tooltip("Стартовая позиция в сетке (нижний левый угол для многоклеточных объектов)")]
    public Vector2Int gridStartPosition; // Позиция нижнего левого угла в сетке

    [Tooltip("Размер объекта в клетках")]
    public Vector2Int gridSize = Vector2Int.one; // Размер в клетках (по умолчанию 1x1)
    
    void Start()
    {
        if (string.IsNullOrEmpty(objectName))
        {
            objectName = gameObject.name;
        }
    }
    
    /// <summary>
    /// Получить информацию об объекте в виде строки
    /// </summary>
    public string GetObjectInfo()
    {
        return $"Type: {objectType}, Name: {objectName}, Health: {health}";
    }
    
    /// <summary>
    /// Проверить, является ли объект определенного типа
    /// </summary>
    public bool IsOfType(string type)
    {
        return string.Equals(objectType, type, System.StringComparison.OrdinalIgnoreCase);
    }
}