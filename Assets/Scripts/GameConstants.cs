using UnityEngine;

/// <summary>
/// GameConstants - централизованное хранилище всех игровых констант
///
/// CODE QUALITY: Вынесение magic numbers в именованные константы:
/// 1. Улучшает читаемость кода
/// 2. Упрощает балансировку игры
/// 3. Предотвращает дублирование значений
/// 4. Делает код самодокументируемым
///
/// ОРГАНИЗАЦИЯ: Константы сгруппированы по категориям
///
/// Вместо:
///   if (distance <= 2.0f) { ... }  // Что означает 2.0?
///
/// Используем:
///   if (distance <= GameConstants.Combat.MELEE_ATTACK_RANGE) { ... }  // Понятно!
/// </summary>
public static class GameConstants
{
    // ========================================================================
    // COMBAT - Боевые константы
    // ========================================================================
    public static class Combat
    {
        // Дистанции и урон
        public const float MELEE_ATTACK_RANGE = 2.0f;
        public const float RANGED_ATTACK_RANGE = 15.0f;
        public const float ATTACK_COOLDOWN = 1.5f;
        public const float DEFAULT_DAMAGE = 10.0f;
        public const float COMBAT_CHECK_INTERVAL = 0.5f;
        public const float POSITION_RESERVATION_RADIUS = 1.5f;

        // Сигнальные значения
        public const float INVALID_LAST_ATTACK_TIME = -999f;
        public const int INVALID_GRID_POSITION = -9999;

        // Тайминги
        public const float COMBAT_FRAME_DELAY = 0.1f;
        public const float PATH_UPDATE_THRESHOLD = 1.5f;
        public const float ATTACK_COOLDOWN_MULTIPLIER = 0.5f;
        public const float ATTACK_PAUSE_DURATION = 0.1f;

        // Анимация и поворот
        public const float ROTATION_SPEED_ATTACK = 720f;
        public const float ROTATION_ANGLE_THRESHOLD = 1f;

        // Высоты и позиции
        public const float SHOOTER_HEIGHT_OFFSET = 1f;
        public const float DAMAGE_TEXT_HEIGHT_OFFSET = 1.8f;
        public const float DAMAGE_TEXT_END_HEIGHT = 10f;

        // Поиск позиций
        public const int LINE_OF_SIGHT_SEARCH_ANGLE_STEP = 45;
        public const float PURSUIT_RANGE_MULTIPLIER = 1.5f;
    }

    // ========================================================================
    // CHARACTER - Константы персонажей
    // ========================================================================
    public static class Character
    {
        // Базовые характеристики
        public const float DEFAULT_HEALTH = 100f;
        public const float DEFAULT_MOVE_SPEED = 3f;
        public const float STOP_DISTANCE = 0.1f;
        public const float PATH_UPDATE_INTERVAL = 0.5f;
        public const float INTERACTION_RANGE = 2.0f;

        // Генерация имен
        public const int MAX_NAME_GENERATION_ATTEMPTS = 1000;
        public const int RANDOM_SEED_RANGE = 1000;

        // Смерть и падение
        public const float DEATH_ROTATION_ANGLE = -90f;

        // Визуальные константы
        public const float GIZMO_HEIGHT_OFFSET = 2.5f;
        public const float GIZMO_SPHERE_RADIUS = 0.5f;

        // Инвентарь по фракциям
        public const int PLAYER_INVENTORY_SLOTS = 20;
        public const float PLAYER_INVENTORY_MAX_WEIGHT = 100f;
        public const int ENEMY_INVENTORY_SLOTS = 10;
        public const float ENEMY_INVENTORY_MAX_WEIGHT = 50f;
        public const int NEUTRAL_INVENTORY_SLOTS = 15;
        public const float NEUTRAL_INVENTORY_MAX_WEIGHT = 75f;
    }

    // ========================================================================
    // MINING - Константы добычи ресурсов
    // ========================================================================
    public static class Mining
    {
        public const float MINING_TIME = 5.0f;
        public const float MINING_RANGE = 3.0f;
        public const int METAL_PER_MINING = 10;
        public const float MINING_CHECK_INTERVAL = 0.1f;
    }

    // ========================================================================
    // CONSTRUCTION - Константы строительства
    // ========================================================================
    public static class Construction
    {
        public const float CONSTRUCTION_TIME = 10.0f;
        public const float CONSTRUCTION_RANGE = 3.0f;
        public const int DEFAULT_METAL_COST = 50;
        public const float CONSTRUCTION_CHECK_INTERVAL = 0.1f;
    }

