using UnityEngine;

/// <summary>
/// Временный скрипт для скрытия ActionArea панели строительства
/// </summary>
public class HideActionArea : MonoBehaviour
{
    void Start()
    {
        // Ищем ActionArea через некоторое время после старта
        Invoke(nameof(FindAndHideActionArea), 0.1f);
    }

    void FindAndHideActionArea()
    {
        // Ищем все GameObject с именем "ActionArea"
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "ActionArea")
            {
                obj.SetActive(false);
            }
        }
    }

    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
