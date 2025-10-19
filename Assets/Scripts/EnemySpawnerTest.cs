using UnityEngine;

/// <summary>
/// РўРµСЃС‚РѕРІС‹Р№ СЃРєСЂРёРїС‚ РґР»СЏ СЃРѕР·РґР°РЅРёСЏ РІСЂР°РіРѕРІ РґР»СЏ С‚РµСЃС‚РёСЂРѕРІР°РЅРёСЏ СЃРёСЃС‚РµРјС‹ СѓРєР°Р·Р°РЅРёСЏ С†РµР»РµР№
/// </summary>
public class EnemySpawnerTest : MonoBehaviour
{
    [Header("Enemy Spawn Settings")]
    public int enemyCount = 3;
    public float spawnRadius = 8f; // Р Р°РґРёСѓСЃ СЂР°Р·Р±СЂРѕСЃР° СЃРїР°РІРЅР°
    public Vector3 spawnCenter = new Vector3(25, 0, 25); // РЎРїР°РІРЅ РґР°Р»РµРєРѕ РѕС‚ РёРіСЂРѕРєРѕРІ (РІРЅРµ СЂР°РґРёСѓСЃР° РѕР±РЅР°СЂСѓР¶РµРЅРёСЏ 10 РєР»РµС‚РѕРє)

    [Header("Enemy Prefab Settings")]
    public Material enemyMaterial; // Р•СЃР»Рё РЅРµ СѓСЃС‚Р°РЅРѕРІР»РµРЅ, Р±СѓРґРµС‚ Р·Р°РіСЂСѓР¶РµРЅ M_Enemy РёР· Resources
    public Color enemyColor = Color.red;

    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        // РЎРѕР·РґР°РµРј РІСЂР°РіРѕРІ С‡РµСЂРµР· РЅРµР±РѕР»СЊС€СѓСЋ Р·Р°РґРµСЂР¶РєСѓ, С‡С‚РѕР±С‹ GridManager СѓСЃРїРµР» РёРЅРёС†РёР°Р»РёР·РёСЂРѕРІР°С‚СЊСЃСЏ
        Invoke(nameof(SpawnEnemies), 1f);
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ С‚РµСЃС‚РѕРІС‹С… РІСЂР°РіРѕРІ
    /// </summary>
    public void SpawnEnemies()
    {


        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemy = CreateEnemyCharacter($"Enemy_{i + 1}");

            // Р Р°Р·РјРµС‰Р°РµРј РІСЂР°РіР° РІ СЃР»СѓС‡Р°Р№РЅРѕР№ РїРѕР·РёС†РёРё
            Vector3 spawnPosition = GetRandomSpawnPosition();
            enemy.transform.position = spawnPosition;


        }
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РІСЂР°РіР°-РїРµСЂСЃРѕРЅР°Р¶Р°
    /// </summary>
    GameObject CreateEnemyCharacter(string enemyName)
    {


        GameObject enemy;

        // Р—Р°РіСЂСѓР¶Р°РµРј РїСЂРµС„Р°Р± SKM_Character РёР· Resources
        GameObject characterPrefab = Resources.Load<GameObject>("Prefabs/SKM_Character");
        if (characterPrefab == null)
        {

            // Fallback Рє РєР°РїСЃСѓР»Рµ РµСЃР»Рё РїСЂРµС„Р°Р± РЅРµ РЅР°Р№РґРµРЅ
            enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = enemyName;
            enemy.transform.localScale = Vector3.one;
        }
        else
        {
            // РЎРѕР·РґР°РµРј РІСЂР°РіР° РёР· РїСЂРµС„Р°Р±Р° SKM_Character
            enemy = Instantiate(characterPrefab);
            enemy.name = enemyName;

        }

        SetupEnemyCharacter(enemy, enemyName);
        return enemy;
    }

