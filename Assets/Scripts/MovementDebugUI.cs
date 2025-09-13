using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Debug UI для отслеживания состояния движения персонажей
/// </summary>
public class MovementDebugUI : MonoBehaviour
{
    private static MovementDebugUI instance;
    private MovementController movementController;
    private Rect windowRect = new Rect(Screen.width - 400, 10, 380, 500);
    private Vector2 scrollPosition = Vector2.zero;
    private bool showWindow = true;
    private string debugLog = "";
    
    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("MovementDebugUI: Debug window initialized. Press F1 to toggle.");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Убеждаемся что окно показывается в правильной позиции
        windowRect = new Rect(Screen.width - 400, 10, 380, 500);
        
        // Ищем MovementController с повторными попытками
        StartCoroutine(FindMovementController());
    }
    
    IEnumerator FindMovementController()
    {
        // Даем время сцене загрузиться и попробуем найти MovementController
        for (int attempts = 0; attempts < 10; attempts++)
        {
            movementController = FindObjectOfType<MovementController>();
            if (movementController != null)
            {
                Debug.Log("MovementDebugUI: MovementController найден!");
                yield break;
            }
            
            Debug.Log($"MovementDebugUI: попытка найти MovementController #{attempts + 1}");
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.LogWarning("MovementDebugUI: MovementController не найден после 10 попыток!");
    }
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        // Автоматически создаем дебаг окно при старте игры
        if (instance == null)
        {
            GameObject debugGO = new GameObject("MovementDebugUI");
            debugGO.AddComponent<MovementDebugUI>();
            Debug.Log("MovementDebugUI: Auto-created debug window. Press F1 to toggle.");
        }
    }
    
    void Update()
    {
        // Переключение окна по F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showWindow = !showWindow;
            Debug.Log($"MovementDebugUI: Debug window {(showWindow ? "SHOWN" : "HIDDEN")}");
        }
        
        // Дополнительная проверка на случай если F1 не работает
        if (Input.GetKeyDown(KeyCode.F2))
        {
            showWindow = true;
            Debug.Log("MovementDebugUI: Force showing debug window with F2");
        }
    }
    
    void OnGUI()
    {
        if (!showWindow) return;
        
        // Убеждаемся что позиция окна корректна
        if (windowRect.x < 0 || windowRect.x > Screen.width - 200)
            windowRect.x = Screen.width - 400;
        if (windowRect.y < 0 || windowRect.y > Screen.height - 100)
            windowRect.y = 10;
            
        windowRect = GUI.Window(0, windowRect, DrawDebugWindow, "Movement Debug");
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    
    void DrawDebugWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // Заголовок
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.fontSize = 14;
        
        GUILayout.Label("Movement Debug (F1 to toggle)", headerStyle);
        
        // Кнопка копирования логов
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Debug Info", GUILayout.Width(120)))
        {
            CopyDebugInfoToClipboard();
        }
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            debugLog = "";
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        // Информация о персонажах
        DrawCharactersInfo();
        
        GUILayout.Space(10);
        
        // Информация о группах движения
        DrawMovementGroupsInfo();
        
        GUILayout.Space(10);
        
        // Общая статистика
        DrawGeneralStats();
        
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        
        // Делаем окно перетаскиваемым
        GUI.DragWindow();
    }
    
    void DrawCharactersInfo()
    {
        GUIStyle sectionStyle = new GUIStyle(GUI.skin.label);
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.normal.textColor = Color.cyan;
        
        GUILayout.Label("=== ПЕРСОНАЖИ ===", sectionStyle);
        
        CharacterMovement[] allMovements = FindObjectsOfType<CharacterMovement>();
        
        if (allMovements.Length == 0)
        {
            GUILayout.Label("Нет активных персонажей", GUI.skin.box);
            return;
        }
        
        foreach (var movement in allMovements)
        {
            if (movement == null) continue;
            
            GUILayout.BeginVertical(GUI.skin.box);
            
            // Основная информация
            string movingStatus = movement.IsMoving() ? "ДВИЖЕТСЯ" : "СТОИТ";
            Color statusColor = movement.IsMoving() ? Color.green : Color.red;
            
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.normal.textColor = statusColor;
            statusStyle.fontStyle = FontStyle.Bold;
            
            GUILayout.Label($"{movement.name}: {movingStatus}", statusStyle);
            
            // Позиция
            Vector3 pos = movement.transform.position;
            GUILayout.Label($"Позиция: ({pos.x:F1}, {pos.z:F1})");
            
            // Информация о текущем пути (используем рефлексию для доступа к приватным полям)
            var targetField = typeof(CharacterMovement).GetField("targetPosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pathField = typeof(CharacterMovement).GetField("currentPath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var indexField = typeof(CharacterMovement).GetField("currentPathIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var coroutineField = typeof(CharacterMovement).GetField("movementCoroutine", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (targetField != null)
            {
                Vector3 target = (Vector3)targetField.GetValue(movement);
                GUILayout.Label($"Цель: ({target.x:F1}, {target.z:F1})");
            }
            
            if (pathField != null)
            {
                var path = pathField.GetValue(movement) as List<Vector2Int>;
                if (path != null && path.Count > 0)
                {
                    GUILayout.Label($"Путь: {path.Count} точек");
                    
                    if (indexField != null)
                    {
                        int index = (int)indexField.GetValue(movement);
                        GUILayout.Label($"Текущая точка: {index}/{path.Count}");
                    }
                }
                else
                {
                    GUILayout.Label("Путь: НЕТ");
                }
            }
            
            if (coroutineField != null)
            {
                var coroutine = coroutineField.GetValue(movement);
                string coroutineStatus = coroutine != null ? "АКТИВНА" : "НЕТ";
                GUILayout.Label($"Корутина: {coroutineStatus}");
            }
            
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }
    
    void DrawMovementGroupsInfo()
    {
        GUIStyle sectionStyle = new GUIStyle(GUI.skin.label);
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.normal.textColor = Color.yellow;
        
        GUILayout.Label("=== ГРУППЫ ДВИЖЕНИЯ ===", sectionStyle);
        
        if (movementController == null)
        {
            GUILayout.Label("MovementController не найден!", GUI.skin.box);
            return;
        }
        
        // Получаем информацию о группах через рефлексию
        var groupsField = typeof(MovementController).GetField("movingGroups", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (groupsField != null)
        {
            var groups = groupsField.GetValue(movementController) as System.Collections.IDictionary;
            
            if (groups == null || groups.Count == 0)
            {
                GUILayout.Label("Активных групп нет", GUI.skin.box);
                return;
            }
            
            foreach (System.Collections.DictionaryEntry kvp in groups)
            {
                Vector2Int targetPos = (Vector2Int)kvp.Key;
                var group = kvp.Value;
                
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"Цель: {targetPos}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                
                // Получаем информацию о группе
                var groupType = group.GetType();
                var allCharsField = groupType.GetField("allCharacters");
                var arrivedCharsField = groupType.GetField("arrivedCharacters");
                var targetOccupiedField = groupType.GetField("targetOccupied");
                
                if (allCharsField != null && arrivedCharsField != null)
                {
                    var allChars = allCharsField.GetValue(group) as List<CharacterMovement>;
                    var arrivedChars = arrivedCharsField.GetValue(group) as List<CharacterMovement>;
                    
                    GUILayout.Label($"Всего персонажей: {allChars?.Count ?? 0}");
                    GUILayout.Label($"Прибыло: {arrivedChars?.Count ?? 0}");
                    
                    if (targetOccupiedField != null)
                    {
                        bool occupied = (bool)targetOccupiedField.GetValue(group);
                        GUILayout.Label($"Цель занята: {(occupied ? "ДА" : "НЕТ")}");
                    }
                    
                    // Список персонажей в группе
                    if (allChars != null)
                    {
                        foreach (var character in allChars)
                        {
                            if (character != null)
                            {
                                bool hasArrived = arrivedChars?.Contains(character) ?? false;
                                string status = hasArrived ? "✓" : "→";
                                GUILayout.Label($"  {status} {character.name}");
                            }
                        }
                    }
                }
                
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }
    }
    
    void DrawGeneralStats()
    {
        GUIStyle sectionStyle = new GUIStyle(GUI.skin.label);
        sectionStyle.fontStyle = FontStyle.Bold;
        sectionStyle.normal.textColor = Color.magenta;
        
        GUILayout.Label("=== ОБЩАЯ СТАТИСТИКА ===", sectionStyle);
        
        GUILayout.BeginVertical(GUI.skin.box);
        
        // Количество персонажей
        CharacterMovement[] allMovements = FindObjectsOfType<CharacterMovement>();
        int movingCount = allMovements.Count(m => m != null && m.IsMoving());
        int stoppedCount = allMovements.Length - movingCount;
        
        GUILayout.Label($"Всего персонажей: {allMovements.Length}");
        GUILayout.Label($"Движется: {movingCount}");
        GUILayout.Label($"Стоит: {stoppedCount}");
        
        // Время
        GUILayout.Label($"Время: {Time.time:F1}");
        GUILayout.Label($"FPS: {(1f / Time.unscaledDeltaTime):F0}");
        
        GUILayout.EndVertical();
    }
    
    void CopyDebugInfoToClipboard()
    {
        debugLog = GenerateDebugReport();
        GUIUtility.systemCopyBuffer = debugLog;
        Debug.Log("Debug info copied to clipboard!");
    }
    
    string GenerateDebugReport()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("=== MOVEMENT DEBUG REPORT ===");
        report.AppendLine($"Time: {System.DateTime.Now:HH:mm:ss}");
        report.AppendLine($"Game Time: {Time.time:F1}");
        report.AppendLine();
        
        // Персонажи
        report.AppendLine("=== ПЕРСОНАЖИ ===");
        CharacterMovement[] allMovements = FindObjectsOfType<CharacterMovement>();
        
        if (allMovements.Length == 0)
        {
            report.AppendLine("Нет активных персонажей");
        }
        else
        {
            foreach (var movement in allMovements)
            {
                if (movement == null) continue;
                
                report.AppendLine($"Персонаж: {movement.name}");
                report.AppendLine($"  Движется: {(movement.IsMoving() ? "ДА" : "НЕТ")}");
                
                Vector3 pos = movement.transform.position;
                report.AppendLine($"  Позиция: ({pos.x:F2}, {pos.z:F2})");
                
                // Получаем приватные поля через рефлексию
                var targetField = typeof(CharacterMovement).GetField("targetPosition", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var pathField = typeof(CharacterMovement).GetField("currentPath", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var indexField = typeof(CharacterMovement).GetField("currentPathIndex", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var coroutineField = typeof(CharacterMovement).GetField("movementCoroutine", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (targetField != null)
                {
                    Vector3 target = (Vector3)targetField.GetValue(movement);
                    report.AppendLine($"  Цель: ({target.x:F2}, {target.z:F2})");
                }
                
                if (pathField != null)
                {
                    var path = pathField.GetValue(movement) as List<Vector2Int>;
                    if (path != null && path.Count > 0)
                    {
                        report.AppendLine($"  Путь: {path.Count} точек");
                        if (indexField != null)
                        {
                            int index = (int)indexField.GetValue(movement);
                            report.AppendLine($"  Текущая точка пути: {index}/{path.Count}");
                        }
                    }
                    else
                    {
                        report.AppendLine("  Путь: НЕТ");
                    }
                }
                
                if (coroutineField != null)
                {
                    var coroutine = coroutineField.GetValue(movement);
                    report.AppendLine($"  Корутина: {(coroutine != null ? "АКТИВНА" : "НЕТ")}");
                }
                
                report.AppendLine();
            }
        }
        
        // Группы движения
        report.AppendLine("=== ГРУППЫ ДВИЖЕНИЯ ===");
        if (movementController == null)
        {
            report.AppendLine("MovementController не найден!");
        }
        else
        {
            var groupsField = typeof(MovementController).GetField("movingGroups", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (groupsField != null)
            {
                var groups = groupsField.GetValue(movementController) as System.Collections.IDictionary;
                
                if (groups == null || groups.Count == 0)
                {
                    report.AppendLine("Активных групп нет");
                }
                else
                {
                    foreach (System.Collections.DictionaryEntry kvp in groups)
                    {
                        Vector2Int targetPos = (Vector2Int)kvp.Key;
                        var group = kvp.Value;
                        
                        report.AppendLine($"Группа к цели: {targetPos}");
                        
                        var groupType = group.GetType();
                        var allCharsField = groupType.GetField("allCharacters");
                        var arrivedCharsField = groupType.GetField("arrivedCharacters");
                        var targetOccupiedField = groupType.GetField("targetOccupied");
                        
                        if (allCharsField != null && arrivedCharsField != null)
                        {
                            var allChars = allCharsField.GetValue(group) as List<CharacterMovement>;
                            var arrivedChars = arrivedCharsField.GetValue(group) as List<CharacterMovement>;
                            
                            report.AppendLine($"  Всего персонажей: {allChars?.Count ?? 0}");
                            report.AppendLine($"  Прибыло: {arrivedChars?.Count ?? 0}");
                            
                            if (targetOccupiedField != null)
                            {
                                bool occupied = (bool)targetOccupiedField.GetValue(group);
                                report.AppendLine($"  Цель занята: {(occupied ? "ДА" : "НЕТ")}");
                            }
                            
                            if (allChars != null)
                            {
                                report.AppendLine("  Персонажи:");
                                foreach (var character in allChars)
                                {
                                    if (character != null)
                                    {
                                        bool hasArrived = arrivedChars?.Contains(character) ?? false;
                                        string status = hasArrived ? "ПРИБЫЛ" : "В ПУТИ";
                                        report.AppendLine($"    - {character.name}: {status}");
                                    }
                                }
                            }
                        }
                        report.AppendLine();
                    }
                }
            }
        }
        
        // Общая статистика
        report.AppendLine("=== СТАТИСТИКА ===");
        int movingCount = allMovements.Count(m => m != null && m.IsMoving());
        int stoppedCount = allMovements.Length - movingCount;
        
        report.AppendLine($"Всего персонажей: {allMovements.Length}");
        report.AppendLine($"Движется: {movingCount}");
        report.AppendLine($"Стоит: {stoppedCount}");
        report.AppendLine($"FPS: {(1f / Time.unscaledDeltaTime):F0}");
        
        return report.ToString();
    }
}