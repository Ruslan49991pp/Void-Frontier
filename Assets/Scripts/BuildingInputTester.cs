using UnityEngine;

/// <summary>
/// Тестер для проверки нового поведения ПКМ в режиме строительства
/// </summary>
public class BuildingInputTester : MonoBehaviour
{
    [Header("Test Keys")]
    public KeyCode enterBuildModeKey = KeyCode.B;
    public KeyCode selectRoom1Key = KeyCode.Alpha1;
    public KeyCode selectRoom2Key = KeyCode.Alpha2;
    public KeyCode selectRoom3Key = KeyCode.Alpha3;
    public KeyCode clearSelectionKey = KeyCode.C;

    private ShipBuildingSystem buildingSystem;
    private GameUI gameUI;

    void Start()
    {
        buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        gameUI = FindObjectOfType<GameUI>();

        Debug.Log("BuildingInputTester: Initialized");
        Debug.Log($"Building System: {(buildingSystem != null ? "Found" : "Not Found")}");
        Debug.Log($"Game UI: {(gameUI != null ? "Found" : "Not Found")}");
    }

    void Update()
    {
        // Тест входа в режим строительства
        if (Input.GetKeyDown(enterBuildModeKey))
        {
            TestEnterBuildMode();
        }

        // Тест выбора комнат
        if (Input.GetKeyDown(selectRoom1Key))
        {
            TestSelectRoom(0);
        }
        if (Input.GetKeyDown(selectRoom2Key))
        {
            TestSelectRoom(1);
        }
        if (Input.GetKeyDown(selectRoom3Key))
        {
            TestSelectRoom(2);
        }

        // Тест очистки выбора
        if (Input.GetKeyDown(clearSelectionKey))
        {
            TestClearSelection();
        }
    }

    void TestEnterBuildMode()
    {
        if (buildingSystem == null) return;

        buildingSystem.SetBuildMode(true);
        Debug.Log("Build mode activated");
    }

    void TestSelectRoom(int roomIndex)
    {
        if (buildingSystem == null) return;

        if (buildingSystem.IsBuildingModeActive())
        {
            buildingSystem.SetSelectedRoomType(roomIndex);
            Debug.Log($"Room type {roomIndex} selected");
        }
        else
        {
            Debug.Log("Build mode is not active");
        }
    }

    void TestClearSelection()
    {
        if (buildingSystem == null) return;

        if (buildingSystem.IsBuildingModeActive())
        {
            buildingSystem.ClearRoomSelection();
            Debug.Log("Room selection cleared");
        }
        else
        {
            Debug.Log("Build mode is not active");
        }
    }

    void OnGUI()
    {
        // Отображаем инструкции и состояние
        int yPos = 200;
        GUI.Label(new Rect(10, yPos, 400, 20), $"Press {enterBuildModeKey} to enter build mode");
        GUI.Label(new Rect(10, yPos + 20, 400, 20), $"Press {selectRoom1Key}/{selectRoom2Key}/{selectRoom3Key} to select room type");
        GUI.Label(new Rect(10, yPos + 40, 400, 20), $"Press {clearSelectionKey} to clear selection");
        GUI.Label(new Rect(10, yPos + 60, 400, 20), "Right Click: Clear selection or exit build mode");
        GUI.Label(new Rect(10, yPos + 80, 400, 20), "ESC: Exit build mode");

        if (buildingSystem != null)
        {
            GUI.Label(new Rect(10, yPos + 110, 400, 20), $"Build Mode Active: {buildingSystem.IsBuildingModeActive()}");
            GUI.Label(new Rect(10, yPos + 130, 400, 20), $"Selected Room Index: {buildingSystem.GetSelectedRoomIndex()}");

            var currentRoom = buildingSystem.GetCurrentRoomType();
            if (currentRoom != null)
            {
                GUI.Label(new Rect(10, yPos + 150, 400, 20), $"Selected Room: {currentRoom.roomName}");
            }
            else
            {
                GUI.Label(new Rect(10, yPos + 150, 400, 20), "Selected Room: None");
            }
        }
    }
}