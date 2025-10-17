using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Простая система поиска пути с обходом препятствий
/// </summary>
public class SimplePathfinder : MonoBehaviour
{
    private GridManager gridManager;

    private GridManager GetGridManager()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
        }
        return gridManager;
    }
    
    /// <summary>
    /// Найти путь от начальной до конечной позиции
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        var gridManager = GetGridManager();
        if (gridManager == null)
        {
            return new List<Vector2Int> { end };
        }

        
        
        // Если начальная и конечная точки одинаковы
        if (start == end)
        {
            return new List<Vector2Int>();
        }
        
        // Вычисляем реальное расстояние
        int distance = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);
        
        // Если путь прямой и свободен, строим прямую линию
        if (IsDirectPathClear(start, end))
        {
            var directPath = GetLinePoints(start, end);
            directPath.RemoveAt(0); // Убираем стартовую точку
            return directPath;
        }
        
        // Используем простой A* алгоритм
        var path = FindPathAStar(start, end);
        return path;
    }
    
    /// <summary>
    /// Проверить, свободен ли прямой путь
    /// </summary>
    bool IsDirectPathClear(Vector2Int start, Vector2Int end)
    {
        // Простая проверка по линии Брезенхема
        List<Vector2Int> linePoints = GetLinePoints(start, end);

        foreach (var point in linePoints)
        {
            bool passable = IsCellPassable(point);

            if (!passable)
            {
                return false;
            }
        }

        return true;
    }
    
    /// <summary>
    /// Получить точки линии между двумя позициями (улучшенный алгоритм с поддержкой диагоналей)
    /// </summary>
    List<Vector2Int> GetLinePoints(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> points = new List<Vector2Int>();
        
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        
        int x = start.x;
        int y = start.y;
        
        int xInc = (end.x > start.x) ? 1 : -1;
        int yInc = (end.y > start.y) ? 1 : -1;
        
        points.Add(new Vector2Int(x, y));
        
        if (dx == 0) // Вертикальная линия
        {
            while (y != end.y)
            {
                y += yInc;
                points.Add(new Vector2Int(x, y));
            }
        }
        else if (dy == 0) // Горизонтальная линия
        {
            while (x != end.x)
            {
                x += xInc;
                points.Add(new Vector2Int(x, y));
            }
        }
        else if (dx == dy) // Идеальная диагональ
        {
            while (x != end.x && y != end.y)
            {
                x += xInc;
                y += yInc;
                points.Add(new Vector2Int(x, y));
            }
        }
        else // Обычная линия Брезенхема
        {
            int error = dx - dy;
            
            while (x != end.x || y != end.y)
            {
                int error2 = error * 2;
                
                if (error2 > -dy && x != end.x)
                {
                    error -= dy;
                    x += xInc;
                }
                
                if (error2 < dx && y != end.y)
                {
                    error += dx;
                    y += yInc;
                }
                
                points.Add(new Vector2Int(x, y));
            }
        }
        
        return points;
    }
    
    /// <summary>
    /// Поиск пути с использованием упрощенного A*
    /// </summary>
    List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int end)
    {
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<Vector2Int>();
        
        var startNode = new PathNode(start, 0, GetHeuristic(start, end), null);
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {
            // Найти узел с наименьшей стоимостью F
            var currentNode = openSet.OrderBy(n => n.F).First();
            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);
            
            // Если достигли цели
            if (currentNode.Position == end)
            {
                return ReconstructPath(currentNode);
            }
            
            // Проверяем соседей
            var neighbors = GetNeighbors(currentNode.Position);
            
            foreach (var neighborPos in neighbors)
            {
                if (closedSet.Contains(neighborPos))
                    continue;
                
                // Вычисляем стоимость движения (диагональ стоит √2 ≈ 1.414)
                bool isDiagonal = (neighborPos.x != currentNode.Position.x) && (neighborPos.y != currentNode.Position.y);
                float moveCost = isDiagonal ? 1.414f : 1.0f;
                float gCost = currentNode.G + moveCost;
                
                var existingNode = openSet.FirstOrDefault(n => n.Position == neighborPos);
                
                if (existingNode == null)
                {
                    var neighborNode = new PathNode(neighborPos, gCost, GetHeuristic(neighborPos, end), currentNode);
                    openSet.Add(neighborNode);
                }
                else if (gCost < existingNode.G)
                {
                    existingNode.G = gCost;
                    existingNode.Parent = currentNode;
                }
            }
        }

        // Путь не найден - возвращаем пустой список (НЕ прямой путь, т.к. путь заблокирован препятствиями)
        // FileLogger.LogWarning($"[PATHFINDER] ✗ No path found from {start} to {end} - target is unreachable (blocked by walls/obstacles)");
        return new List<Vector2Int>(); // Пустой список означает что путь невозможен
    }
    
    /// <summary>
    /// Получить соседние клетки (8 направлений: 4 основных + 4 диагональных)
    /// </summary>
    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        var neighbors = new List<Vector2Int>
        {
            // Основные направления
            pos + Vector2Int.up,           // север
            pos + Vector2Int.down,         // юг
            pos + Vector2Int.left,         // запад
            pos + Vector2Int.right,        // восток
            
            // Диагональные направления
            pos + new Vector2Int(-1, 1),   // северо-запад
            pos + new Vector2Int(1, 1),    // северо-восток
            pos + new Vector2Int(-1, -1),  // юго-запад
            pos + new Vector2Int(1, -1)    // юго-восток
        };
        
        var gridManager = GetGridManager();
        return neighbors.Where(p => gridManager.IsValidGridPosition(p) && IsCellPassable(p)).ToList();
    }
    
    /// <summary>
    /// Вычислить эвристическую функцию (диагональное расстояние)
    /// </summary>
    float GetHeuristic(Vector2Int from, Vector2Int to)
    {
        // Диагональная эвристика: более точная для движения по 8 направлениям
        int dx = Mathf.Abs(from.x - to.x);
        int dy = Mathf.Abs(from.y - to.y);
        
        // Количество диагональных шагов (минимум из dx и dy)
        int diagonalSteps = Mathf.Min(dx, dy);
        // Количество прямых шагов (оставшиеся)
        int straightSteps = Mathf.Max(dx, dy) - diagonalSteps;
        
        // Диагональные шаги стоят √2, прямые шаги стоят 1
        return diagonalSteps * 1.414f + straightSteps * 1.0f;
    }
    
    /// <summary>
    /// Восстановить путь из узлов
    /// </summary>
    List<Vector2Int> ReconstructPath(PathNode endNode)
    {
        var path = new List<Vector2Int>();
        var current = endNode;
        
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }
        
        path.Reverse();
        path.RemoveAt(0); // Убираем стартовую позицию
        
        return path;
    }
    
    /// <summary>
    /// Проверить, можно ли пройти через клетку (свободна или содержит персонажа/ресурс)
    /// </summary>
    bool IsCellPassable(Vector2Int pos)
    {
        var gridManager = GetGridManager();
        if (gridManager == null || !gridManager.IsValidGridPosition(pos))
            return false;

        var cell = gridManager.GetCell(pos);
        if (cell == null || !cell.isOccupied)
            return true;

        // Персонажи и ресурсы не блокируют путь (можно пройти через них)
        // Ресурсы можно подбирать, персонажи могут проходить друг через друга
        bool passable = cell.objectType == "Character" || cell.objectType == "Resource";

        return passable;
    }
    
    /// <summary>
    /// Узел для алгоритма A*
    /// </summary>
    private class PathNode
    {
        public Vector2Int Position;
        public float G; // Стоимость от старта
        public float H; // Эвристическая стоимость до цели
        public float F => G + H; // Общая стоимость
        public PathNode Parent;
        
        public PathNode(Vector2Int position, float g, float h, PathNode parent)
        {
            Position = position;
            G = g;
            H = h;
            Parent = parent;
        }
    }
}