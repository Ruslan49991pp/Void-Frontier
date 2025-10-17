# VoidFrontier - Architecture Improvements

## Обзор изменений

Проект VoidFrontier был значительно улучшен для повышения производительности, расширяемости и поддерживаемости кода. Все изменения организованы в три фазы:

- **Фаза 1**: Оптимизация производительности
- **Фаза 2**: Архитектурный рефакторинг
- **Фаза 3**: Улучшение качества кода

---

## Фаза 1: Оптимизация производительности

### 1.1 Удаление FindObjectOfType из Update циклов

**Проблема**: `FindObjectOfType` - очень медленная операция (O(n) сканирование всей сцены)

**Решение**:
- `CameraController.cs`: Интервальный поиск с 2-секундным интервалом вместо поиска каждый кадр
- Ожидаемый прирост: +10-15 FPS

**Файлы изменены**:
- `CameraController.cs:44-80`

### 1.2 Централизация Raycast системы

**Проблема**: Каждый персонаж выполнял RaycastAll в Update() - 10 персонажей = 600 raycast/сек

**Решение**:
- Удален `CheckMouseHover()` из `Character.cs`
- Hover теперь обрабатывается централизованно в `SelectionManager`
- Ожидаемый прирост: +15-20 FPS

**Файлы изменены**:
- `Character.cs:Update()` - закомментирован CheckMouseHover()

### 1.3 Event-driven ResourcePanelUI

**Проблема**: `FindObjectsOfType` каждые 5 секунд для поиска персонажей

**Решение**:
- Увеличен интервал с 5 до 30 секунд
- Добавлена подписка на событие `Character.OnPlayerCharacterSpawned`
- Немедленное обновление при появлении нового персонажа

**Файлы изменены**:
- `ResourcePanelUI.cs:33-75, 94-105`

### 1.4 Оптимизация GridManager

**Проблема**: Dictionary lookup медленнее прямого доступа к массиву для 10,000 ячеек

**Решение**:
- Заменен `Dictionary.TryGetValue` на прямой доступ к массиву
- Преобразование координат: `arrayX = gridPosition.x + halfWidth`
- Ожидаемый прирост: +5-8 FPS

**Файлы изменены**:
- `GridManager.cs:GetCell(), IsValidGridPosition()`

### 1.5 Оптимизация материалов в SelectionManager

**Проблема**: Создание/уничтожение материалов вызывает GC паузы (~50ms)

**Решение**:
- Hover материалы кэшируются и переиспользуются
- Материалы создаются один раз и уничтожаются только в OnDestroy
- Устранены GC паузы от аллокации материалов

**Файлы изменены**:
- `SelectionManager.cs:991-1046`

**Итого Фаза 1**: Ожидаемый прирост производительности **+35-50 FPS**

---

## Фаза 2: Архитектурный рефакторинг

### 2.1 ServiceLocator Pattern

**Цель**: Централизованное управление всеми системами игры

**Новые файлы**:
- `ServiceLocator.cs` - Статический класс для регистрации/получения сервисов
- `GameBootstrap.cs` - Инициализация всех систем при старте игры

**Использование**:
```csharp
// Регистрация (в GameBootstrap)
ServiceLocator.Register<GridManager>(gridManager);

// Получение
GridManager grid = ServiceLocator.Get<GridManager>();

// Безопасное получение
if (ServiceLocator.TryGet<MiningManager>(out var mining)) {
    // Используем mining
}
```

**Преимущества**:
- O(1) доступ вместо O(n) сканирования сцены
- Единая точка управления зависимостями
- Легко тестировать (можно подменять реализации)

### 2.2 Interface-Based Design

**Цель**: Отделение интерфейса от реализации (SOLID принципы)

**Новый файл**: `IGameSystems.cs`

**Созданные интерфейсы**:
- `IGridManager` - Управление сеткой
- `ISelectionManager` - Система выделения
- `IBuildingSystem` - Строительство
- `ICombatManager` - Боевая система
- `IMiningManager` - Добыча ресурсов
- `IConstructionSystem` - Строительство объектов
- `ICameraController` - Управление камерой
- `IResourcePanel` - UI ресурсов

**Использование**:
```csharp
// Вместо конкретного класса
IGridManager grid = ServiceLocator.Get<IGridManager>();
GridCell cell = grid.GetCell(position);
```

