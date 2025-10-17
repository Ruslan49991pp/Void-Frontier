using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Визуализирует занятые клетки объекта кубами
/// Помогает отладить проблемы с размещением и доступом к объектам
/// </summary>
public class OccupiedCellsVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Показывать занятые клетки при старте")]
    public bool showOnStart = true;

    [Tooltip("Цвет визуализации")]
    public Color visualizationColor = new Color(1f, 0f, 0f, 0.3f); // Полупрозрачный красный

    [Tooltip("Высота кубов визуализации")]
    public float cubeHeight = 0.2f;

    [Header("Object Info")]
    [Tooltip("Количество клеток по X")]
    public int cellsX = 8;

    [Tooltip("Количество клеток по Y")]
    public int cellsY = 8;

    [Tooltip("Размер одной клетки")]
    public float cellSize = 1f;

    private List<GameObject> visualizationCubes = new List<GameObject>();
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        if (gridManager != null)
        {
            cellSize = gridManager.cellSize;
        }

        if (showOnStart)
        {
            ShowOccupiedCells();
        }
    }

    /// <summary>
    /// Показать визуализацию занятых клеток
    /// </summary>
    [ContextMenu("Show Occupied Cells")]
    public void ShowOccupiedCells()
    {
        ClearVisualization();

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning("[OccupiedCellsVisualizer] GridManager not found!");
                return;
            }
        }

        // Получаем позицию объекта в сетке
        Vector2Int objectGridPos;

        // Пытаемся получить сохраненную позицию из LocationObjectInfo
        LocationObjectInfo locationInfo = GetComponent<LocationObjectInfo>();
        if (locationInfo != null && locationInfo.gridSize.x > 1)
        {
            // Используем сохраненную позицию нижнего левого угла
            objectGridPos = locationInfo.gridStartPosition;
            cellsX = locationInfo.gridSize.x;
            cellsY = locationInfo.gridSize.y;
        }
        else
        {
            // Fallback: вычисляем позицию из мировых координат
            objectGridPos = gridManager.WorldToGrid(transform.position);
        }

        // Создаем родительский объект для визуализации
        GameObject visualParent = new GameObject("CellsVisualization");
        visualParent.transform.SetParent(transform);
        visualParent.transform.localPosition = Vector3.zero;

        // Создаем кубы для каждой занятой клетки
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                Vector2Int cellGridPos = new Vector2Int(objectGridPos.x + x, objectGridPos.y + y);
                Vector3 cellWorldPos = gridManager.GridToWorld(cellGridPos);

                // Создаем куб визуализации
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Cell_{cellGridPos.x}_{cellGridPos.y}";
                cube.transform.SetParent(visualParent.transform);
                cube.transform.position = cellWorldPos;
                cube.transform.localScale = new Vector3(cellSize * 0.95f, cubeHeight, cellSize * 0.95f);

                // Настраиваем материал
                Renderer renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = visualizationColor;
                    mat.SetFloat("_Mode", 3); // Transparent mode
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    renderer.material = mat;
                }

                // Убираем коллайдер
                Collider collider = cube.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                visualizationCubes.Add(cube);
            }
        }
    }

    /// <summary>
    /// Скрыть визуализацию
    /// </summary>
    [ContextMenu("Hide Occupied Cells")]
    public void HideOccupiedCells()
    {
        ClearVisualization();
    }

    /// <summary>
    /// Очистить все кубы визуализации
    /// </summary>
    void ClearVisualization()
    {
        foreach (GameObject cube in visualizationCubes)
        {
            if (cube != null)
            {
                Destroy(cube);
            }
        }
        visualizationCubes.Clear();

        // Удаляем родительский объект если он есть
        Transform visualParent = transform.Find("CellsVisualization");
        if (visualParent != null)
        {
            Destroy(visualParent.gameObject);
        }
    }

    void OnDestroy()
    {
        ClearVisualization();
    }

    void OnDrawGizmos()
    {
        // Дополнительная визуализация в редакторе
        if (gridManager == null)
            return;

        Gizmos.color = visualizationColor;

        // Получаем позицию объекта в сетке
        Vector2Int objectGridPos;

        // Пытаемся получить сохраненную позицию из LocationObjectInfo
        LocationObjectInfo locationInfo = GetComponent<LocationObjectInfo>();
        if (locationInfo != null && locationInfo.gridSize.x > 1)
        {
            // Используем сохраненную позицию нижнего левого угла
            objectGridPos = locationInfo.gridStartPosition;
        }
        else
        {
            // Fallback: вычисляем позицию из мировых координат
            objectGridPos = gridManager.WorldToGrid(transform.position);
        }

        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                Vector2Int cellGridPos = new Vector2Int(objectGridPos.x + x, objectGridPos.y + y);
                Vector3 cellWorldPos = gridManager.GridToWorld(cellGridPos);
                Gizmos.DrawWireCube(cellWorldPos, new Vector3(cellSize * 0.95f, 0.1f, cellSize * 0.95f));
            }
        }
    }
}
