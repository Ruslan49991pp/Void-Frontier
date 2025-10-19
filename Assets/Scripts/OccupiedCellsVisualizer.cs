using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Р’РёР·СѓР°Р»РёР·РёСЂСѓРµС‚ Р·Р°РЅСЏС‚С‹Рµ РєР»РµС‚РєРё РѕР±СЉРµРєС‚Р° РєСѓР±Р°РјРё
/// РџРѕРјРѕРіР°РµС‚ РѕС‚Р»Р°РґРёС‚СЊ РїСЂРѕР±Р»РµРјС‹ СЃ СЂР°Р·РјРµС‰РµРЅРёРµРј Рё РґРѕСЃС‚СѓРїРѕРј Рє РѕР±СЉРµРєС‚Р°Рј
/// </summary>
public class OccupiedCellsVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("РџРѕРєР°Р·С‹РІР°С‚СЊ Р·Р°РЅСЏС‚С‹Рµ РєР»РµС‚РєРё РїСЂРё СЃС‚Р°СЂС‚Рµ")]
    public bool showOnStart = true;

    [Tooltip("Р¦РІРµС‚ РІРёР·СѓР°Р»РёР·Р°С†РёРё")]
    public Color visualizationColor = new Color(1f, 0f, 0f, 0.3f); // РџРѕР»СѓРїСЂРѕР·СЂР°С‡РЅС‹Р№ РєСЂР°СЃРЅС‹Р№

    [Tooltip("Р’С‹СЃРѕС‚Р° РєСѓР±РѕРІ РІРёР·СѓР°Р»РёР·Р°С†РёРё")]
    public float cubeHeight = 0.2f;

    [Header("Object Info")]
    [Tooltip("РљРѕР»РёС‡РµСЃС‚РІРѕ РєР»РµС‚РѕРє РїРѕ X")]
    public int cellsX = 8;

    [Tooltip("РљРѕР»РёС‡РµСЃС‚РІРѕ РєР»РµС‚РѕРє РїРѕ Y")]
    public int cellsY = 8;

    [Tooltip("Р Р°Р·РјРµСЂ РѕРґРЅРѕР№ РєР»РµС‚РєРё")]
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
    /// РџРѕРєР°Р·Р°С‚СЊ РІРёР·СѓР°Р»РёР·Р°С†РёСЋ Р·Р°РЅСЏС‚С‹С… РєР»РµС‚РѕРє
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
                return;
            }
        }

        // РџРѕР»СѓС‡Р°РµРј РїРѕР·РёС†РёСЋ РѕР±СЉРµРєС‚Р° РІ СЃРµС‚РєРµ
        Vector2Int objectGridPos;

        // РџС‹С‚Р°РµРјСЃСЏ РїРѕР»СѓС‡РёС‚СЊ СЃРѕС…СЂР°РЅРµРЅРЅСѓСЋ РїРѕР·РёС†РёСЋ РёР· LocationObjectInfo
        LocationObjectInfo locationInfo = GetComponent<LocationObjectInfo>();
        if (locationInfo != null && locationInfo.gridSize.x > 1)
        {
            // РСЃРїРѕР»СЊР·СѓРµРј СЃРѕС…СЂР°РЅРµРЅРЅСѓСЋ РїРѕР·РёС†РёСЋ РЅРёР¶РЅРµРіРѕ Р»РµРІРѕРіРѕ СѓРіР»Р°
            objectGridPos = locationInfo.gridStartPosition;
            cellsX = locationInfo.gridSize.x;
            cellsY = locationInfo.gridSize.y;
        }
        else
        {
            // Fallback: РІС‹С‡РёСЃР»СЏРµРј РїРѕР·РёС†РёСЋ РёР· РјРёСЂРѕРІС‹С… РєРѕРѕСЂРґРёРЅР°С‚
            objectGridPos = gridManager.WorldToGrid(transform.position);
        }

        // РЎРѕР·РґР°РµРј СЂРѕРґРёС‚РµР»СЊСЃРєРёР№ РѕР±СЉРµРєС‚ РґР»СЏ РІРёР·СѓР°Р»РёР·Р°С†РёРё
        GameObject visualParent = new GameObject("CellsVisualization");
        visualParent.transform.SetParent(transform);
        visualParent.transform.localPosition = Vector3.zero;

        // РЎРѕР·РґР°РµРј РєСѓР±С‹ РґР»СЏ РєР°Р¶РґРѕР№ Р·Р°РЅСЏС‚РѕР№ РєР»РµС‚РєРё
        for (int x = 0; x < cellsX; x++)
        {
            for (int y = 0; y < cellsY; y++)
            {
                Vector2Int cellGridPos = new Vector2Int(objectGridPos.x + x, objectGridPos.y + y);
                Vector3 cellWorldPos = gridManager.GridToWorld(cellGridPos);

                // РЎРѕР·РґР°РµРј РєСѓР± РІРёР·СѓР°Р»РёР·Р°С†РёРё
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Cell_{cellGridPos.x}_{cellGridPos.y}";
                cube.transform.SetParent(visualParent.transform);
                cube.transform.position = cellWorldPos;
                cube.transform.localScale = new Vector3(cellSize * 0.95f, cubeHeight, cellSize * 0.95f);

                // РќР°СЃС‚СЂР°РёРІР°РµРј РјР°С‚РµСЂРёР°Р»
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

                // РЈР±РёСЂР°РµРј РєРѕР»Р»Р°Р№РґРµСЂ
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
    /// РЎРєСЂС‹С‚СЊ РІРёР·СѓР°Р»РёР·Р°С†РёСЋ
    /// </summary>
    [ContextMenu("Hide Occupied Cells")]
    public void HideOccupiedCells()
    {
        ClearVisualization();
    }

    /// <summary>
    /// РћС‡РёСЃС‚РёС‚СЊ РІСЃРµ РєСѓР±С‹ РІРёР·СѓР°Р»РёР·Р°С†РёРё
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

        // РЈРґР°Р»СЏРµРј СЂРѕРґРёС‚РµР»СЊСЃРєРёР№ РѕР±СЉРµРєС‚ РµСЃР»Рё РѕРЅ РµСЃС‚СЊ
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
        // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅР°СЏ РІРёР·СѓР°Р»РёР·Р°С†РёСЏ РІ СЂРµРґР°РєС‚РѕСЂРµ
        if (gridManager == null)
            return;

        Gizmos.color = visualizationColor;

        // РџРѕР»СѓС‡Р°РµРј РїРѕР·РёС†РёСЋ РѕР±СЉРµРєС‚Р° РІ СЃРµС‚РєРµ
        Vector2Int objectGridPos;

        // РџС‹С‚Р°РµРјСЃСЏ РїРѕР»СѓС‡РёС‚СЊ СЃРѕС…СЂР°РЅРµРЅРЅСѓСЋ РїРѕР·РёС†РёСЋ РёР· LocationObjectInfo
        LocationObjectInfo locationInfo = GetComponent<LocationObjectInfo>();
        if (locationInfo != null && locationInfo.gridSize.x > 1)
        {
            // РСЃРїРѕР»СЊР·СѓРµРј СЃРѕС…СЂР°РЅРµРЅРЅСѓСЋ РїРѕР·РёС†РёСЋ РЅРёР¶РЅРµРіРѕ Р»РµРІРѕРіРѕ СѓРіР»Р°
            objectGridPos = locationInfo.gridStartPosition;
        }
        else
        {
            // Fallback: РІС‹С‡РёСЃР»СЏРµРј РїРѕР·РёС†РёСЋ РёР· РјРёСЂРѕРІС‹С… РєРѕРѕСЂРґРёРЅР°С‚
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
