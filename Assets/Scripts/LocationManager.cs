using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationData
{
    public string locationName;
    public Vector2Int gridSize;
    public Vector3 playerSpawnPoint;
    public List<Vector3> pointsOfInterest;
    public bool isGenerated;
}

[System.Serializable]
public class ObjectSpawnSettings
{
    [Header("Stations")]
    public GameObject[] stationPrefabs;
    public int minStations = 0;
    public int maxStations = 2;
    
    [Header("Asteroids")]
    public GameObject[] asteroidPrefabs;
    public int minAsteroids = 8;
    public int maxAsteroids = 12;
    
    [Header("Debris")]
    public GameObject[] debrisPrefabs;
    public int minDebris = 15;
    public int maxDebris = 25;
}

public class LocationManager : MonoBehaviour
{
    [Header("Location Settings")]
    public LocationData currentLocation;
    public Vector2Int defaultGridSize = new Vector2Int(150, 150); // РћРїС‚РёРјРёР·РёСЂРѕРІР°РЅРЅС‹Р№ СЂР°Р·РјРµСЂ
    public float gridCellSize = 1f;
    
    [Header("Generation")]
    public ObjectSpawnSettings spawnSettings;
    public Transform contentParent;
    public bool autoGenerateOnStart = true;
    public float minDistanceBetweenObjects = 20f;
    public int maxPlacementAttempts = 100;
    
    [Header("Player")]
    public Transform playerShip;
    public float spawnSafeDistance = 50f;
    
    [Header("Grid System")]
    public GridManager gridManager;
    
    // Р’РЅСѓС‚СЂРµРЅРЅРёРµ РїРµСЂРµРјРµРЅРЅС‹Рµ
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private Dictionary<Vector2Int, GameObject> gridObjects = new Dictionary<Vector2Int, GameObject>();
    
    // РЎРѕР±С‹С‚РёСЏ
    public System.Action<LocationData> OnLocationGenerated;
    public System.Action OnLocationCleared;
    
    void Start()
    {
        InitializeLocation();

        if (autoGenerateOnStart)
        {
            // Р—Р°РґРµСЂР¶РєР° С‡С‚РѕР±С‹ GridManager СѓСЃРїРµР» СЃРѕР·РґР°С‚СЊ РїРµСЂСЃРѕРЅР°Р¶РµР№ Рё РєРѕРєРїРёС‚ РїРµСЂРІС‹РјРё
            StartCoroutine(DelayedGenerateLocation());
        }
    }

    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ Р»РѕРєР°С†РёРё СЃ Р·Р°РґРµСЂР¶РєРѕР№ РґР»СЏ РїСЂРёРѕСЂРёС‚РµС‚Р° РєРѕРєРїРёС‚Р°
    /// </summary>
    System.Collections.IEnumerator DelayedGenerateLocation()
    {
        // Р–РґРµРј 2 РєР°РґСЂР° С‡С‚РѕР±С‹ GridManager СѓСЃРїРµР» СЃРѕР·РґР°С‚СЊ РїРµСЂСЃРѕРЅР°Р¶РµР№ Рё РєРѕРєРїРёС‚
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        GenerateLocation();
    }
    
