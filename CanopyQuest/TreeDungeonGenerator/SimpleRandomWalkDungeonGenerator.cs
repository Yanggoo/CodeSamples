using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class SimpleRandomWalkDungeonGenerator : AbstractDungeonGenerator
{
    [SerializeField]
    protected SimpleRandomWalkSO randomWalkParameters;

    protected override void RunProceduralGeneration()
    {
        tileMapVisualizer.Clear();
        HashSet<Vector2Int> floorPosition = RunRandomWalk(randomWalkParameters,startPosition);
        tileMapVisualizer.PaintFloorTile(floorPosition);
        WallGenerator.CreateWalls(floorPosition, tileMapVisualizer);
    }

    protected HashSet<Vector2Int> RunRandomWalk(SimpleRandomWalkSO parameters, Vector2Int posiiton)
    {
        var currentPosition = posiiton;
        HashSet<Vector2Int> floorPsotion = new HashSet<Vector2Int>();
        for(int i = 0; i < parameters.iterations; i++)
        {
            var path = ProveduralGenerationAlgorithms.SimpleRandomWalk(currentPosition, parameters.walkLength);
            floorPsotion.UnionWith(path);
            if (parameters.startRandomlyEachIteration)
            {
                currentPosition = floorPsotion.ElementAt(RandomManager.Instance.GetRandomInt(0, floorPsotion.Count));
            }
        }
        return floorPsotion;
    }

}
