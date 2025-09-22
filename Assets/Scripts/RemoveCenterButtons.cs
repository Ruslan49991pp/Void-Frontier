using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Скрипт для удаления всех кнопок Center в сцене
/// </summary>
public class RemoveCenterButtons : MonoBehaviour
{
    void Start()
    {
        RemoveAllCenterButtons();

        // Самоуничтожаемся после работы
        Destroy(gameObject);
    }

    void RemoveAllCenterButtons()
    {
        // Находим все кнопки в сцене
        Button[] allButtons = FindObjectsOfType<Button>(true);

        foreach (Button button in allButtons)
        {
            // Проверяем название кнопки
            if (button.name.Contains("Center") || button.name.Contains("CENTER") || button.name.Contains("center"))
            {

                DestroyImmediate(button.gameObject);
                continue;
            }

            // Проверяем текст кнопки
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                string text = buttonText.text.ToLower();
                if (text.Contains("center") || text.Contains("центр"))
                {

                    DestroyImmediate(button.gameObject);
                    continue;
                }
            }

            // Проверяем обработчики onClick
            if (button.onClick != null && button.onClick.GetPersistentEventCount() > 0)
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    string methodName = button.onClick.GetPersistentMethodName(i);
                    if (methodName == "CenterOnTarget")
                    {

                        DestroyImmediate(button.gameObject);
                        break;
                    }
                }
            }
        }
    }
}