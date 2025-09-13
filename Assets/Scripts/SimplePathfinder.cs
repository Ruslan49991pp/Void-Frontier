using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Простая система поиска пути с обходом препятствий
/// </summary>
public class SimplePathfinder : MonoBehaviour
{
    private GridManager gridManager;
    
    void Awake()
    {
        gridManager = FindObjectOfType<GridManager>();
    }
    
    /// <summary>
    /// Найти путь от начальной до конечной позиции
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager не найден!");
            return new List<Vector2Int> { end };
        }
        
        Debug.Log($"SimplePathfinder: поиск пути от {start} к {end}");
        
        // Если начальная и конечная точки одинаковы
        if (start == end)
        {
            Debug.Log($"SimplePathfinder: старт и финиш одинаковы, путь не нужен");
            return new List<Vector2Int>();
        }
        
        // Вычисляем реальное расстояние
        int distance = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);
        Debug.Log($"SimplePathfinder: манхэттенское расстояние = {distance}");
        
        // Если путь прямой и свободен, строим прямую линию
        if (IsDirectPathClear(start, end))
        {
            var directPath = GetLinePoints(start, end);
            directPath.RemoveAt(0); // Убираем стартовую точку
            Debug.Log($"SimplePathfinder: прямой путь свободен, построена линия из {directPath.Count} точек");
            return directPath;
        }
        
        // Используем простой A* алгоритм
        var path = FindPathAStar(start, end);
        Debug.Log($"SimplePathfinder: A* нашел путь длиной {path?.Count ?? 0}");
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
            if (!IsCellPassable(point))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Получить точки линии между двумя позициями (алгоритм Брезенхема)
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
        
        int error = dx - dy;
        
        for (int i = 0; i <= dx + dy; i++)
        {
            points.Add(new Vector2Int(x, y));
            
            if (x == end.x && y == end.y) break;
            
            int error2 = error * 2;
            
            if (error2 > -dy)
            {
                error -= dy;
                x += xInc;
            }
            
            if (error2 < dx)
            {
                error += dx;
                y += yInc;
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
                if (closedSet.Contains(neighborPos) || !IsCellPassable(neighborPos))
                    continue;
                
                float gCost = currentNode.G + 1;
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
        
        // Путь не найден, возвращаем прямой путь к цели
        Debug.LogWarning($"Путь от {start} к {end} не найден, используем прямой путь");
        return new List<Vector2Int> { end };
    }
    
    /// <summary>
    /// Получить соседние клетки (4 направления)
    /// </summary>
    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        }.Where(p => gridManager.IsValidGridPosition(p)).ToList();
    }
    
    /// <summary>
    /// Вычислить эвристическую функцию (манхэттенское расстояние)
    /// </summary>
    float GetHeuristic(Vector2Int from, Vector2Int to)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
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
    /// Проверить, можно ли пройти через клетку (свободна или содержит персонажа)
    /// </summary>
    bool IsCellPassable(Vector2Int pos)
    {
        if (!gridManager.IsValidGridPosition(pos))
            return false;
        
        var cell = gridManager.GetCell(pos);
        if (cell == null || !cell.isOccupied)
            return true;
        
        // Персонажи не блокируют путь (другие персонажи могут проходить через них)
        return cell.objectType == "Character";
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