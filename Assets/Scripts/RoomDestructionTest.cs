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


        if (buildingSystem == null)
        {

            return;
        }

        // Активируем режим строительства
        buildingSystem.SetBuildMode(true);
        buildingSystem.SetSelectedRoomType(0); // Первый тип комнаты


    }

    void TestSelectRoom()
    {


        if (selectionManager == null)
        {

            return;
        }

        // Ищем комнату в сцене
        RoomInfo[] rooms = FindObjectsOfType<RoomInfo>();

        if (rooms.Length > 0)
        {
            GameObject firstRoom = rooms[0].gameObject;


            // Очищаем выделение и выделяем комнату
            selectionManager.ClearSelection();
            selectionManager.AddToSelection(firstRoom);
        }
        else
        {

        }
    }

    void TestDestroyRoom()
    {


        if (selectionManager == null || buildingSystem == null)
        {

            return;
        }

        // Получаем выделенные объекты
        var selectedObjects = selectionManager.GetSelectedObjects();

        foreach (GameObject obj in selectedObjects)
        {
            RoomInfo roomInfo = obj.GetComponent<RoomInfo>();
            if (roomInfo != null)
            {

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