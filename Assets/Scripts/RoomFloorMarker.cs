using UnityEngine;

/// <summary>
/// Маркер пола комнаты для связи пола с родительской комнатой
/// Используется для правильного выделения комнат через пол
/// </summary>
public class RoomFloorMarker : MonoBehaviour
{
    [Header("Room Reference")]
    public GameObject parentRoom;

    /// <summary>
    /// Получить родительскую комнату
    /// </summary>
    public GameObject GetParentRoom()
    {
        return parentRoom;
    }

    /// <summary>
    /// Получить информацию о родительской комнате
    /// </summary>
    public RoomInfo GetParentRoomInfo()
    {
        if (parentRoom != null)
        {
            return parentRoom.GetComponent<RoomInfo>();
        }
        return null;
    }
}