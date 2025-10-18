# Руководство по миграции на автоматическое развертывание систем

## Обзор изменений

GameBootstrap теперь **автоматически создает ВСЕ необходимые системы**. Это критически важно для процедурной генерации локаций.

## Зачем это нужно?

### До изменений:
- Нужно вручную добавлять менеджеры в каждую сцену
- Процедурные локации требуют сложной настройки
- Высокий риск забыть добавить систему
- Сложно синхронизировать изменения между сценами

### После изменений:
✅ Процедурные локации работают автоматически
✅ Единая точка конфигурации
✅ Гарантированная консистентность
✅ Минимальная настройка новых сцен

---

## Шаг 1: Удалите лишние менеджеры из сцены (опционально)

GameBootstrap теперь создает системы автоматически, поэтому вы можете удалить их из Hierarchy:

### Системы, которые можно удалить:
- ✓ GridManager
- ✓ SelectionManager
- ✓ CombatSystem
- ✓ ConstructionManager
- ✓ MiningManager
- ✓ EnemyTargetingSystem
- ✓ ShipBuildingSystem
- ✓ ResourcePanelUI
- ✓ InventoryUI
- ✓ EventSystem
- ✓ CameraController
- ✓ GamePauseManager

### Что НУЖНО оставить:
- ✓ GameBootstrap (обязательно!)
- ✓ BuildMenuManager (требует Inspector ссылок на UI кнопки и панели)
- ✓ ItemIconManager (требует Inspector ссылку на ItemDatabase)
- ✓ Canvas и другие UI элементы (Canvas_Main, Canvas_Popup и т.д.)
- ✓ Main Camera
- ✓ Directional Light
- ✓ Любые специфичные для сцены объекты

### Как удалить безопасно:

1. **Откройте вашу главную сцену**
2. **Проверьте наличие GameBootstrap:**
   - В Hierarchy должен быть GameObject с компонентом GameBootstrap
   - Если его нет - создайте: `GameObject → Create Empty`, назовите "GameBootstrap"
   - Добавьте компонент GameBootstrap

3. **Удалите дублирующиеся менеджеры:**
   - Найдите в Hierarchy объекты из списка выше
   - Удалите их (они создадутся автоматически)

4. **Проверьте Script Execution Order:**
   - `Edit → Project Settings → Script Execution Order`
   - Убедитесь что GameBootstrap установлен на **-1000**

---

## Шаг 2: Тестирование

1. **Запустите игру в Play Mode**
2. **Проверьте Console:**
   ```
   [GameBootstrap] Starting game initialization...
   [GameBootstrap] ✓ Created GridManager
   [GameBootstrap] ✓ Created SelectionManager
   [GameBootstrap] ✓ Created CombatSystem
   ...
   [ServiceLocator] Registered 10 services
   [GameBootstrap] Game initialization complete!
   ```
3. **Проверьте Hierarchy во время игры:**
   - Должны появиться все созданные системы
4. **Проверьте игровой лог:**
   ```
   C:\Users\<User>\AppData\LocalLow\DefaultCompany\VoidFrontier\game_log.txt
   ```
   - **Не должно быть ошибок** типа "ServiceLocator not initialized"

---

## Шаг 3: Создание новых локаций

### Для ручных сцен:
```csharp
// Создайте новую сцену
// File → New Scene → Basic (Built-in)
// Добавьте только:
// 1. GameObject с компонентом GameBootstrap
// 2. Main Camera
// 3. Directional Light
// ВСЁ! Все системы создадутся автоматически
```

### Для процедурных локаций:
```csharp
public class LocationGenerator : MonoBehaviour
{
    void GenerateNewLocation(int seed)
    {
        // Создаем новую сцену процедурно
        Scene newScene = SceneManager.CreateScene("GeneratedLocation_" + seed);
        SceneManager.SetActiveScene(newScene);

        // Создаем GameBootstrap
        GameObject bootstrap = new GameObject("GameBootstrap");
        bootstrap.AddComponent<GameBootstrap>();

        // Все системы создадутся автоматически!
        // Добавляйте свой контент:
        // - Процедурный рельеф
        // - Врагов
        // - Ресурсы
        // - и т.д.
    }
}
```

---

## Устранение проблем

### Ошибка: "ServiceLocator is not initialized yet!"
**Решение:** Проверьте Script Execution Order, GameBootstrap должен быть на -1000

### Ошибка: "Service X not found!"
**Решение:** Убедитесь что GameBootstrap создает эту систему в EnsureSystemsExist()

### Дублирование систем
**Решение:** Удалите из сцены те системы, которые GameBootstrap создает автоматически

### UI не появляется
**Решение:**
- Canvas объекты должны оставаться в сцене
- UI менеджеры (ResourcePanelUI, InventoryUI) создаются автоматически
- Проверьте что UI компоненты правильно связаны с Canvas

---

## Преимущества новой архитектуры

### 1. Процедурная генерация
```csharp
// Раньше:
Scene scene = CreateScene();
AddGridManager(scene);
AddSelectionManager(scene);
AddCombatSystem(scene);
// ... 10+ строк кода

// Теперь:
Scene scene = CreateScene();
AddGameBootstrap(scene);
// ВСЁ!
```

### 2. Единая конфигурация
Все системы настраиваются в одном месте: **GameBootstrap.cs**

### 3. Гибкость
```csharp
// Можно переопределить систему в конкретной сцене:
// 1. Вручную добавьте GridManager в Hierarchy
// 2. Настройте его параметры
// 3. GameBootstrap найдет его и НЕ создаст дубликат
```

### 4. Консистентность
Все сцены гарантированно получают одинаковый набор систем.

---

## Дополнительная информация

### Какие системы создаются автоматически?

См. `GameBootstrap.cs → EnsureSystemsExist()`

**CORE SYSTEMS:**
- GridManager
- SelectionManager
- CombatSystem (BaseManager)
- ConstructionManager (BaseManager)
- MiningManager (BaseManager)
- EnemyTargetingSystem

**BUILDING SYSTEMS:**
- ShipBuildingSystem

**UI SYSTEMS:**
- ResourcePanelUI
- InventoryUI
- EventSystem (обработчик UI событий)

**ТРЕБУЮТ РУЧНОЙ НАСТРОЙКИ (НЕ создаются автоматически):**
- BuildMenuManager - требует Inspector ссылок на UI кнопки и панели
- ItemIconManager - требует Inspector ссылку на ItemDatabase (singleton)

**CAMERA & INPUT:**
- CameraController

**GAME MANAGEMENT:**
- GamePauseManager

### Как добавить новую систему?

1. Откройте `GameBootstrap.cs`
2. Добавьте код в `EnsureSystemsExist()`:
```csharp
// MyNewSystem - описание системы
if (FindObjectOfType<MyNewSystem>() == null)
{
    GameObject obj = new GameObject("MyNewSystem");
    obj.AddComponent<MyNewSystem>();
    Debug.Log("[GameBootstrap] ✓ Created MyNewSystem");
}
```
3. Добавьте регистрацию в `RegisterServices()`:
```csharp
MyNewSystem system = FindObjectOfType<MyNewSystem>();
if (system != null)
{
    ServiceLocator.Register<MyNewSystem>(system);
}
```

---

## Вопросы?

Если возникли проблемы:
1. Проверьте Console на ошибки
2. Проверьте game_log.txt
3. Убедитесь что GameBootstrap в Script Execution Order
4. Проверьте что в сцене есть GameObject с GameBootstrap

**Успешной миграции!** 🚀