    /// <summary>
    /// РќР°СЃС‚СЂРѕРёС‚СЊ GameObject РєР°Рє РІСЂР°РіР°
    /// </summary>
    void SetupEnemyCharacter(GameObject enemy, string enemyName)
    {
        // РџРѕР»СѓС‡Р°РµРј РёР»Рё РґРѕР±Р°РІР»СЏРµРј РєРѕРјРїРѕРЅРµРЅС‚ Character
        Character character = enemy.GetComponent<Character>();
        if (character == null)
        {
            character = enemy.AddComponent<Character>();

        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј РєР°Рє РІСЂР°РіР°
        character.characterData = new CharacterData();

        // Р“РµРЅРµСЂРёСЂСѓРµРј СЃР»СѓС‡Р°Р№РЅС‹Рµ РёРјРµРЅР° РґР»СЏ РІСЂР°РіРѕРІ
        string[] enemyFirstNames = { "Viktor", "Igor", "Boris", "Alexei", "Dmitri", "Sergei", "Pavel", "Nikolai", "Anton", "Maksim" };
        string[] enemyLastNames = { "Volkov", "Petrov", "Kozlov", "Morozov", "Smirnov", "Popov", "Lebedev", "Novikov", "Fedorov", "Orlov" };

        character.characterData.firstName = enemyFirstNames[Random.Range(0, enemyFirstNames.Length)];
        character.characterData.lastName = enemyLastNames[Random.Range(0, enemyLastNames.Length)];
        character.characterData.faction = Faction.Enemy;
        character.characterData.profession = "Hostile Unit";
        character.characterData.level = Random.Range(1, 5);
        character.characterData.maxHealth = 100f;
        character.characterData.health = character.characterData.maxHealth;
        character.characterData.bio = $"Hostile {character.characterData.profession} - Level {character.characterData.level}";

        // РќР°СЃС‚СЂР°РёРІР°РµРј С†РІРµС‚Р° РґР»СЏ РІСЂР°РіР°
        character.defaultColor = enemyColor;
        character.selectedColor = Color.red;
        character.hoverColor = Color.yellow;

        // РќР°С…РѕРґРёРј Рё РЅР°СЃС‚СЂР°РёРІР°РµРј Р’РЎР• СЂРµРЅРґРµСЂРµСЂС‹
        MeshRenderer[] allRenderers = enemy.GetComponentsInChildren<MeshRenderer>();
        Renderer primaryRenderer = null;



        if (allRenderers.Length > 0)
        {
            // РџСЂРёРѕСЂРёС‚РµС‚: 1) enemyMaterial РёР· Inspector'Р°, 2) GhostRed РёР· Resources, 3) СЃРѕР·РґР°РЅРёРµ РЅР° Р»РµС‚Сѓ
            Material enemyMat = enemyMaterial != null ? enemyMaterial : CreateEnemyMaterial();

            foreach (MeshRenderer renderer in allRenderers)
            {


                renderer.material = enemyMat;

                if (primaryRenderer == null)
                {
                    primaryRenderer = renderer;
                }
            }

            // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РѕСЃРЅРѕРІРЅРѕР№ СЂРµРЅРґРµСЂРµСЂ РґР»СЏ Character
            character.characterRenderer = primaryRenderer;

        }
        else
        {
            // РџРѕРїСЂРѕР±СѓРµРј РЅР°Р№С‚Рё Р»СЋР±С‹Рµ РґСЂСѓРіРёРµ СЂРµРЅРґРµСЂРµСЂС‹
            Renderer[] anyRenderers = enemy.GetComponentsInChildren<Renderer>();


            if (anyRenderers.Length > 0)
            {
                Material enemyMat = enemyMaterial != null ? enemyMaterial : CreateEnemyMaterial();

                foreach (Renderer renderer in anyRenderers)
                {

                    renderer.material = enemyMat;

                    if (primaryRenderer == null)
                    {
                        primaryRenderer = renderer;
                    }
                }

                character.characterRenderer = primaryRenderer;
            }
            else
            {

            }
        }

        // РЈР±РµР¶РґР°РµРјСЃСЏ С‡С‚Рѕ РµСЃС‚СЊ РєРѕР»Р»Р°Р№РґРµСЂ РґР»СЏ СЂР°СЃРєР°СЃС‚РѕРІ
        Collider collider = enemy.GetComponent<Collider>();
        if (collider == null)
        {
            collider = enemy.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                collider = enemy.AddComponent<CapsuleCollider>();

            }
        }

        // Р”РѕР±Р°РІР»СЏРµРј РґРІРёР¶РµРЅРёРµ (РЅРѕ РЅРµ AI - РІСЂР°РіРё РїРѕРєР° СЃС‚Р°С‚РёС‡РЅС‹Рµ)
        CharacterMovement movement = enemy.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            movement = enemy.AddComponent<CharacterMovement>();
            movement.debugMovement = false; // РћС‚РєР»СЋС‡Р°РµРј РґРµР±Р°Рі РґР»СЏ РІСЂР°РіРѕРІ
        }

        // Р”РѕР±Р°РІР»СЏРµРј LocationObjectInfo РґР»СЏ СЃРёСЃС‚РµРјС‹ РІС‹РґРµР»РµРЅРёСЏ
        LocationObjectInfo objectInfo = enemy.GetComponent<LocationObjectInfo>();
        if (objectInfo == null)
        {
            objectInfo = enemy.AddComponent<LocationObjectInfo>();
        }
        objectInfo.objectType = "Character";
        objectInfo.objectName = character.GetFullName();
        objectInfo.health = character.characterData.health;