**Преимущества**:
- Легко создавать моки для тестирования
- Четкое определение API системы
- Возможность менять реализацию без изменения использующего кода

### 2.3 Centralized EventBus

**Цель**: Decoupled коммуникация между системами

**Новые файлы**:
- `EventBus.cs` - Система подписки/публикации событий
- `GameEvents.cs` - Определения всех игровых событий

**Типы событий**:
- Character Events (Spawned, Died, Damaged)
- Resource Events (Changed, ItemPickedUp)
- Building Events (ModulePlaced, ModuleRemoved, ConstructionCompleted)
- Combat Events (Started, Ended)
- Selection Events (Changed)
- Mining Events (Started, Completed)
- Game State Events (Paused)

**Использование**:
```csharp
// Подписка
EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);

void OnCharacterSpawned(CharacterSpawnedEvent evt) {
    Debug.Log($"Character spawned: {evt.character.GetFullName()}");
}

// Публикация
EventBus.Publish(new CharacterSpawnedEvent(character));

// ВАЖНО: Отписка в OnDestroy!
void OnDestroy() {
    EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
}
```

**Преимущества**:
- Системы не знают друг о друге напрямую
- Легко добавлять новых слушателей
- Централизованная точка отладки коммуникации

### 2.4 BaseManager Pattern

**Цель**: Единый базовый класс для всех менеджеров

**Новый файл**: `BaseManager.cs`

**Возможности**:
- Стандартизированный lifecycle (Initialize, Shutdown)
- Интеграция с ServiceLocator
- Встроенное логирование
- Helper методы для получения сервисов

**Использование**:
```csharp
public class MyManager : BaseManager
{
    protected override void OnManagerInitialized()
    {
        // Ваша инициализация
        // ServiceLocator уже доступен

        GridManager grid = GetService<GridManager>();
    }

    protected override void OnManagerShutdown()
    {
        // Очистка ресурсов
        // Отписка от событий
    }
}
```

**Преимущества**:
- Единообразный код инициализации
- Меньше boilerplate кода
- Встроенная интеграция с ServiceLocator

### 2.5 ServiceLocator Integration

**Изменено**: `SelectionManager.cs`

**Что сделано**:
- Добавлены кэшированные ссылки на GridManager и MiningManager
- Инициализация через ServiceLocator вместо FindObjectOfType
- Автоматическая регистрация динамически созданных менеджеров

**До**:
```csharp
GridManager gridManager = FindObjectOfType<GridManager>(); // Медленно!
```

**После**:
```csharp
gridManager = ServiceLocator.Get<GridManager>(); // Быстро!
```

---

## Фаза 3: Улучшение качества кода

### 3.1 Game Constants

**Цель**: Централизация всех magic numbers

**Новый файл**: `GameConstants.cs`

**Категории констант**:
- Combat (дистанции атак, кулдауны, урон)
- Character (здоровье, скорость, дистанции)
- Mining (время добычи, дистанции, количество ресурсов)
- Construction (время строительства, стоимость)
- Grid (размеры сетки, размер ячеек)
- Camera (скорости, зум)
- UI (пороги, размеры, длительности)
- Performance (интервалы оптимизации)
- Colors (стандартизированные цвета)
- Layers/Tags (строковые константы)

**Использование**:
```csharp
// Вместо
if (distance <= 2.0f) { ... } // Что означает 2.0?

// Используем
if (distance <= GameConstants.Combat.MELEE_ATTACK_RANGE) { ... } // Понятно!
```

**Преимущества**:
- Код самодокументируется
- Легко балансировать игру (все в одном месте)
- Предотвращает дублирование значений

### 3.2 Game Utilities

**Цель**: Централизация общих операций

**Новый файл**: `GameUtilities.cs`

**Категории утилит**:
- Component Utilities (безопасное получение компонентов)
- Null Check Utilities (Unity-specific null checks)
- Position & Distance (2D/3D конвертация, плоские дистанции)
- Color Utilities (альфа, lerp)
- GameObject Utilities (безопасное уничтожение, активация)
- Math Utilities (clamp, normalize, approximately)
- Layer & Tag Utilities (проверки слоев)
- Time Utilities (elapsed time, progress)

