using UnityEngine;

/// <summary>
/// Тестовый скрипт для проверки функциональности разрушения комнат
/// </summary>
public class RoomDestructionTest : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode testBuildRoomKey = KeyCode.B;
    public KeyCode testSelectRoomKey = KeyCode.R;
    public KeyCode testDestroyRoomKey = KeyCode.X;

    private ShipBuildingSystem buildingSystem;
    private SelectionManager selectionManager;
    private GameUI gameUI;

    void Start()
    {
        buildingSystem = FindObjectOfType<ShipBuildingSystem>();
        selectionManager = FindObjectOfType<SelectionManager>();
        gameUI = FindObjectOfType<GameUI>();

        Debug.Log("RoomDestructionTest: Initialized");
        Debug.Log($"Building System: {(buildingSystem != null ? "Found" : "Not Found")}");
        Debug.Log($"Selection Manager: {(selectionManager != null ? "Found" : "Not Found")}");
        Debug.Log($"Game UI: {(gameUI != null ? "Found" : "Not Found")}");
    }

    void Update()
    {
        // Тест построения комнаты
        if (Input.GetKeyDown(testBuildRoomKey))
        {
            TestBuildRoom();
        }

        // Тест выделения комнаты
        if (Input.GetKeyDown(testSelectRoomKey))
        {
            TestSelectRoom();
        }

        // Тест разрушения комнаты
        if (Input.GetKeyDown(testDestroyRoomKey))
        {
            TestDestroyRoom();
        }
    }

    void TestBuildRoom()
    {
        Debug.Log("Testing room building...");

        if (buildingSystem == null)
        {
            Debug.LogError("Building system not found!");
            return;
        }

        // Активируем режим строительства
        buildingSystem.SetBuildMode(true);
        buildingSystem.SetSelectedRoomType(0); // Первый тип комнаты

        Debug.Log("Build mode activated. Click to place room.");
    }

    void TestSelectRoom()
    {
        Debug.Log("Testing room selection...");

        if (selectionManager == null)
        {
            Debug.LogError("Selection manager not found!");
            return;
        }

        // Ищем комнату в сцене
        RoomInfo[] rooms = FindObjectsOfType<RoomInfo>();

        if (rooms.Length > 0)
        {
            GameObject firstRoom = rooms[0].gameObject;
            Debug.Log($"Selecting room: {firstRoom.name}");

            // Очищаем выделение и выделяем комнату
            selectionManager.ClearSelection();
            selectionManager.AddToSelection(firstRoom);
        }
        else
        {
            Debug.LogWarning("No rooms found in scene!");
        }
    }

    void TestDestroyRoom()
    {
        Debug.Log("Testing room destruction...");

        if (selectionManager == null || buildingSystem == null)
        {
            Debug.LogError("Required systems not found!");
            return;
        }

        // Получаем выделенные объекты
        var selectedObjects = selectionManager.GetSelectedObjects();

        foreach (GameObject obj in selectedObjects)
        {
            RoomInfo roomInfo = obj.GetComponent<RoomInfo>();
            if (roomInfo != null)
            {
                Debug.Log($"Destroying room: {roomInfo.roomName}");
                buildingSystem.DeleteRoom(obj);
                break;
            }
        }
    }

    void OnGUI()
    {
        // Отображаем инструкции на экране
        GUI.Label(new Rect(10, 10, 300, 20), $"Press {testBuildRoomKey} to test building");
        GUI.Label(new Rect(10, 30, 300, 20), $"Press {testSelectRoomKey} to test selection");
        GUI.Label(new Rect(10, 50, 300, 20), $"Press {testDestroyRoomKey} to test destruction");

        if (buildingSystem != null)
        {
            GUI.Label(new Rect(10, 80, 300, 20), $"Build mode: {buildingSystem.IsBuildingModeActive()}");
        }

        if (selectionManager != null)
        {
            var selectedObjects = selectionManager.GetSelectedObjects();
            GUI.Label(new Rect(10, 100, 300, 20), $"Selected objects: {selectedObjects.Count}");
        }
    }
}