        // РЈСЃС‚Р°РЅР°РІР»РёРІР°РµРј РїСЂР°РІРёР»СЊРЅС‹Р№ СЃР»РѕР№ РґР»СЏ РІР·Р°РёРјРѕРґРµР№СЃС‚РІРёСЏ СЃ SelectionManager
        enemy.layer = LayerMask.NameToLayer("Default");


    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РЅР°РґРµР¶РЅС‹Р№ РјР°С‚РµСЂРёР°Р» РґР»СЏ РІСЂР°РіР°
    /// </summary>
    Material CreateEnemyMaterial()
    {
        // РЎРЅР°С‡Р°Р»Р° РїС‹С‚Р°РµРјСЃСЏ Р·Р°РіСЂСѓР·РёС‚СЊ РіРѕС‚РѕРІС‹Р№ РєСЂР°СЃРЅС‹Р№ РјР°С‚РµСЂРёР°Р» РґР»СЏ РІСЂР°РіРѕРІ
        Material enemyMat = Resources.Load<Material>("Materials/M_Enemy");
        if (enemyMat != null)
        {

            return enemyMat;
        }


        Material mat = null;

        // РџСЂРѕР±СѓРµРј СЂР°Р·Р»РёС‡РЅС‹Рµ С€РµР№РґРµСЂС‹ РІ РїРѕСЂСЏРґРєРµ РїСЂРµРґРїРѕС‡С‚РµРЅРёСЏ
        string[] shaderNames = {
            "Standard",
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Simple Lit",
            "Legacy Shaders/Diffuse",
            "Legacy Shaders/VertexLit",
            "Unlit/Color",
            "Sprites/Default"
        };

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null && shader.name != "Hidden/InternalErrorShader")
            {
                mat = new Material(shader);

                break;
            }
        }

        // Р•СЃР»Рё РЅРёС‡РµРіРѕ РЅРµ РЅР°С€Р»Рё, СЃРѕР·РґР°РµРј СЃ Р±Р°Р·РѕРІС‹Рј РєРѕРЅСЃС‚СЂСѓРєС‚РѕСЂРѕРј
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse"));
        }

        // РќР°СЃС‚СЂР°РёРІР°РµРј РјР°С‚РµСЂРёР°Р»
        mat.color = enemyColor;
        mat.name = "EnemyMaterial_Generated";

        // Р”РѕРїРѕР»РЅРёС‚РµР»СЊРЅС‹Рµ РЅР°СЃС‚СЂРѕР№РєРё РґР»СЏ РІРёРґРёРјРѕСЃС‚Рё
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetColor("_Color", enemyColor);
        }
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", enemyColor);
        }


        return mat;
    }

    /// <summary>
    /// РџРѕР»СѓС‡РёС‚СЊ СЃР»СѓС‡Р°Р№РЅСѓСЋ РїРѕР·РёС†РёСЋ РґР»СЏ СЃРїР°РІРЅР° РІСЂР°РіР°
    /// </summary>
    Vector3 GetRandomSpawnPosition()
    {
        if (gridManager != null)
        {
            // РџС‹С‚Р°РµРјСЃСЏ РЅР°Р№С‚Рё СЃРІРѕР±РѕРґРЅСѓСЋ РєР»РµС‚РєСѓ РЅР° СЃРµС‚РєРµ
            for (int attempts = 0; attempts < 20; attempts++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                Vector3 worldPos = spawnCenter + new Vector3(randomOffset.x, 0, randomOffset.y);

                Vector2Int gridPos = gridManager.WorldToGrid(worldPos);

                if (gridManager.IsValidGridPosition(gridPos))
                {
                    var cell = gridManager.GetCell(gridPos);
                    if (cell == null || !cell.isOccupied)
                    {
                        return gridManager.GridToWorld(gridPos);
                    }
                }
            }
        }

        // Fallback: СЃР»СѓС‡Р°Р№РЅР°СЏ РїРѕР·РёС†РёСЏ РІРѕРєСЂСѓРі С†РµРЅС‚СЂР° СЃРїР°РІРЅР°
        Vector2 fallbackOffset = Random.insideUnitCircle * spawnRadius;
        return spawnCenter + new Vector3(fallbackOffset.x, 0, fallbackOffset.y);
    }

    /// <summary>
    /// РЎРѕР·РґР°С‚СЊ РґРѕРїРѕР»РЅРёС‚РµР»СЊРЅРѕРіРѕ РІСЂР°РіР° (РґР»СЏ С‚РµСЃС‚РёСЂРѕРІР°РЅРёСЏ РІ runtime)
    /// </summary>
    public void SpawnAdditionalEnemy()
    {
        GameObject enemy = CreateEnemyCharacter($"Enemy_Additional_{Random.Range(100, 999)}");
        Vector3 spawnPosition = GetRandomSpawnPosition();
        enemy.transform.position = spawnPosition;


    }

    void OnDrawGizmosSelected()
    {
        // РџРѕРєР°Р·С‹РІР°РµРј РѕР±Р»Р°СЃС‚СЊ СЃРїР°РІРЅР° РІСЂР°РіРѕРІ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnCenter, Vector3.one * 0.5f);
    }
}
