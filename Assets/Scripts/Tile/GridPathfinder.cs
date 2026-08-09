using System.Collections.Generic;
using UnityEngine;

// 시설을 고정 장애물로 취급하는 8방향 타일 A* 경로 탐색기
public sealed class GridPathfinder
{
    private const int StraightCost = 10; // 상하좌우 한 칸 이동 비용
    private const int DiagonalCost = 14; // 대각선 한 칸의 근사 이동 비용

    // 상하좌우를 먼저, 대각선을 나중에 탐색하는 고정 방향 순서
    private static readonly Vector3Int[] Directions =
    {
        new(0, 1, 0),
        new(1, 0, 0),
        new(0, -1, 0),
        new(-1, 0, 0),
        new(1, 1, 0),
        new(1, -1, 0),
        new(-1, -1, 0),
        new(-1, 1, 0)
    };

    private readonly TileOccupancyManager occupancyManager;
    private readonly TileCoordinateManager coordinateManager;

    // 점유 관리자를 통해 맵 좌표와 시설 장애물 정보 참조
    public GridPathfinder(TileOccupancyManager occupancyManager)
    {
        this.occupancyManager = occupancyManager;
        coordinateManager = occupancyManager != null
            ? occupancyManager.CoordinateManager
            : null;
    }

    // A*로 시작 셀부터 목적지 셀까지의 최저 비용 경로 탐색
    public bool TryFindPath(
        Vector3Int start,
        Vector3Int goal,
        out List<Vector3Int> path)
    {
        path = new List<Vector3Int>();

        if (!CanStand(start) || !CanStand(goal))
        {
            return false;
        }

        PriorityQueue open = new();
        Dictionary<Vector3Int, int> costs = new();
        Dictionary<Vector3Int, Vector3Int> previous = new();
        HashSet<Vector3Int> closed = new();

        costs[start] = 0;
        open.Enqueue(start, GetHeuristic(start, goal));

        while (open.Count > 0)
        {
            Vector3Int current = open.Dequeue();

            if (!closed.Add(current))
            {
                continue;
            }

            if (current == goal)
            {
                BuildPath(start, goal, previous, path);
                return true;
            }

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector3Int next = current + Directions[i];

                if (!CanMove(current, next) || closed.Contains(next))
                {
                    continue;
                }

                int nextCost = costs[current] + GetStepCost(Directions[i]);

                if (costs.TryGetValue(next, out int oldCost) &&
                    nextCost >= oldCost)
                {
                    continue;
                }

                costs[next] = nextCost;
                previous[next] = current;
                open.Enqueue(next, nextCost + GetHeuristic(next, goal));
            }
        }

