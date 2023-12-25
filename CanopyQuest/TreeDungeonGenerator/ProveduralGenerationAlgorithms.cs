using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProveduralGenerationAlgorithms
{
    public static HashSet<Vector2Int> SimpleRandomWalk(Vector2Int startPosition, int walkLength)
    {
        HashSet<Vector2Int> path = new HashSet<Vector2Int>();
        path.Add(startPosition);
        var previousPosition = startPosition;
        for(int i = 0; i < walkLength; i++)
        {
            var newPosition = previousPosition + Direction2D.GetRandomDirection();
            path.Add(newPosition);
            previousPosition = newPosition;
        }
        return path;

    }

    public static List<Vector2Int> RandomWalkCorridor(Vector2Int startPosition, int corridorLength)
    {
        List<Vector2Int> corridor = new List<Vector2Int>();
        var direction = Direction2D.GetRandomDirection();
        var currentPosition = startPosition;
        corridor.Add(currentPosition);

        for(int i = 0; i < corridorLength; i++)
        {
            currentPosition += direction;
            corridor.Add(currentPosition);
        }

        return corridor;
    }

    public static List<BoundsInt> BinarySpacePartitioning(BoundsInt spaceToSplit,int minWidth,int minHeight)
    {
        Queue<BoundsInt> roomsQueue = new Queue<BoundsInt>();
        List<BoundsInt> roomList = new List<BoundsInt>();
        roomsQueue.Enqueue(spaceToSplit);
        while (roomsQueue.Count > 0)
        {
            var room = roomsQueue.Dequeue();
            if (room.size.y >= minHeight && room.size.x >= minWidth)
            {
                if (RandomManager.Instance.GetRandomFloat(0.0f,1.0f) < 0.5f)
                {
                    if (room.size.y >= minHeight * 2)
                    {
                        SplitVerticaly(minHeight, roomsQueue, room);
                        
                    }else if (room.size.x >= 2 * minWidth)
                    {
                        SplitHorizontaly(minHeight, roomsQueue, room);
                    }
                    else
                    {
                        roomList.Add(room);
                    }
                }
                else
                {
                    if (room.size.x >= 2 * minWidth)
                    {
                        SplitHorizontaly(minHeight, roomsQueue, room);
                    }
                    else if (room.size.y >= minHeight * 2)
                    {
                        SplitVerticaly(minHeight, roomsQueue, room);
                    }
                    else
                    {
                        roomList.Add(room);
                    }
                }
            }

        }
        return roomList;
    }

    private static void SplitHorizontaly(int minWidth, Queue<BoundsInt> roomsQueue, BoundsInt room)
    {
        var xSplit = RandomManager.Instance.GetRandomInt(1, room.size.x);
        BoundsInt room1 = new BoundsInt(room.min, new Vector3Int(xSplit, room.size.y, room.size.z));
        BoundsInt room2 = new BoundsInt(new Vector3Int(room.min.x+xSplit,room.min.y,room.min.z), new Vector3Int(room.size.x-xSplit, room.size.y, room.size.z));
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);

    }

    private static void SplitVerticaly(int minHeight, Queue<BoundsInt> roomsQueue, BoundsInt room)
    {
        var ySplit = RandomManager.Instance.GetRandomInt(1, room.size.y);
        BoundsInt room1 = new BoundsInt(room.min, new Vector3Int(room.size.x, ySplit, room.size.z));
        BoundsInt room2 = new BoundsInt(new Vector3Int(room.min.x, room.min.y+ySplit, room.min.z), new Vector3Int(room.size.x, room.size.y-ySplit, room.size.z));
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }
}

public static class Direction2D
{
    public static List<Vector2Int> cardinalDirectionsList = new List<Vector2Int>
    {
        new Vector2Int(0,1),//up
        new Vector2Int(1,0),//right
        new Vector2Int(0,-1),//down
        new Vector2Int(-1,0)//left

    };

    public static List<Vector2Int>diagonalDirectionsList = new List<Vector2Int>
    {
        new Vector2Int(1,1),//up-right
        new Vector2Int(1,-1),//right-down
        new Vector2Int(-1,-1),//down-left
        new Vector2Int(-1,1)//left-up

    };

    public static List<Vector2Int> eightDirectionList = new List<Vector2Int>
    {
        new Vector2Int(0,1),//up
        new Vector2Int(1,1),//up-right
        new Vector2Int(1,0),//right
        new Vector2Int(1,-1),//right-down
        new Vector2Int(0,-1),//down
        new Vector2Int(-1,-1),//down-left
        new Vector2Int(-1,0),//left
        new Vector2Int(-1,1)//left-up
    };

    public static Vector2Int GetRandomDirection()
    {
        return cardinalDirectionsList[RandomManager.Instance.GetRandomInt(0, cardinalDirectionsList.Count)];
    }
}