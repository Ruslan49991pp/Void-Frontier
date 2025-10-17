using UnityEngine;

/// <summary>
/// Определения всех игровых событий
///
/// АРХИТЕКТУРА: Централизованные события позволяют:
/// 1. Отделить отправителя от получателя
/// 2. Множественные слушатели для одного события
/// 3. Легко добавлять новые слушатели без изменения кода отправителя
/// 4. Упростить отладку (все события в одном месте)
///
/// ИСПОЛЬЗОВАНИЕ:
///   // Подписка на событие
///   EventBus.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
///
///   // Публикация события
///   EventBus.Publish(new CharacterSpawnedEvent(character));
///
///   // Отписка от события
///   EventBus.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
/// </summary>

// ============================================================================
// BASE EVENT
// ============================================================================

/// <summary>
/// Базовый класс для всех событий
/// </summary>
public abstract class GameEvent
{
    public float timestamp;

    public GameEvent()
    {
        timestamp = Time.time;
    }
}

// ============================================================================
// CHARACTER EVENTS
// ============================================================================

/// <summary>
/// Событие: Персонаж создан
/// </summary>
public class CharacterSpawnedEvent : GameEvent
{
    public Character character;

    public CharacterSpawnedEvent(Character character)
    {
        this.character = character;
    }
}

/// <summary>
/// Событие: Персонаж умер
/// </summary>
public class CharacterDiedEvent : GameEvent
{
    public Character character;
    public Character killer;

    public CharacterDiedEvent(Character character, Character killer = null)
    {
        this.character = character;
        this.killer = killer;
    }
}

/// <summary>
/// Событие: Персонаж получил урон
/// </summary>
public class CharacterDamagedEvent : GameEvent
{
    public Character character;
    public float damage;
    public Character attacker;

    public CharacterDamagedEvent(Character character, float damage, Character attacker = null)
    {
        this.character = character;
        this.damage = damage;
        this.attacker = attacker;
    }
}

// ============================================================================
// RESOURCE EVENTS
// ============================================================================

/// <summary>
/// Событие: Ресурсы изменились
/// </summary>
public class ResourcesChangedEvent : GameEvent
{
    public int metalAmount;
    public int metalChange;

    public ResourcesChangedEvent(int metalAmount, int metalChange)
    {
        this.metalAmount = metalAmount;
        this.metalChange = metalChange;
    }
}

/// <summary>
/// Событие: Предмет подобран
/// </summary>
public class ItemPickedUpEvent : GameEvent
{
    public Item item;
    public Character character;

    public ItemPickedUpEvent(Item item, Character character)
    {
        this.item = item;
        this.character = character;
    }
}

// ============================================================================
// BUILDING EVENTS
// ============================================================================

/// <summary>
/// Событие: Модуль размещен
/// </summary>
public class ModulePlacedEvent : GameEvent
{
    public GameObject module;
    public Vector2Int gridPosition;

    public ModulePlacedEvent(GameObject module, Vector2Int gridPosition)
    {
        this.module = module;
        this.gridPosition = gridPosition;
    }
}

/// <summary>
/// Событие: Модуль удален
/// </summary>
public class ModuleRemovedEvent : GameEvent
{
    public GameObject module;
    public Vector2Int gridPosition;

    public ModuleRemovedEvent(GameObject module, Vector2Int gridPosition)
    {
        this.module = module;
        this.gridPosition = gridPosition;
    }
}

/// <summary>
/// Событие: Строительство завершено
/// </summary>
public class ConstructionCompletedEvent : GameEvent
{
    public GameObject constructionSite;

    public ConstructionCompletedEvent(GameObject constructionSite)
    {
        this.constructionSite = constructionSite;
    }
}

// ============================================================================
// COMBAT EVENTS
// ============================================================================

/// <summary>
/// Событие: Бой начался
/// </summary>
public class CombatStartedEvent : GameEvent
{
    public Character attacker;
    public Character target;

    public CombatStartedEvent(Character attacker, Character target)
    {
        this.attacker = attacker;
        this.target = target;
    }
}

/// <summary>
/// Событие: Бой закончился
/// </summary>
public class CombatEndedEvent : GameEvent
{
    public Character character;

    public CombatEndedEvent(Character character)
    {
        this.character = character;
    }
}

// ============================================================================
// SELECTION EVENTS
// ============================================================================

/// <summary>
/// Событие: Выделение изменилось
/// </summary>
public class SelectionChangedEvent : GameEvent
{
    public System.Collections.Generic.List<GameObject> selectedObjects;

    public SelectionChangedEvent(System.Collections.Generic.List<GameObject> selectedObjects)
    {
        this.selectedObjects = selectedObjects;
    }
}

// ============================================================================
// MINING EVENTS
// ============================================================================

/// <summary>
/// Событие: Добыча началась
/// </summary>
public class MiningStartedEvent : GameEvent
{
    public Character miner;
    public GameObject asteroid;

    public MiningStartedEvent(Character miner, GameObject asteroid)
    {
        this.miner = miner;
        this.asteroid = asteroid;
    }
}

/// <summary>
/// Событие: Добыча завершена
/// </summary>
public class MiningCompletedEvent : GameEvent
{
    public Character miner;
    public GameObject asteroid;
    public int metalGained;

    public MiningCompletedEvent(Character miner, GameObject asteroid, int metalGained)
    {
        this.miner = miner;
        this.asteroid = asteroid;
        this.metalGained = metalGained;
    }
}

// ============================================================================
// GAME STATE EVENTS
// ============================================================================

/// <summary>
/// Событие: Игра поставлена на паузу
/// </summary>
public class GamePausedEvent : GameEvent
{
    public bool isPaused;
    public bool isBuildModePause;

    public GamePausedEvent(bool isPaused, bool isBuildModePause = false)
    {
        this.isPaused = isPaused;
        this.isBuildModePause = isBuildModePause;
    }
}
