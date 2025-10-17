using UnityEngine;

/// <summary>
/// Данные о ресурсе
/// Создать через Assets -> Create -> Resources/Resource Data
/// </summary>
[CreateAssetMenu(fileName = "NewResource", menuName = "Resources/Resource Data")]
public class ResourceData : ScriptableObject
{
    [Header("Основная информация")]
    [Tooltip("Название ресурса")]
    public string resourceName = "New Resource";

    [Tooltip("Описание ресурса")]
    [TextArea(3, 5)]
    public string description = "";

    [Header("Визуальные данные")]
    [Tooltip("Иконка ресурса для UI")]
    public Sprite icon;

    [Tooltip("Префаб ресурса для отображения в мире")]
    public GameObject prefab;

    [Header("Свойства")]
    [Tooltip("Максимальный размер стека")]
    public int maxStackSize = 999;

    [Tooltip("Вес единицы ресурса")]
    public float weightPerUnit = 0.1f;

    [Tooltip("Базовая стоимость единицы")]
    public int valuePerUnit = 1;

    [Header("Категория")]
    [Tooltip("Категория ресурса для сортировки")]
    public ResourceCategory category = ResourceCategory.Raw;

    /// <summary>
    /// Получить цвет категории для UI
    /// </summary>
    public Color GetCategoryColor()
    {
        switch (category)
        {
            case ResourceCategory.Raw:       return new Color(0.7f, 0.5f, 0.3f); // Коричневый
            case ResourceCategory.Processed: return new Color(0.5f, 0.7f, 0.9f); // Голубой
            case ResourceCategory.Advanced:  return new Color(0.9f, 0.7f, 0.2f); // Золотой
            case ResourceCategory.Rare:      return new Color(0.8f, 0.3f, 0.8f); // Фиолетовый
            default: return Color.white;
        }
    }

    /// <summary>
    /// Получить полное описание ресурса
    /// </summary>
    public string GetFullDescription()
    {
        string fullDesc = description;
        fullDesc += $"\n\nКатегория: {GetCategoryName()}";
        fullDesc += $"\nМакс. в стеке: {maxStackSize}";
        fullDesc += $"\nВес: {weightPerUnit} кг/ед.";
        fullDesc += $"\nСтоимость: {valuePerUnit} кредитов/ед.";
        return fullDesc;
    }

    /// <summary>
    /// Получить название категории
    /// </summary>
    public string GetCategoryName()
    {
        switch (category)
        {
            case ResourceCategory.Raw:       return "Сырье";
            case ResourceCategory.Processed: return "Обработанное";
            case ResourceCategory.Advanced:  return "Продвинутое";
            case ResourceCategory.Rare:      return "Редкое";
            default: return "Неизвестно";
        }
    }
}

/// <summary>
/// Категории ресурсов
/// </summary>
public enum ResourceCategory
{
    Raw,        // Сырье (металлические руды, древесина и т.д.)
    Processed,  // Обработанное (металлические слитки, доски и т.д.)
    Advanced,   // Продвинутое (сплавы, композиты и т.д.)
    Rare        // Редкое (экзотические материалы)
}