    // ========================================================================
    // GRID - Константы сетки
    // ========================================================================
    public static class Grid
    {
        public const int DEFAULT_GRID_WIDTH = 100;
        public const int DEFAULT_GRID_HEIGHT = 100;
        public const float CELL_SIZE = 1.0f;
        public const float CELL_HEIGHT_OFFSET = 0.1f;
    }

    // ========================================================================
    // CAMERA - Константы камеры
    // ========================================================================
    public static class Camera
    {
        public const float DEFAULT_PAN_SPEED = 20f;
        public const float DEFAULT_DRAG_SPEED = 0.6f;
        public const float DEFAULT_SMOOTH_TIME = 0.08f;
        public const float DEFAULT_ZOOM_SPEED = 10f;
        public const float MIN_ORTHO_SIZE = 6f;
        public const float MAX_ORTHO_SIZE = 30f;
        public const float EDGE_SCROLL_SPEED = 15f;
        public const float EDGE_BORDER_SIZE = 5f;
    }

    // ========================================================================
    // UI - Константы интерфейса
    // ========================================================================
    public static class UI
    {
        public const float CLICK_THRESHOLD = 5f; // pixels
        public const float HEALTHBAR_WIDTH = 50f;
        public const float HEALTHBAR_HEIGHT = 5f;
        public const float HEALTHBAR_OFFSET_Y = 2f;
        public const float DAMAGE_TEXT_DURATION = 1.0f;
        public const float DAMAGE_TEXT_RISE_SPEED = 1.0f;
    }

    // ========================================================================
    // PERFORMANCE - Константы производительности
    // ========================================================================
    public static class Performance
    {
        public const float FIND_INTERVAL = 2.0f; // Интервал между FindObjectOfType вызовами
        public const float CACHE_REFRESH_INTERVAL = 30.0f; // Интервал обновления кэша
        public const float UPDATE_INTERVAL = 0.1f; // Интервал обновления UI/логики
        public const int MAX_RAYCAST_HITS = 50; // Максимум результатов raycast
    }

    // ========================================================================
    // PHYSICS - Физические константы
    // ========================================================================
    public static class Physics
    {
        public const float RAYCAST_MAX_DISTANCE = 1000f;
        public const float OVERLAP_SPHERE_RADIUS = 5f;
        public const float GROUND_CHECK_DISTANCE = 0.5f;
    }

    // ========================================================================
    // ITEMS - Константы предметов
    // ========================================================================
    public static class Items
    {
        public const float PICKUP_RANGE = 2.0f;
        public const float ITEM_SPAWN_HEIGHT = 0.5f;
        public const int DEFAULT_STACK_SIZE = 50;
    }

    // ========================================================================
    // RESOURCES - Константы ресурсов
    // ========================================================================
    public static class Resources
    {
        public const int STARTING_METAL = 100;
        public const int MAX_METAL = 999999;
        public const int ASTEROID_MIN_METAL = 50;
        public const int ASTEROID_MAX_METAL = 200;
    }

    // ========================================================================
    // COLORS - Цветовые константы
    // ========================================================================
    public static class Colors
    {
        public static readonly Color ALLY_COLOR = Color.green;
        public static readonly Color ENEMY_COLOR = Color.red;
        public static readonly Color NEUTRAL_COLOR = Color.yellow;
        public static readonly Color HOVER_COLOR = Color.cyan;
        public static readonly Color SELECTION_COLOR = new Color(0.8f, 0.8f, 1f, 0.25f);
        public static readonly Color DAMAGE_TEXT_COLOR = Color.red;
    }

    // ========================================================================
    // LAYERS - Названия слоев
    // ========================================================================
    public static class Layers
    {
        public const string DEFAULT = "Default";
        public const string CHARACTERS = "Characters";
        public const string TERRAIN = "Terrain";
        public const string UI = "UI";
        public const string BUILDINGS = "Buildings";
    }

    // ========================================================================
    // TAGS - Названия тегов
    // ========================================================================
    public static class Tags
    {
        public const string PLAYER = "Player";
        public const string ENEMY = "Enemy";
        public const string FLOOR = "Floor";
        public const string ASTEROID = "Asteroid";
    }
}
