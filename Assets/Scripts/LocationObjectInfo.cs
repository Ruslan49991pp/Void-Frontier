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