    /// <summary>
    /// РРЅРёС†РёР°Р»РёР·Р°С†РёСЏ Р»РѕРєР°С†РёРё СЃ Р±Р°Р·РѕРІС‹РјРё РЅР°СЃС‚СЂРѕР№РєР°РјРё
    /// </summary>
    public void InitializeLocation()
    {
        if (currentLocation == null)
        {
            currentLocation = new LocationData();
            currentLocation.locationName = "VoidFrontier Location";
            currentLocation.gridSize = defaultGridSize;
            currentLocation.isGenerated = false;
            currentLocation.pointsOfInterest = new List<Vector3>();
        }

        // РЈР±РµР¶РґР°РµРјСЃСЏ С‡С‚Рѕ СЂР°Р·РјРµСЂ СЃРµС‚РєРё РЅРµ РЅСѓР»РµРІРѕР№
        if (currentLocation.gridSize.x == 0 || currentLocation.gridSize.y == 0)
        {
            currentLocation.gridSize = defaultGridSize;
        }

        // Р’РђР–РќРћ: РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј spawnSettings РµСЃР»Рё РµРіРѕ РЅРµС‚
        if (spawnSettings == null)
        {
            spawnSettings = new ObjectSpawnSettings();
        }
        
        // РЎРѕР·РґР°РµРј СЂРѕРґРёС‚РµР»СЊСЃРєРёР№ РѕР±СЉРµРєС‚ РґР»СЏ СЃРѕРґРµСЂР¶РёРјРѕРіРѕ РµСЃР»Рё РµРіРѕ РЅРµС‚
        if (contentParent == null)
        {
            GameObject contentGO = new GameObject("LocationContent");
            contentGO.transform.SetParent(transform);
            contentParent = contentGO.transform;
        }
        
        // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј GridManager РµСЃР»Рё РµРіРѕ РЅРµС‚
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                GameObject gridGO = new GameObject("GridManager");
                gridGO.transform.SetParent(transform);
                gridManager = gridGO.AddComponent<GridManager>();
            }
        }
        
        // РЎРёРЅС…СЂРѕРЅРёР·РёСЂСѓРµРј СЂР°Р·РјРµСЂС‹ РјРµР¶РґСѓ GridManager Рё LocationManager
        if (gridManager != null)
        {
            gridManager.UpdateGridSettings(currentLocation.gridSize.x, currentLocation.gridSize.y, gridCellSize);
        }
    }
    
    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ С‚РµСЃС‚РѕРІС‹С… РїСЂРµС„Р°Р±РѕРІ РµСЃР»Рё РѕРЅРё РЅРµ СѓСЃС‚Р°РЅРѕРІР»РµРЅС‹
    /// </summary>
    void EnsureTestPrefabs()
    {
        // РЎРѕР·РґР°РµРј С‚РµСЃС‚РѕРІС‹Рµ РїСЂРµС„Р°Р±С‹ РµСЃР»Рё РјР°СЃСЃРёРІС‹ РїСѓСЃС‚С‹Рµ
        if (spawnSettings.stationPrefabs == null || spawnSettings.stationPrefabs.Length == 0)
        {
            GameObject stationPrefab = CreateTestPrefab("TestStation", Color.blue, new Vector3(8, 4, 8));
            spawnSettings.stationPrefabs = new GameObject[] { stationPrefab };
        }
        
        if (spawnSettings.asteroidPrefabs == null || spawnSettings.asteroidPrefabs.Length == 0)
        {
            GameObject asteroidPrefab = CreateTestPrefab("TestAsteroid", Color.gray, new Vector3(6, 3, 6));
            spawnSettings.asteroidPrefabs = new GameObject[] { asteroidPrefab };
        }
        
        if (spawnSettings.debrisPrefabs == null || spawnSettings.debrisPrefabs.Length == 0)
        {
            GameObject debrisPrefab = CreateTestPrefab("TestDebris", Color.yellow, new Vector3(0.8f, 0.8f, 0.8f));
            spawnSettings.debrisPrefabs = new GameObject[] { debrisPrefab };
        }
    }
    
    /// <summary>
    /// РЎРѕР·РґР°РЅРёРµ С‚РµСЃС‚РѕРІРѕРіРѕ РїСЂРµС„Р°Р±Р° РІ РІРёРґРµ РєСѓР±Р°
    /// </summary>
    GameObject CreateTestPrefab(string name, Color color, Vector3 size)
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = name;
        prefab.transform.localScale = size;
        
        // РЈР±РµР¶РґР°РµРјСЃСЏ С‡С‚Рѕ РєРѕР»Р»Р°Р№РґРµСЂ РµСЃС‚СЊ Рё РЅР°СЃС‚СЂРѕРµРЅ РїСЂР°РІРёР»СЊРЅРѕ
        BoxCollider collider = prefab.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = prefab.AddComponent<BoxCollider>();
        }
        
        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј С†РІРµС‚
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            // РЈР±РµР¶РґР°РµРјСЃСЏ С‡С‚Рѕ СЂРµРЅРґРµСЂРµСЂ РІРєР»СЋС‡РµРЅ
            renderer.enabled = true;

            // РџС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё РїРѕРґС…РѕРґСЏС‰РёР№ С€РµР№РґРµСЂ (UNLIT РґР»СЏ РІРёРґРёРјРѕСЃС‚Рё Р±РµР· РѕСЃРІРµС‰РµРЅРёСЏ)
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;
                renderer.material = material;
            }
            else
            {
                // РСЃРїРѕР»СЊР·СѓРµРј СЃСѓС‰РµСЃС‚РІСѓСЋС‰РёР№ РјР°С‚РµСЂРёР°Р» Рё РїСЂРѕСЃС‚Рѕ РјРµРЅСЏРµРј С†РІРµС‚
                renderer.material.color = color;
            }
        }
        else
        {
        }
        
        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ LocationObjectInfo
        LocationObjectInfo objectInfo = prefab.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = prefab.AddComponent<LocationObjectInfo>();
        }
        
        // РќР°СЃС‚СЂР°РёРІР°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± РѕР±СЉРµРєС‚Рµ
        if (name.Contains("Station"))
        {
            objectInfo.objectType = "Station";
            objectInfo.objectName = "Test Station";
            objectInfo.health = 500f;
            objectInfo.isDestructible = false;
        }
        else if (name.Contains("Asteroid"))
        {
            objectInfo.objectType = "Asteroid";
            objectInfo.objectName = "Test Asteroid";
            objectInfo.health = 200f;
            objectInfo.canBeScavenged = true;
        }
        else if (name.Contains("Debris"))
        {
            objectInfo.objectType = "Debris";
            objectInfo.objectName = "Test Debris";
            objectInfo.health = 50f;
            objectInfo.canBeScavenged = true;
        }

        // Р’РђР–РќРћ: РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј СЃР»РѕР№ "Selectable" РґР»СЏ raycast
        int selectableLayer = LayerMask.NameToLayer("Selectable");
        if (selectableLayer != -1)
        {
            prefab.layer = selectableLayer;
        }
        else
        {
        }

        // РЎРєСЂС‹РІР°РµРј РїСЂРµС„Р°Р±
        prefab.SetActive(false);


        return prefab;
    }
    
    
    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ РІСЃРµРіРѕ СЃРѕРґРµСЂР¶РёРјРѕРіРѕ Р»РѕРєР°С†РёРё
    /// </summary>
    public void GenerateLocation()
    {
        ClearLocation();
        
        currentLocation.isGenerated = true;
        
        
        // Р”РёР°РіРЅРѕСЃС‚РёРєР° РїСЂРµС„Р°Р±РѕРІ
        if (spawnSettings == null)
        {
            return;
        }
        
        
        // РџСЂРѕРІРµСЂСЏРµРј Рё СЃРѕР·РґР°РµРј С‚РµСЃС‚РѕРІС‹Рµ РїСЂРµС„Р°Р±С‹ РµСЃР»Рё РѕРЅРё РЅРµ СѓСЃС‚Р°РЅРѕРІР»РµРЅС‹
        EnsureTestPrefabs();
        
        // Р“РµРЅРµСЂРёСЂСѓРµРј РѕР±СЉРµРєС‚С‹
        GenerateStations();
        GenerateAsteroids();
        GenerateDebris();
        
        // РћРїСЂРµРґРµР»СЏРµРј С‚РѕС‡РєСѓ СЃРїР°РІРЅР° РёРіСЂРѕРєР°
        SetPlayerSpawnPoint();
        
        // РЎРѕР·РґР°РµРј С‚РѕС‡РєРё РёРЅС‚РµСЂРµСЃР°
        GeneratePointsOfInterest();
        
        
        // Р’С‹РІРѕРґРёРј СЃС‚Р°С‚РёСЃС‚РёРєСѓ СЃРµС‚РєРё
        if (gridManager != null)
        {
            gridManager.LogGridStats();
        }
        
        // Р”РёР°РіРЅРѕСЃС‚РёРєР° СЃРѕР·РґР°РЅРЅС‹С… РѕР±СЉРµРєС‚РѕРІ
        DiagnoseCreatedObjects();
        
        OnLocationGenerated?.Invoke(currentLocation);
    }
    
    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ СЃС‚Р°РЅС†РёР№
    /// </summary>
    void GenerateStations()
    {
        
        if (spawnSettings.stationPrefabs == null || spawnSettings.stationPrefabs.Length == 0)
        {
            return;
        }
        
        int stationCount = Random.Range(spawnSettings.minStations, spawnSettings.maxStations + 1);
        
        int createdCount = 0;
        for (int i = 0; i < stationCount; i++)
        {
            
            // РЎС‚Р°РЅС†РёРё Р·Р°РЅРёРјР°СЋС‚ 20x20 РєР»РµС‚РѕРє (20x20 РјРµС‚СЂРѕРІ)
            GridCell cell = gridManager.GetRandomFreeCellArea(20, 20);
            if (cell == null)
            {
                continue;
            }
            
            GameObject stationPrefab = spawnSettings.stationPrefabs[Random.Range(0, spawnSettings.stationPrefabs.Length)];
            
            if (stationPrefab != null)
            {
                // Р’С‹С‡РёСЃР»СЏРµРј С‚РѕС‡РЅС‹Р№ С†РµРЅС‚СЂ РѕР±Р»Р°СЃС‚Рё 20x20
                Vector3 areaCenterOffset = new Vector3(
                    (20 - 1) * 0.5f * gridCellSize,  // (РєРѕР»РёС‡РµСЃС‚РІРѕ РєР»РµС‚РѕРє - 1) / 2 * СЂР°Р·РјРµСЂ РєР»РµС‚РєРё
                    0,
                    (20 - 1) * 0.5f * gridCellSize
                );
                Vector3 stationPosition = cell.worldPosition + areaCenterOffset;
                
                // Р‘РµР· РїРѕРІРѕСЂРѕС‚Р° - РёСЃРїРѕР»СЊР·СѓРµРј РїРѕРІРѕСЂРѕС‚ РїСЂРµС„Р°Р±Р°
                GameObject station = Instantiate(stationPrefab, stationPosition, Quaternion.identity, contentParent);
                station.SetActive(true);
                
                // Р—Р°РЅРёРјР°РµРј РѕР±Р»Р°СЃС‚СЊ 20x20 РІ СЃРµС‚РєРµ
                gridManager.OccupyCellArea(cell.gridPosition, 20, 20, station, "Station");
                
                RegisterObject(station, "Station");
                
                currentLocation.pointsOfInterest.Add(stationPosition);
                createdCount++;
            }
        }
        
    }
    
    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ Р°СЃС‚РµСЂРѕРёРґРѕРІ
    /// </summary>
    void GenerateAsteroids()
    {
        
        if (spawnSettings.asteroidPrefabs == null || spawnSettings.asteroidPrefabs.Length == 0)
        {
            return;
        }
        
        int asteroidCount = Random.Range(spawnSettings.minAsteroids, spawnSettings.maxAsteroids + 1);
        
        int createdCount = 0;
        for (int i = 0; i < asteroidCount; i++)
        {
            
            // РђСЃС‚РµСЂРѕРёРґС‹ Р·Р°РЅРёРјР°СЋС‚ 8x8 РєР»РµС‚РѕРє (8x8 РјРµС‚СЂРѕРІ)
            GridCell cell = gridManager.GetRandomFreeCellArea(8, 8);
            if (cell == null)
            {
                continue;
            }
            
            GameObject asteroidPrefab = spawnSettings.asteroidPrefabs[Random.Range(0, spawnSettings.asteroidPrefabs.Length)];
            
            if (asteroidPrefab != null)
            {
                // Р’С‹С‡РёСЃР»СЏРµРј С‚РѕС‡РЅС‹Р№ С†РµРЅС‚СЂ РѕР±Р»Р°СЃС‚Рё 8x8
                Vector3 areaCenterOffset = new Vector3(
                    (8 - 1) * 0.5f * gridCellSize,   // (РєРѕР»РёС‡РµСЃС‚РІРѕ РєР»РµС‚РѕРє - 1) / 2 * СЂР°Р·РјРµСЂ РєР»РµС‚РєРё
                    0,
                    (8 - 1) * 0.5f * gridCellSize
                );
                Vector3 asteroidPosition = cell.worldPosition + areaCenterOffset;
                
                // Р Р°РЅРґРѕРјРёР·РёСЂСѓРµРј СЂР°Р·РјРµСЂ Р°СЃС‚РµСЂРѕРёРґР° (0.7 - 1.3 РѕС‚ Р±Р°Р·РѕРІРѕРіРѕ СЂР°Р·РјРµСЂР°)
                float sizeMultiplier = Random.Range(0.7f, 1.3f);

                // Р‘РµР· РїРѕРІРѕСЂРѕС‚Р° - РёСЃРїРѕР»СЊР·СѓРµРј РїРѕРІРѕСЂРѕС‚ РїСЂРµС„Р°Р±Р°
                GameObject asteroid = Instantiate(asteroidPrefab, asteroidPosition, Quaternion.identity, contentParent);
                asteroid.transform.localScale *= sizeMultiplier; // РЈРІРµР»РёС‡РёРІР°РµРј/СѓРјРµРЅСЊС€Р°РµРј СЂР°Р·РјРµСЂ
                asteroid.SetActive(true);

                // Р”РёР°РіРЅРѕСЃС‚РёРєР° РІРёРґРёРјРѕСЃС‚Рё
                Renderer asteroidRenderer = asteroid.GetComponent<Renderer>();
                if (asteroidRenderer != null)
                {
                }
                else
                {
                }

                // Р—Р°РЅРёРјР°РµРј РѕР±Р»Р°СЃС‚СЊ 8x8 РІ СЃРµС‚РєРµ
                gridManager.OccupyCellArea(cell.gridPosition, 8, 8, asteroid, "Asteroid");

                // РќР°Р·РЅР°С‡Р°РµРј РєРѕР»РёС‡РµСЃС‚РІРѕ РјРµС‚Р°Р»Р»Р° РІ Р·Р°РІРёСЃРёРјРѕСЃС‚Рё РѕС‚ СЂР°Р·РјРµСЂР°
                LocationObjectInfo asteroidInfo = asteroid.GetComponent<LocationObjectInfo>();

                // Р•СЃР»Рё РєРѕРјРїРѕРЅРµРЅС‚Р° РЅРµС‚ - РґРѕР±Р°РІР»СЏРµРј РµРіРѕ
                if (asteroidInfo == null)
                {
                    asteroidInfo = asteroid.AddComponent<LocationObjectInfo>();
                }

                // РќР°СЃС‚СЂР°РёРІР°РµРј РёРЅС„РѕСЂРјР°С†РёСЋ РѕР± Р°СЃС‚РµСЂРѕРёРґРµ
                asteroidInfo.objectType = "Asteroid";
                asteroidInfo.objectName = "Asteroid";
                asteroidInfo.health = 200f;
                asteroidInfo.isDestructible = false;
                asteroidInfo.canBeScavenged = true;

                // РњРµС‚Р°Р»Р» Р·Р°РІРёСЃРёС‚ РѕС‚ СЂР°Р·РјРµСЂР°: Р±Р°Р·РѕРІРѕРµ 100-300, СѓРјРЅРѕР¶РµРЅРЅРѕРµ РЅР° СЂР°Р·РјРµСЂ
                int baseMetal = Random.Range(100, 301);
                asteroidInfo.maxMetalAmount = Mathf.RoundToInt(baseMetal * sizeMultiplier);
                asteroidInfo.metalAmount = asteroidInfo.maxMetalAmount;

                // Р’РђР–РќРћ: РЎРѕС…СЂР°РЅСЏРµРј РїРѕР·РёС†РёСЋ РІ СЃРµС‚РєРµ РґР»СЏ РєРѕСЂСЂРµРєС‚РЅРѕР№ РІРёР·СѓР°Р»РёР·Р°С†РёРё
                asteroidInfo.gridStartPosition = cell.gridPosition;
                asteroidInfo.gridSize = new Vector2Int(8, 8);

                // Р”РѕР±Р°РІР»СЏРµРј РІРёР·СѓР°Р»РёР·Р°С‚РѕСЂ Р·Р°РЅСЏС‚С‹С… РєР»РµС‚РѕРє РґР»СЏ РѕС‚Р»Р°РґРєРё
                OccupiedCellsVisualizer visualizer = asteroid.AddComponent<OccupiedCellsVisualizer>();
                visualizer.cellsX = 8;
                visualizer.cellsY = 8;
                visualizer.showOnStart = true;
                visualizer.visualizationColor = new Color(1f, 0f, 0f, 0.3f); // РљСЂР°СЃРЅС‹Р№ РїРѕР»СѓРїСЂРѕР·СЂР°С‡РЅС‹Р№
                visualizer.cubeHeight = 0.2f;

                RegisterObject(asteroid, "Asteroid");

                createdCount++;
            }
        }
        
    }
    
    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ РѕР±Р»РѕРјРєРѕРІ
    /// </summary>
    void GenerateDebris()
    {
        if (spawnSettings.debrisPrefabs == null || spawnSettings.debrisPrefabs.Length == 0) 
        {
            return;
        }
        
        int debrisCount = Random.Range(spawnSettings.minDebris, spawnSettings.maxDebris + 1);
        
        int createdCount = 0;
        for (int i = 0; i < debrisCount; i++)
        {
            
            // РћР±Р»РѕРјРєРё Р·Р°РЅРёРјР°СЋС‚ 1 РєР»РµС‚РєСѓ Рё СЂР°Р·РјРµС‰Р°СЋС‚СЃСЏ РІ С†РµРЅС‚СЂРµ
            GridCell cell = GetRandomGridCell("Debris");
            if (cell == null)
            {
                continue;
            }
            
            GameObject debrisPrefab = spawnSettings.debrisPrefabs[Random.Range(0, spawnSettings.debrisPrefabs.Length)];
            
            if (debrisPrefab != null)
            {
                // Р Р°Р·РјРµС‰Р°РµРј С‚РѕС‡РЅРѕ РІ С†РµРЅС‚СЂРµ РµРґРёРЅСЃС‚РІРµРЅРЅРѕР№ СЏС‡РµР№РєРё СЃ РѕРіСЂР°РЅРёС‡РµРЅРЅС‹Рј РїРѕРІРѕСЂРѕС‚РѕРј
                Quaternion limitedRotation = Quaternion.Euler(
                    Random.Range(-15f, 15f),    // РќРµР±РѕР»СЊС€РѕР№ РЅР°РєР»РѕРЅ РїРѕ X
                    Random.Range(0f, 360f),     // РџРѕР»РЅС‹Р№ РїРѕРІРѕСЂРѕС‚ РїРѕ Y
                    Random.Range(-15f, 15f)     // РќРµР±РѕР»СЊС€РѕР№ РЅР°РєР»РѕРЅ РїРѕ Z
                );
                GameObject debris = Instantiate(debrisPrefab, cell.worldPosition, limitedRotation, contentParent);
                debris.SetActive(true);
                
                // Р—Р°РЅРёРјР°РµРј СЏС‡РµР№РєСѓ РІ СЃРµС‚РєРµ
                gridManager.OccupyCell(cell.gridPosition, debris, "Debris");
                
                RegisterObject(debris, "Debris");
                
                createdCount++;
            }
        }
        
    }
    
    /// <summary>
    /// РЈСЃС‚Р°РЅРѕРІРєР° С‚РѕС‡РєРё СЃРїР°РІРЅР° РёРіСЂРѕРєР°
    /// </summary>
    void SetPlayerSpawnPoint()
    {
        Vector3 spawnPoint = Vector3.zero; // РРЅРёС†РёР°Р»РёР·РёСЂСѓРµРј Р·РЅР°С‡РµРЅРёРµРј РїРѕ СѓРјРѕР»С‡Р°РЅРёСЋ
        int attempts = 0;
        int maxAttempts = 50;
        
        
        do
        {
            GridCell playerCell = GetRandomGridCell("Player");
            if (playerCell != null)
            {
                spawnPoint = playerCell.worldPosition;
                // Р—Р°РЅРёРјР°РµРј СЏС‡РµР№РєСѓ РґР»СЏ РёРіСЂРѕРєР° (С‚РѕР»СЊРєРѕ РµСЃР»Рё playerShip РЅРµ null)
                if (playerShip != null)
                {
                    gridManager.OccupyCell(playerCell.gridPosition, playerShip.gameObject, "Player");
                }
                break;
            }
            attempts++;
        }
        while (attempts < maxAttempts);
        
        currentLocation.playerSpawnPoint = spawnPoint;
        
        
        // Р Р°Р·РјРµС‰Р°РµРј РёРіСЂРѕРєР° РµСЃР»Рё РѕРЅ РµСЃС‚СЊ
        if (playerShip != null)
        {
            playerShip.position = spawnPoint;
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Р“РµРЅРµСЂР°С†РёСЏ С‚РѕС‡РµРє РёРЅС‚РµСЂРµСЃР°
    /// </summary>
    void GeneratePointsOfInterest()
    {
        // РўРѕС‡РєРё РёРЅС‚РµСЂРµСЃР° СѓР¶Рµ РґРѕР±Р°РІР»СЏСЋС‚СЃСЏ РїСЂРё СЃРѕР·РґР°РЅРёРё СЃС‚Р°РЅС†РёР№
        // Р—РґРµСЃСЊ РјРѕР¶РЅРѕ РґРѕР±Р°РІРёС‚СЊ РґРѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Рµ С‚РѕС‡РєРё РёРЅС‚РµСЂРµСЃР°
    }
    
    /// <summary>
    /// РџРѕР»СѓС‡РµРЅРёРµ СЃР»СѓС‡Р°Р№РЅРѕР№ РїРѕР·РёС†РёРё РІ РїСЂРµРґРµР»Р°С… Р»РѕРєР°С†РёРё (СЃС‚Р°СЂС‹Р№ РјРµС‚РѕРґ, РѕСЃС‚Р°РІР»РµРЅ РґР»СЏ СЃРѕРІРјРµСЃС‚РёРјРѕСЃС‚Рё)
    /// </summary>
    Vector3 GetRandomLocationPosition()
    {
        // РўРµРїРµСЂСЊ РёСЃРїРѕР»СЊР·СѓРµРј СЃРµС‚РєСѓ РґР»СЏ РїРѕР»СѓС‡РµРЅРёСЏ РїРѕР·РёС†РёРё
        if (gridManager != null)
        {
            GridCell cell = gridManager.GetRandomFreeCell();
            if (cell != null)
            {
                return cell.worldPosition;
            }
        }
        
        // Fallback РЅР° СЃС‚Р°СЂС‹Р№ РјРµС‚РѕРґ РµСЃР»Рё СЃРµС‚РєР° РЅРµРґРѕСЃС‚СѓРїРЅР°
        float halfWidth = (currentLocation.gridSize.x * gridCellSize) * 0.5f;
        float halfHeight = (currentLocation.gridSize.y * gridCellSize) * 0.5f;
        
        float x = Random.Range(-halfWidth, halfWidth);
        float z = Random.Range(-halfHeight, halfHeight);
        
        Vector3 position = new Vector3(x, 0, z);
        
        return position;
    }
    
    /// <summary>
    /// РџРѕР»СѓС‡РµРЅРёРµ СЃР»СѓС‡Р°Р№РЅРѕР№ СЃРІРѕР±РѕРґРЅРѕР№ СЏС‡РµР№РєРё СЃРµС‚РєРё РґР»СЏ СЂР°Р·РјРµС‰РµРЅРёСЏ РѕР±СЉРµРєС‚Р°
    /// </summary>
    GridCell GetRandomGridCell(string objectType = "")
    {
        if (gridManager == null)
        {
            return null;
        }
        
        GridCell cell = gridManager.GetRandomFreeCell();
        if (cell != null)
        {
        }
        
        return cell;
    }
    
    /// <summary>
    /// РџРѕР»СѓС‡РµРЅРёРµ Р±РµР·РѕРїР°СЃРЅРѕР№ РїРѕР·РёС†РёРё СЃ СѓС‡РµС‚РѕРј РјРёРЅРёРјР°Р»СЊРЅРѕРіРѕ СЂР°СЃСЃС‚РѕСЏРЅРёСЏ РґРѕ РґСЂСѓРіРёС… РѕР±СЉРµРєС‚РѕРІ
    /// </summary>
    Vector3 GetSafeSpawnPosition(float minDistance = 0f)
    {
        if (minDistance <= 0f)
            minDistance = minDistanceBetweenObjects;
            
        Vector3 position;
        int attempts = 0;
        
        do
        {
            position = GetRandomLocationPosition();
            attempts++;
            
            if (IsPositionSafe(position, minDistance))
                break;
                
        } while (attempts < maxPlacementAttempts);
        
        
        return position;
    }
    
    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° Р±РµР·РѕРїР°СЃРЅРѕСЃС‚Рё РїРѕР·РёС†РёРё (РЅРµС‚ РѕР±СЉРµРєС‚РѕРІ РІ Р·Р°РґР°РЅРЅРѕРј СЂР°РґРёСѓСЃРµ)
    /// </summary>
    bool IsPositionSafe(Vector3 position, float minDistance)
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null && Vector3.Distance(position, obj.transform.position) < minDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° Р±РµР·РѕРїР°СЃРЅРѕСЃС‚Рё РїРѕР·РёС†РёРё РґР»СЏ РёРіСЂРѕРєР°
    /// </summary>
    bool IsPositionSafeForPlayer(Vector3 position)
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null && Vector3.Distance(position, obj.transform.position) < spawnSafeDistance)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Р РµРіРёСЃС‚СЂР°С†РёСЏ РѕР±СЉРµРєС‚Р° РІ СЃРёСЃС‚РµРјРµ
    /// </summary>
    void RegisterObject(GameObject obj, string objectType)
    {
        spawnedObjects.Add(obj);
        
        // Р”РѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ РґР»СЏ РёРґРµРЅС‚РёС„РёРєР°С†РёРё С‚РёРїР° РѕР±СЉРµРєС‚Р° РІРјРµСЃС‚Рѕ С‚РµРіР°
        var objectInfo = obj.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = obj.AddComponent<LocationObjectInfo>();
            objectInfo.objectType = objectType;
            
            // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РёРјСЏ РµСЃР»Рё РѕРЅРѕ РЅРµ Р·Р°РґР°РЅРѕ
            if (string.IsNullOrEmpty(objectInfo.objectName))
            {
                objectInfo.objectName = obj.name;
            }
        }
        
        // РџСЂРѕРІРµСЂСЏРµРј РЅР°Р»РёС‡РёРµ РєРѕР»Р»Р°Р№РґРµСЂР° РґР»СЏ raycast
        Collider collider = obj.GetComponent<Collider>();
    }
    
    /// <summary>
    /// РћС‡РёСЃС‚РєР° Р»РѕРєР°С†РёРё
    /// </summary>
    public void ClearLocation()
    {
        // РЈРґР°Р»СЏРµРј РІСЃРµ СЃРѕР·РґР°РЅРЅС‹Рµ РѕР±СЉРµРєС‚С‹, РєСЂРѕРјРµ РїРµСЂСЃРѕРЅР°Р¶РµР№
        List<GameObject> charactersToPreserve = new List<GameObject>();
        
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                // РџСЂРѕРІРµСЂСЏРµРј, СЏРІР»СЏРµС‚СЃСЏ Р»Рё РѕР±СЉРµРєС‚ РїРµСЂСЃРѕРЅР°Р¶РµРј
                Character character = obj.GetComponent<Character>();
                if (character != null)
                {
                    // РЎРѕС…СЂР°РЅСЏРµРј РїРµСЂСЃРѕРЅР°Р¶Р°
                    charactersToPreserve.Add(obj);
                }
                else
                {
                    // РЈРґР°Р»СЏРµРј РІСЃРµ РѕСЃС‚Р°Р»СЊРЅС‹Рµ РѕР±СЉРµРєС‚С‹
                    DestroyImmediate(obj);
                }
            }
        }
        
        // РћС‡РёС‰Р°РµРј СЃРїРёСЃРєРё Рё РІРѕСЃСЃС‚Р°РЅР°РІР»РёРІР°РµРј РїРµСЂСЃРѕРЅР°Р¶РµР№
        spawnedObjects.Clear();
        gridObjects.Clear();
        
        // Р”РѕР±Р°РІР»СЏРµРј РїРµСЂСЃРѕРЅР°Р¶РµР№ РѕР±СЂР°С‚РЅРѕ РІ СЃРїРёСЃРѕРє
        foreach (GameObject character in charactersToPreserve)
        {
            spawnedObjects.Add(character);
            
            // Р РµРіРёСЃС‚СЂРёСЂСѓРµРј РїРµСЂСЃРѕРЅР°Р¶Р° РІ СЃРµС‚РєРµ Р·Р°РЅРѕРІРѕ
            Character charComponent = character.GetComponent<Character>();
            if (charComponent != null && gridManager != null)
            {
                Vector2Int gridPos = gridManager.WorldToGrid(character.transform.position);
                gridObjects[gridPos] = character;
            }
        }
        
        // РћС‡РёС‰Р°РµРј СЃРµС‚РєСѓ
        if (gridManager != null)
        {
            gridManager.ClearGrid();
        }
        
        if (currentLocation != null)
        {
            currentLocation.pointsOfInterest.Clear();
            currentLocation.isGenerated = false;
        }
        
        OnLocationCleared?.Invoke();
    }
    
    /// <summary>
    /// РџСЂРµРѕР±СЂР°Р·РѕРІР°РЅРёРµ РјРёСЂРѕРІС‹С… РєРѕРѕСЂРґРёРЅР°С‚ РІ РєРѕРѕСЂРґРёРЅР°С‚С‹ СЃРµС‚РєРё
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - gridCellSize * 0.5f) / gridCellSize);
        int z = Mathf.RoundToInt((worldPosition.z - gridCellSize * 0.5f) / gridCellSize);
        return new Vector2Int(x, z);
    }
    
    /// <summary>
    /// РџСЂРµРѕР±СЂР°Р·РѕРІР°РЅРёРµ РєРѕРѕСЂРґРёРЅР°С‚ СЃРµС‚РєРё РІ РјРёСЂРѕРІС‹Рµ РєРѕРѕСЂРґРёРЅР°С‚С‹ (С†РµРЅС‚СЂ СЏС‡РµР№РєРё)
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * gridCellSize + gridCellSize * 0.5f, 0f, gridPosition.y * gridCellSize + gridCellSize * 0.5f);
    }
    
    /// <summary>
    /// РџСЂРѕРІРµСЂРєР° РІР°Р»РёРґРЅРѕСЃС‚Рё РїРѕР·РёС†РёРё СЃРµС‚РєРё
    /// </summary>
    public bool IsValidGridPosition(Vector2Int gridPosition)
    {
        int halfWidth = currentLocation.gridSize.x / 2;
        int halfHeight = currentLocation.gridSize.y / 2;
        
        return gridPosition.x >= -halfWidth && gridPosition.x < halfWidth &&
               gridPosition.y >= -halfHeight && gridPosition.y < halfHeight;
    }
    
    /// <summary>
    /// РџРѕР»СѓС‡РµРЅРёРµ РІСЃРµС… РѕР±СЉРµРєС‚РѕРІ РѕРїСЂРµРґРµР»РµРЅРЅРѕРіРѕ С‚РёРїР°
    /// </summary>
    public List<GameObject> GetObjectsByType(string objectType)
    {
        List<GameObject> result = new List<GameObject>();
        
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                var objectInfo = obj.GetComponent<LocationObjectInfo>();
                if (objectInfo != null && objectInfo.objectType == objectType)
                {
                    result.Add(obj);
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// РЎРѕС…СЂР°РЅРµРЅРёРµ СЃРѕСЃС‚РѕСЏРЅРёСЏ Р»РѕРєР°С†РёРё
    /// </summary>
    public string SaveLocationState()
    {
        return JsonUtility.ToJson(currentLocation, true);
    }
    
    /// <summary>
    /// Р—Р°РіСЂСѓР·РєР° СЃРѕСЃС‚РѕСЏРЅРёСЏ Р»РѕРєР°С†РёРё
    /// </summary>
    public void LoadLocationState(string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            currentLocation = JsonUtility.FromJson<LocationData>(json);
            // РџРѕСЃР»Рµ Р·Р°РіСЂСѓР·РєРё РјРѕР¶РЅРѕ СЂРµРіРµРЅРµСЂРёСЂРѕРІР°С‚СЊ Р»РѕРєР°С†РёСЋ РµСЃР»Рё РЅСѓР¶РЅРѕ
        }
    }
    
    /// <summary>
    /// Р”РёР°РіРЅРѕСЃС‚РёРєР° СЃРѕР·РґР°РЅРЅС‹С… РѕР±СЉРµРєС‚РѕРІ РґР»СЏ РѕС‚Р»Р°РґРєРё РІС‹РґРµР»РµРЅРёСЏ
    /// </summary>
    void DiagnoseCreatedObjects()
    {
        
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj == null) continue;
            
            
            // РџСЂРѕРІРµСЂСЏРµРј РєРѕР»Р»Р°Р№РґРµСЂС‹
            Collider[] colliders = obj.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (collider is BoxCollider box)
                {
                }
            }
            
            // РџСЂРѕРІРµСЂСЏРµРј LocationObjectInfo
            LocationObjectInfo objectInfo = obj.GetComponent<LocationObjectInfo>();
            if (objectInfo != null)
            {
            }
            
            // РџСЂРѕРІРµСЂСЏРµРј СЃР»РѕР№
        }
        
    }
    
    void OnDrawGizmos()
    {
        if (currentLocation != null)
        {
            // Р РёСЃСѓРµРј РіСЂР°РЅРёС†С‹ Р»РѕРєР°С†РёРё
            Gizmos.color = Color.cyan;
            float halfWidth = (currentLocation.gridSize.x * gridCellSize) * 0.5f;
            float halfHeight = (currentLocation.gridSize.y * gridCellSize) * 0.5f;
            
            Vector3 center = transform.position;
            Vector3 size = new Vector3(halfWidth * 2, 1f, halfHeight * 2);
            Gizmos.DrawWireCube(center, size);
            
            // Р РёСЃСѓРµРј С‚РѕС‡РєСѓ СЃРїР°РІРЅР° РёРіСЂРѕРєР°
            if (currentLocation.isGenerated)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentLocation.playerSpawnPoint, 5f);
                
                // Р РёСЃСѓРµРј С‚РѕС‡РєРё РёРЅС‚РµСЂРµСЃР°
                Gizmos.color = Color.yellow;
                foreach (Vector3 poi in currentLocation.pointsOfInterest)
                {
                    Gizmos.DrawWireSphere(poi, 3f);
                }
            }
        }
    }
}