        return false;
    }

    // 목적지에서 역방향 Dijkstra를 한 번 실행해 모든 도달 가능 셀의 비용을 계산한다.
    public Dictionary<Vector3Int, int> BuildDistanceMap(Vector3Int destination)
    {
        Dictionary<Vector3Int, int> distances = new();

        if (!CanStand(destination))
        {
            return distances;
        }

        PriorityQueue open = new();
        distances[destination] = 0;
        open.Enqueue(destination, 0);

        while (open.Count > 0)
        {
            QueueEntry entry = open.DequeueEntry();

            if (!distances.TryGetValue(entry.Cell, out int currentCost) ||
                entry.Priority != currentCost)
            {
                continue;
            }

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector3Int next = entry.Cell + Directions[i];

                if (!CanMove(entry.Cell, next))
                {
                    continue;
                }

                int nextCost = currentCost + GetStepCost(Directions[i]);

                if (distances.TryGetValue(next, out int oldCost) &&
                    nextCost >= oldCost)
                {
                    continue;
                }

                distances[next] = nextCost;
                open.Enqueue(next, nextCost);
            }
        }

        return distances;
    }

    // 실제 타일이며 고정 장애물인 시설이 없는 셀인지 확인
    private bool CanStand(Vector3Int cell)
    {
        return coordinateManager != null &&
               coordinateManager.HasTile(cell) &&
               !occupancyManager.HasFacility(cell);
    }

    // 대상 셀과 대각선 모서리 통과 조건을 함께 검사
    private bool CanMove(Vector3Int from, Vector3Int to)
    {
        if (!CanStand(to))
        {
            return false;
        }

        int x = to.x - from.x;
        int y = to.y - from.y;

        if (x == 0 || y == 0)
        {
            return true;
        }

        // 대각선 양옆 중 하나라도 막혀 있으면 모서리를 통과하지 않는다.
        return CanStand(from + new Vector3Int(x, 0, 0)) &&
               CanStand(from + new Vector3Int(0, y, 0));
    }

    // 직선 또는 대각선 방향에 맞는 한 칸 이동 비용 반환
    private static int GetStepCost(Vector3Int direction)
    {
        return direction.x != 0 && direction.y != 0
            ? DiagonalCost
            : StraightCost;
    }

    // 8방향 이동 비용에 맞는 옥타일 거리 휴리스틱 계산
    private static int GetHeuristic(Vector3Int from, Vector3Int to)
    {
        int x = Mathf.Abs(from.x - to.x);
        int y = Mathf.Abs(from.y - to.y);
        int diagonal = Mathf.Min(x, y);
        int straight = Mathf.Max(x, y) - diagonal;
        return diagonal * DiagonalCost + straight * StraightCost;
    }

    // 목적지에서 이전 셀을 역추적한 뒤 시작점부터의 순서로 경로 구성
    private static void BuildPath(
        Vector3Int start,
        Vector3Int goal,
        Dictionary<Vector3Int, Vector3Int> previous,
        List<Vector3Int> path)
    {
        Vector3Int current = goal;
        path.Add(current);

        while (current != start)
        {
            current = previous[current];
            path.Add(current);
        }

        path.Reverse();
    }

    // 우선순위가 같을 때 삽입 순서를 보존하는 큐 항목
    private readonly struct QueueEntry
    {
        public readonly Vector3Int Cell;
        public readonly int Priority;
        public readonly int Sequence;

        public QueueEntry(Vector3Int cell, int priority, int sequence)
        {
            Cell = cell;
            Priority = priority;
            Sequence = sequence;
        }
    }

    // 최소 우선순위 항목을 먼저 꺼내는 이진 최소 힙
    private sealed class PriorityQueue
    {
        private readonly List<QueueEntry> entries = new();
        private int sequence;

        public int Count => entries.Count;

        // 새 항목을 마지막에 추가한 뒤 부모 방향으로 힙 속성 복원
        public void Enqueue(Vector3Int cell, int priority)
        {
            QueueEntry entry = new(cell, priority, sequence++);
            entries.Add(entry);
            int index = entries.Count - 1;

            while (index > 0)
            {
                int parent = (index - 1) / 2;

                if (!IsEarlier(entries[index], entries[parent]))
                {
                    break;
                }

                (entries[index], entries[parent]) =
                    (entries[parent], entries[index]);
                index = parent;
            }
        }

        public Vector3Int Dequeue()
        {
            return DequeueEntry().Cell;
        }

        // 루트 항목을 제거한 뒤 자식 방향으로 힙 속성 복원
        public QueueEntry DequeueEntry()
        {
            QueueEntry result = entries[0];
            int last = entries.Count - 1;
            entries[0] = entries[last];
            entries.RemoveAt(last);

            int index = 0;

            while (index < entries.Count)
            {
                int left = index * 2 + 1;
                int right = left + 1;

                if (left >= entries.Count)
                {
                    break;
                }

                int child = right < entries.Count &&
                            IsEarlier(entries[right], entries[left])
                    ? right
                    : left;

                if (!IsEarlier(entries[child], entries[index]))
                {
                    break;
                }

                (entries[index], entries[child]) =
                    (entries[child], entries[index]);
                index = child;
            }

            return result;
        }

        // 비용이 낮은 항목을 우선하고 동률이면 먼저 삽입된 항목을 우선
        private static bool IsEarlier(QueueEntry left, QueueEntry right)
        {
            return left.Priority < right.Priority ||
                   left.Priority == right.Priority &&
                   left.Sequence < right.Sequence;
        }
    }
}