**Использование**:
```csharp
// Безопасное получение компонента
Character character = GameUtils.GetComponentSafe<Character>(gameObject);

// Проверка дистанции
if (GameUtils.IsInRange(player, enemy, attackRange)) { ... }

// 2D/3D конвертация
Vector2 flatPos = GameUtils.To2D(worldPosition);

// Проверка что объект не уничтожен
if (GameUtils.IsDestroyed(obj)) { ... }
```

**Преимущества**:
- Уменьшение дублирования кода (DRY principle)
- Единообразие операций
- Легче тестировать

---

## Инструкция по интеграции

### Шаг 1: Добавить GameBootstrap в сцену

1. Создайте пустой GameObject в сцене
2. Назовите его "GameBootstrap"
3. Добавьте компонент `GameBootstrap`
4. **ВАЖНО**: Настройте Script Execution Order:
   - Edit → Project Settings → Script Execution Order
   - Добавьте `GameBootstrap` со значением **-1000**
   - Это гарантирует что он выполнится раньше всех остальных скриптов

### Шаг 2: Проверить консоль при старте

При запуске игры в консоли должны появиться сообщения:

```
[GameBootstrap] Starting game initialization...
[ServiceLocator] Registered service: GridManager
[ServiceLocator] Registered service: SelectionManager
...
[GameBootstrap] Registered X services
[GameBootstrap] Game initialization complete!
```

### Шаг 3: Постепенная миграция систем

Для каждого менеджера который нужно обновить:

1. **Наследование от BaseManager** (опционально):
```csharp
public class MyManager : BaseManager
{
    protected override void OnManagerInitialized() {
        // Инициализация
    }
}
```

2. **Замена FindObjectOfType**:
```csharp
// Было
GridManager grid = FindObjectOfType<GridManager>();

// Стало
GridManager grid = ServiceLocator.Get<GridManager>();
```

3. **Использование EventBus**:
```csharp
// В Start/Awake
EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);

// В OnDestroy
EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
```

4. **Замена magic numbers**:
```csharp
// Было
if (distance <= 2.0f) { ... }

// Стало
if (distance <= GameConstants.Combat.MELEE_ATTACK_RANGE) { ... }
```

---

## Ожидаемые результаты

### Производительность
- **+35-50 FPS** от оптимизаций Phase 1
- Устранение GC пауз (~50ms)
- Уменьшение raycast с 600+/сек до ~60/сек

### Качество кода
- **Меньше дублирования** благодаря GameUtilities
- **Самодокументируемый код** благодаря GameConstants
- **Четкая архитектура** благодаря ServiceLocator + EventBus

### Поддерживаемость
- **Легче добавлять новые системы** (шаблон BaseManager)
- **Легче тестировать** (интерфейсы + ServiceLocator)
- **Легче находить ошибки** (централизованное логирование)

---

## Следующие шаги

### Рекомендуется:

1. **Миграция остальных менеджеров** на ServiceLocator:
   - MiningManager
   - CombatManager
   - ConstructionSystem

2. **Реализация интерфейсов** для существующих систем:
   - Добавить `: IGridManager` к GridManager
   - Обновить регистрацию в GameBootstrap

3. **Замена magic numbers** в остальном коде:
   - Используйте поиск по числам (2.0f, 5.0f, etc)
   - Заменяйте на константы из GameConstants

4. **Добавление новых событий** по мере необходимости:
   - Определить в GameEvents.cs
   - Публиковать в нужных местах
   - Подписываться в заинтересованных системах

---

## Troubleshooting

### Проблема: ServiceLocator not initialized

**Решение**: Убедитесь что GameBootstrap выполняется первым (Script Execution Order = -1000)

### Проблема: Service not found in ServiceLocator

**Решение**: Проверьте что менеджер зарегистрирован в `GameBootstrap.RegisterServices()`

### Проблема: Memory leaks от EventBus

**Решение**: ВСЕГДА вызывайте `EventBus.Unsubscribe()` в OnDestroy()

### Проблема: Null reference exceptions

**Решение**: Используйте `GameUtils.IsDestroyed()` для Unity-specific null checks

---

## Контакты и поддержка

Для вопросов по архитектуре обратитесь к:
- ServiceLocator.cs - управление сервисами
- EventBus.cs - система событий
- BaseManager.cs - базовый класс менеджеров
- GameConstants.cs - константы
- GameUtilities.cs - утилиты

Все файлы содержат подробные комментарии и примеры использования.
