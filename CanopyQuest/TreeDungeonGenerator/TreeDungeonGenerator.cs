using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TreeDungeonGenerator : SimpleRandomWalkDungeonGenerator
{
    public static TreeDungeonGenerator instance;
    [SerializeField]
    private GameObject[] areas;
    [SerializeField]
    protected int minRoomWidth = 4, minroomHeight = 4;
    [SerializeField]
    [Range(0, 10)]
    protected int offset = 1;
    [SerializeField]
    protected bool randomWalkRooms = false;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    protected float lerpParameter = 0.5f;
    [SerializeField]
    protected int corridorWidthMin = 3;
    [SerializeField]
    protected int corridorWidthMax = 5;
    [SerializeField]
    private int playerHeight = 2;
    [SerializeField]
    private int playerJumpHeight = 3;
    [SerializeField]
    private int platformLengthMax = 8;
    [SerializeField]
    private float platformPossibility = 1.0f;
    [SerializeField]
    private float spikePossibility = 0.7f;
    [SerializeField]
    private float chestPossibility = 0.3f;
    [SerializeField]
    private int spikeLengthMax = 6;
    [SerializeField]
    private int torchNumInRoomMax = 3;
    [SerializeField]
    private GameObject torch;
    [SerializeField]
    private GameObject chest;
    [SerializeField]
    private GameObject trahBin;
    [SerializeField]
    private GameObject trahBinPanel;
    [SerializeField]
    private GameObject door;
    [SerializeField]
    private float trashBinPossibility = 0.1f;
    [SerializeField]
    private int coinNeedMin = 5;
    [SerializeField]
    private int coinNeedMax = 15;
    [SerializeField]
    private GameObject birthPlace;
    private Canvas canvas;
    private List<BoundsInt>areaList;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        RunProceduralGeneration();
    }


    [Serializable]
    public class EnemyInfo : IComparable<EnemyInfo>
    {
        public GameObject prefab;
        public Vector3Int size;
        public Vector3 positionOffset;
        public float possibility;
        public int threaten;

        public EnemyInfo(GameObject prefab, Vector3Int size, Vector3 positionOffset, float possibility, int threaten)
        {
            this.prefab = prefab;
            this.size = size;
            this.positionOffset = positionOffset;
            this.possibility = possibility;
            this.threaten = threaten;
        }

        public int CompareTo(EnemyInfo other)
        {
            if (other == null)
            {
                return 1;
            }

            // Sort in ascending order of probability
            return this.possibility.CompareTo(other.possibility);
        }
    }


    public List<EnemyInfo> enemyList;


    HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
    HashSet<Vector2Int> wall = new HashSet<Vector2Int>();
    HashSet<Vector2Int>[] corridors = { new HashSet<Vector2Int>(), new HashSet<Vector2Int>() };//0 horizontal or nonsteep; 1 vertical
    List<List<Vector2Int>> roomCenterList = new List<List<Vector2Int>>();
    public HashSet<BoundsInt> roomList = new HashSet<BoundsInt>();
    Dictionary<Vector2Int, Tuple<int, int>> ladderPosiitonInRoom = new Dictionary<Vector2Int, Tuple<int, int>>();
    HashSet<Vector2Int> oneWayPlatforms = new HashSet<Vector2Int>();
    HashSet<Vector2Int> spikes = new HashSet<Vector2Int>();
    Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom = new Dictionary<BoundsInt, HashSet<BoundsInt>>();
    HashSet<GameObject> mapGameObjectPool = new HashSet<GameObject>();



    private List<BoundsInt> SpliteSpaceHorizontallyWithSpecificArea(BoundsInt space, BoundsInt area)
    {
        BoundsInt space0 = new BoundsInt(space.xMin, space.yMin, space.zMin, area.xMin - space.xMin, space.size.y, space.size.z);
        BoundsInt space1 = new BoundsInt(area.xMax, space.yMin, space.zMin, space.xMax - area.xMax, space.size.y, space.size.z);
        List<BoundsInt> spaceRemain = new List<BoundsInt>();
        if (space0.size.x > 0)
            spaceRemain.Add(space0);
        if (space1.size.x > 0)
            spaceRemain.Add(space1);
        return spaceRemain;
    }

    private void SpliteSpaceVertically(BoundsInt room, HashSet<BoundsInt> outputSpaceSet)
    {
        int minY = room.min.y;
        int maxY = room.max.y;
        int splitLength;

        // 循环切分
        for (int y = minY; y < maxY; y += splitLength)
        {
            splitLength = RandomManager.Instance.GetRandomInt(playerHeight, playerJumpHeight + 1);
            int currentYMin = y;
            int currentYMax = Mathf.Min(y + splitLength, maxY);
            if (maxY != currentYMax && maxY - currentYMax < playerHeight)
            {
                splitLength = maxY - currentYMin - playerHeight;
                currentYMax = y + splitLength;
            }

            // 创建切分后的 Bounds
            BoundsInt splitBound;
            //if (y + 2*splitLength < maxY)
            //{
            //    splitBound = new BoundsInt(room.min.x, currentYMin, room.min.z,
            //                                    room.size.x, currentYMax - currentYMin, room.size.z);
            //}
            //else
            //{
            //    splitBound = new BoundsInt(room.min.x, currentYMin, room.min.z,
            //                                    room.size.x, maxY - currentYMin, room.size.z);
            //    outputSpaceSet.Add(splitBound);
            //    return;
            //}
            splitBound = new BoundsInt(room.min.x, currentYMin, room.min.z,
                                                room.size.x, currentYMax - currentYMin, room.size.z);
            outputSpaceSet.Add(splitBound);
        }
    }
    private void GenerateEnvironments()
    {
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        bool haveBirthPlace = false;
        bool haveDoor = false;
        foreach (var room in roomList)
        {
            spaceInRoom.Add(room, new HashSet<BoundsInt>());
            var center = new Vector2Int(Mathf.RoundToInt(room.center.x), Mathf.RoundToInt(room.center.y));
            //splite space in room
            SpliteSpaceVertically(room, spaceInRoom[room]);

            //if (room.size.y < playerHeight + playerJumpHeight)//the room is not high enough to need platforms
            //    continue;

            // If in the last region, generate door
            if (!haveDoor && areaList[areaList.Count - 1].Contains(room.min)&& areaList[areaList.Count - 1].Contains(room.max))
            {
                haveDoor =GenerateDoorInSapce(spaceInRoom, room);
            }

            //Generate platforms
            if (ladderPosiitonInRoom.ContainsKey(center))
            {
                GeneratePlatformInSpace(spaceInRoom, room, 1.0f, ladderPosiitonInRoom[center]);//must generate platform
            }
            else
            {
                GeneratePlatformInSpace(spaceInRoom, room, platformPossibility, null);
            }
            //Generate birthPlace
            if (!haveBirthPlace)
            {
                haveBirthPlace = GenerateBirthPlace(spaceInRoom, room);
            }
            //Generate enemies
            GenerateEnemysInSapce(spaceInRoom, room, RandomManager.Instance.GetRandomInt(0, enemyList[0].threaten * 2));
            // Generate spike
            GenerateSpikeInSpace(spaceInRoom, room, spikePossibility);
            // Generate chests
            GenerateChestsInSapce(spaceInRoom, room, chestPossibility);
            //Generate torchs
            GenerateTorchsInSapce(spaceInRoom, room, torchNumInRoomMax);
            //Generate trashBins
            GenerateTrashBinInSapce(spaceInRoom, room, trashBinPossibility);
        }
    }


    private bool GenerateBirthPlace(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room)
    {
        HashSet<BoundsInt> motifiedSpace = new HashSet<BoundsInt>();
        bool isGenerated = false;
        Vector3Int birthPlaceSize = new Vector3Int(4, 2, 0);
        foreach (var space in spaceInRoom[room])
        {

            if (!isGenerated && space.yMin == room.yMin && space.size.x >= birthPlaceSize.x && space.size.y >= birthPlaceSize.y)
            {
                isGenerated = true;
                Vector3Int birthPosiiton = new Vector3Int(RandomManager.Instance.GetRandomInt(space.xMin, space.xMax - birthPlaceSize.x + 1), space.yMin, 0);
                birthPlace.transform.position = birthPosiiton;
                var remainSpaces = SpliteSpaceHorizontallyWithSpecificArea(space, new BoundsInt(birthPosiiton, birthPlaceSize));
                foreach (var remain in remainSpaces)
                {
                    motifiedSpace.Add(remain);
                }

            }
            if (!isGenerated)
                motifiedSpace.Add(space);
        }
        spaceInRoom[room] = motifiedSpace;
        return isGenerated;
    }

    private void GenerateEnemysInSapce(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room, int threatenValueMax)
    {
        int threatenValue = RandomManager.Instance.GetRandomInt(0, threatenValueMax);
        HashSet<BoundsInt> motifiedSpace = new HashSet<BoundsInt>();
        foreach (var space in spaceInRoom[room])
        {
            bool isGenerated = false;
            foreach (var enemy in enemyList)
            {
                if (!isGenerated && threatenValue >= enemy.threaten && space.size.x >= enemy.size.x && space.size.y >= enemy.size.y && RandomManager.Instance.GetRandomFloat(0, 1) < enemy.possibility)
                {
                    isGenerated = true;
                    Vector3Int enemyPosiiton = new Vector3Int(RandomManager.Instance.GetRandomInt(space.xMin, space.xMax - enemy.size.x + 1), space.yMin, 0);
                    threatenValue -= enemy.threaten;
                    mapGameObjectPool.Add(Instantiate(enemy.prefab, enemyPosiiton + enemy.positionOffset, Quaternion.identity));
                    var remainSpaces = SpliteSpaceHorizontallyWithSpecificArea(space, new BoundsInt(enemyPosiiton, enemy.size));
                    foreach (var remain in remainSpaces)
                    {
                        motifiedSpace.Add(remain);
                    }
                }
            }
            if (!isGenerated)
                motifiedSpace.Add(space);
        }
        spaceInRoom[room] = motifiedSpace;
    }

    private void GenerateTorchsInSapce(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room, int torchNumInRoomMax)
    {
        int torchNum = RandomManager.Instance.GetRandomInt(0, torchNumInRoomMax);
        HashSet<BoundsInt> motifiedSpace = new HashSet<BoundsInt>();
        foreach (var space in spaceInRoom[room])
        {
            if (torchNum > 0)
            {
                Vector3Int torchPosiiton = new Vector3Int(RandomManager.Instance.GetRandomInt(space.xMin, space.xMax), RandomManager.Instance.GetRandomInt(space.yMin, space.yMax), 0);
                mapGameObjectPool.Add(Instantiate(torch, torchPosiiton, Quaternion.identity));
                var remainSpaces = SpliteSpaceHorizontallyWithSpecificArea(space, new BoundsInt(torchPosiiton, new Vector3Int(1, 1, 0)));
                foreach (var remain in remainSpaces)
                {
                    motifiedSpace.Add(remain);
                }
                torchNum--;
            }
            else
            {
                motifiedSpace.Add(space);
            }
        }
        spaceInRoom[room] = motifiedSpace;
    }

    private void GenerateChestsInSapce(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room, float chestPossibility)
    {
        if (RandomManager.Instance.GetRandomFloat(0.0f, 1.0f) < chestPossibility)
        {
            bool isGenerated = false;
            HashSet<BoundsInt> motifiedSpace = new HashSet<BoundsInt>();
            foreach (var space in spaceInRoom[room])
            {
                if (!isGenerated && space.yMin == room.yMin && space.size.x >= 2 && space.size.y >= 2)
                {
                    Vector3Int chestPosiiton = new Vector3Int(RandomManager.Instance.GetRandomInt(space.xMin, space.xMax - 1), space.yMin, 0);
                    isGenerated = true;
                    mapGameObjectPool.Add(Instantiate(chest, chestPosiiton, Quaternion.identity));
                    var remainSpaces = SpliteSpaceHorizontallyWithSpecificArea(space, new BoundsInt(chestPosiiton, new Vector3Int(2, 2, 0)));
                    foreach (var remain in remainSpaces)
                    {
                        motifiedSpace.Add(remain);
                    }
                }
                else
                {
                    motifiedSpace.Add(space);
                }
            }
            spaceInRoom[room] = motifiedSpace;
        }
    }

    private void GenerateTrashBinInSapce(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room, float trashBinPossibility)
    {
        if (RandomManager.Instance.GetRandomFloat(0.0f, 1.0f) < trashBinPossibility)
        {
            bool isGenerated = false;
            HashSet<BoundsInt> motifiedSpace = new HashSet<BoundsInt>();
            foreach (var space in spaceInRoom[room])
            {
                if (!isGenerated && space.yMin == room.yMin)
                {
                    Vector3Int trahBinPosiiton = new Vector3Int(RandomManager.Instance.GetRandomInt(space.xMin, space.xMax), space.yMin, 0);
                    isGenerated = true;
                    var gbTrashBIn = Instantiate(trahBin, trahBinPosiiton, Quaternion.identity);
                    var gbTrashBinPanel = Instantiate(trahBinPanel, canvas.transform);
                    var panelScript = gbTrashBinPanel.GetComponent<TrashBinPanelNew>();
                    var trashBinScript = gbTrashBIn.GetComponent<TrashBinNew>();
                    trashBinScript.panel = panelScript;
                    panelScript.trashBinPosition = gbTrashBIn.transform;
                    int coinMax = RandomManager.Instance.GetRandomInt(coinNeedMin, coinNeedMax);
                    trashBinScript.SetValueAndCoinNeeded(RandomManager.Instance.GetRandomInt(coinMax / 2, coinMax * 3 / 2), coinMax);
                    trashBinScript.Initialize();
                    mapGameObjectPool.Add(gbTrashBIn);
                    mapGameObjectPool.Add(gbTrashBinPanel);
                    var remainSpaces = SpliteSpaceHorizontallyWithSpecificArea(space, new BoundsInt(trahBinPosiiton, new Vector3Int(1, 1, 0)));
                    foreach (var remain in remainSpaces)
                    {
                        motifiedSpace.Add(remain);
                    }
                }
                else
                {
                    motifiedSpace.Add(space);
                }
            }
            spaceInRoom[room] = motifiedSpace;
        }
    }

    private void GenerateSpikeInSpace(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room, float possibility)
    {
        if (RandomManager.Instance.GetRandomFloat(0, 1.0f) < possibility)
        {
            foreach (var space in spaceInRoom[room])
            {
                if (space.yMin == room.yMin)
                {
                    var spkieCenter = RandomManager.Instance.GetRandomInt(space.xMin, space.xMax + 1);
                    for (int i = Mathf.Max(Mathf.Min(space.xMin, spkieCenter), spkieCenter - RandomManager.Instance.GetRandomInt(0, spikeLengthMax / 2)); i < Mathf.Min(Mathf.Max(space.xMax, spkieCenter), spkieCenter + RandomManager.Instance.GetRandomInt(1, spikeLengthMax / 2)); i++)
                    {
                        if (!floor.Contains(new Vector2Int(i, space.yMin - 1)))
                            spikes.Add(new Vector2Int(i, space.yMin));
                    }
                }
            }
        }
    }

    private bool GenerateDoorInSapce(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room)
    {
        Vector3Int doorSize = new Vector3Int(2, 2, 0);
        HashSet<BoundsInt> motifiedSpace = new HashSet<BoundsInt>();
        bool isGenerated = false;
        foreach (var space in spaceInRoom[room])
        {
            if (!isGenerated)
            {
                if (space.size.x >= doorSize.x && space.size.y >= doorSize.y)
                {
                    isGenerated = true;
                    Vector3Int dooryPosiiton = new Vector3Int(RandomManager.Instance.GetRandomInt(space.xMin, space.xMax - doorSize.x + 1), space.yMin, 0);
                    mapGameObjectPool.Add(Instantiate(door, dooryPosiiton, Quaternion.identity));
                    var remainSpaces = SpliteSpaceHorizontallyWithSpecificArea(space, new BoundsInt(dooryPosiiton, doorSize));
                    foreach (var remain in remainSpaces)
                    {
                        motifiedSpace.Add(remain);
                    }

                }
            }
            else
            {
                motifiedSpace.Add(space);
            }
            spaceInRoom[room] = motifiedSpace;
        }
        return isGenerated;
    }

    private void GeneratePlatformInSpace(Dictionary<BoundsInt, HashSet<BoundsInt>> spaceInRoom, BoundsInt room, float possibility, Tuple<int, int>? ladderPosition)
    {
        HashSet<BoundsInt> motifiedSpace = new HashSet<BoundsInt>();
        foreach (var space in spaceInRoom[room])
        {
            if (space.yMax != room.yMax)
            {
                if (possibility == 1.0f || RandomManager.Instance.GetRandomFloat(0, 1.0f) < possibility)
                {
                    motifiedSpace.Add(new BoundsInt(space.xMin, space.yMin, space.zMin, space.size.x, space.size.y - 1, space.size.z));//remove the uppest 1 layer as platform
                    if (ladderPosition != null)
                    {
                        for (int i = Mathf.Max(space.xMin, ladderPosition.Item1 - RandomManager.Instance.GetRandomInt(0, platformLengthMax / 2)); i < Mathf.Min(space.xMax, ladderPosition.Item1 + RandomManager.Instance.GetRandomInt(1, platformLengthMax / 2)); i++)
                        {
                            if (!corridors[1].Contains(new Vector2Int(i, space.yMax + 1)) && floor.Contains(new Vector2Int(i, space.yMax + 1)) && !oneWayPlatforms.Contains(new Vector2Int(i, space.yMax - 2)) && !oneWayPlatforms.Contains(new Vector2Int(i, space.yMax)))
                                oneWayPlatforms.Add(new Vector2Int(i, space.yMax - 1));
                        }

                        if (ladderPosition.Item2 <= space.xMax && ladderPosition.Item2 >= space.xMin)
                        {
                            for (int i = Mathf.Max(room.xMin, ladderPosition.Item2 - RandomManager.Instance.GetRandomInt(0, platformLengthMax / 2)); i < Mathf.Min(space.xMax, ladderPosition.Item2 + RandomManager.Instance.GetRandomInt(1, platformLengthMax / 2)); i++)
                            {
                                if (!corridors[1].Contains(new Vector2Int(i, space.yMax + 1)) && floor.Contains(new Vector2Int(i, space.yMax + 1)) && !oneWayPlatforms.Contains(new Vector2Int(i, space.yMax - 2)) && !oneWayPlatforms.Contains(new Vector2Int(i, space.yMax)))
                                    oneWayPlatforms.Add(new Vector2Int(i, space.yMax - 1));
                            }
                        }
                    }
                    else
                    {
                        var platformCenter = RandomManager.Instance.GetRandomInt(room.xMin, room.xMax + 1);
                        for (int i = Mathf.Max(space.xMin, platformCenter - RandomManager.Instance.GetRandomInt(0, platformLengthMax / 2)); i < Mathf.Min(space.xMax, platformCenter + RandomManager.Instance.GetRandomInt(1, platformLengthMax / 2)); i++)
                        {
                            if (!corridors[1].Contains(new Vector2Int(i, space.yMax + 1)) && floor.Contains(new Vector2Int(i, space.yMax + 1)) && !oneWayPlatforms.Contains(new Vector2Int(i, space.yMax - 2)) && !oneWayPlatforms.Contains(new Vector2Int(i, space.yMax)))
                                oneWayPlatforms.Add(new Vector2Int(i, space.yMax - 1));
                        }
                    }
                }
            }
            else
            {
                motifiedSpace.Add(space);
            }
        }
        spaceInRoom[room] = motifiedSpace;
    }


    private List<BoundsInt> GetAreaList()
    {

        List<BoundsInt> areaList = new List<BoundsInt>();
        foreach (var area in areas)
        {
            var rec = area.GetComponent<RectangleVisualizer>().Rectangle;
            areaList.Add(new BoundsInt((int)rec.x, (int)rec.y, -1, (int)rec.width, (int)rec.height, 2));
        }
        return areaList;
    }
    protected override void RunProceduralGeneration()
    {
        //clear all data
        floor.Clear();
        wall.Clear();
        oneWayPlatforms.Clear();
        spaceInRoom.Clear();
        ladderPosiitonInRoom.Clear();
        spikes.Clear();
        tileMapVisualizer.Clear();
        foreach (var gameobject in mapGameObjectPool)
        {
            DestroyImmediate(gameobject);
        }
        mapGameObjectPool.Clear();

        foreach (var corridor in corridors)
        {
            corridor.Clear();
        }
        roomCenterList.Clear();
        roomList.Clear();

        //set random seed
        if (DataHolder.instance!=null&&DataHolder.instance.seedCode!=null)
        {
            RandomManager.Instance.SetSeed(DataHolder.instance.seedCode.Value);
        }
        else
        {
            RandomManager.Instance.SetSeed(UnityEngine.Random.Range(0,1000));
        }



        areaList = GetAreaList();
        //generate rooms and corridors in specific areas,
        foreach (var area in areaList)
        {
            HashSet<Vector2Int> floorInArea;
            HashSet<Vector2Int>[] corridorsInArea;
            List<Vector2Int> roomCenters;
            List<BoundsInt> roomListInArea;
            CreateRoomsInArea(area, out floorInArea, out corridorsInArea, out roomCenters, out roomListInArea);
            floor.UnionWith(floorInArea);
            roomCenterList.Add(roomCenters);
            for (int i = 0; i < corridors.Length; i++)
            {
                corridors[i].UnionWith(corridorsInArea[i]);
            }
            foreach (var room in roomListInArea)
            {
                roomList.Add(room);
            }
        }
        //connect different areas
        for (int i = 0; i < areaList.Count - 1; i++)
        {
            var center1 = FindClosestPointTo(new Vector2Int(Mathf.RoundToInt(areaList[i].center.x), Mathf.RoundToInt(areaList[i].center.y)), roomCenterList[i + 1]);
            var center0 = FindClosestPointTo(center1, roomCenterList[i]);
            CreateCorridor(center0, center1, corridorWidthMin, corridorWidthMax, corridors, ladderPosiitonInRoom);
        }
        // reorganize corridors
        for (int i = corridors.Length - 1; i >= 0; i--)
        {
            corridors[i].ExceptWith(floor);
            floor.UnionWith(corridors[i]);
        }

        GenerateEnvironments();

        //create floor
        tileMapVisualizer.PaintFloorTile(floor);
        //create ladders
        tileMapVisualizer.PaintLadders(corridors[1]);
        //create platforms
        tileMapVisualizer.PaintPlatforms(oneWayPlatforms);
        //create spikes
        tileMapVisualizer.PaintSpikes(spikes);


        //create walls
        WallGenerator.CreateWalls(floor, tileMapVisualizer);


    }


    protected void CreateRoomsInArea(BoundsInt area, out HashSet<Vector2Int> floorInArea, out HashSet<Vector2Int>[] corridorsInArea, out List<Vector2Int> roomCenters, out List<BoundsInt> roomListInArea)
    {
        floorInArea = new HashSet<Vector2Int>();
        roomCenters = new List<Vector2Int>();
        corridorsInArea = new HashSet<Vector2Int>[2];
        for (int i = 0; i < corridorsInArea.Length; i++)
        {
            corridorsInArea[i] = new HashSet<Vector2Int>();
        }

        roomListInArea = ProveduralGenerationAlgorithms.BinarySpacePartitioning(area, minRoomWidth, minroomHeight);

        if (randomWalkRooms)
        {
            floorInArea = CreateRandomRoom(roomListInArea);
        }
        else
        {
            floorInArea = CreateSimpleRooms(roomListInArea);
        }

        foreach (var room in roomListInArea)
        {
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(room.center));
        }
        ConnectRoomsInArea(new List<Vector2Int>(roomCenters), corridorsInArea, ladderPosiitonInRoom);

        //tileMapVisualizer.PaintFloorTile(floor);
        //WallGenerator.CreateWalls(floor, tileMapVisualizer);
    }

    private HashSet<Vector2Int> CreateSimpleRooms(List<BoundsInt> roomList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        for (int i = 0; i < roomList.Count; i++)
        {
            var room = roomList[i];
            for (int col = offset; col < room.size.x - offset; col++)
            {

                for (int row = offset; row < room.size.y - offset; row++)
                {
                    floor.Add(new Vector2Int(room.min.x + col, room.min.y + row));
                }
            }
            roomList[i] = new BoundsInt(room.xMin + offset, room.yMin + offset, 0, room.size.x - 2 * offset, room.size.y - 2 * offset, 0);
        }
        return floor;
    }

    private HashSet<Vector2Int> CreateRandomRoom(List<BoundsInt> roomList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        for (int i = 0; i < roomList.Count; i++)
        {
            var roomBound = roomList[i];
            var roomCenter = new Vector2Int(Mathf.RoundToInt(roomBound.center.x), Mathf.RoundToInt(roomBound.center.y));
            var roomFloor = RunRandomWalk(randomWalkParameters, roomCenter);
            foreach (var position in roomFloor)
            {
                if (position.x >= (roomBound.xMin + offset) && position.x <= (roomBound.xMax - offset) && position.y >= (roomBound.yMin + offset) && position.y <= (roomBound.yMax - offset))
                {
                    floor.Add(position);
                }
            }

            roomList[i] = new BoundsInt(roomBound.xMin + offset, roomBound.yMin + offset, 0, roomBound.x - 2 * offset, roomBound.y - 2 * offset, 0);
        }
        return floor;
    }

    private void ConnectRoomsInArea(in List<Vector2Int> roomCenters, HashSet<Vector2Int>[] corridors, Dictionary<Vector2Int, Tuple<int, int>> ladderPosiitonInRoom)
    {
        var currentCenter = roomCenters[RandomManager.Instance.GetRandomInt(0, roomCenters.Count)];
        roomCenters.Remove(currentCenter);
        while (roomCenters.Count > 0)
        {
            Vector2Int closestCenter = FindClosestPointTo(currentCenter, roomCenters);
            roomCenters.Remove(closestCenter);
            int corridorType;
            CreateCorridor(currentCenter, closestCenter, corridorWidthMin, corridorWidthMax, corridors, ladderPosiitonInRoom);
            currentCenter = closestCenter;
        }
    }


    private void CreateCorridor(Vector2Int currentCenter, Vector2Int destination, int corridorWidthMin, int corridorWidthMax, HashSet<Vector2Int>[] corridors, Dictionary<Vector2Int, Tuple<int, int>> ladderPosiitonInRoom)
    {
        bool singleDirectionCorridorMode;
        var dirvec = destination - currentCenter;
        corridorWidthMin = Mathf.Max(playerHeight, corridorWidthMin);
        if (dirvec.x == 0 || Mathf.Abs((float)dirvec.y / dirvec.x) > 1.0f)
        {
            singleDirectionCorridorMode = false;
        }
        else
        {
            singleDirectionCorridorMode = true;
        }
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        var position = currentCenter;
        corridor.Add(position);
        if (singleDirectionCorridorMode)
        {
            while (position != destination)
            {
                //corridorWidth = (int)Mathf.Lerp(corridorWidth, Random.Range(corridorWidthMin, corridorWidthMax + 1), lerpParameter);
                int corridorWidth = RandomManager.Instance.GetRandomInt(corridorWidthMin, corridorWidthMax + 1);

                Vector2Int directionNext = Direction2D.cardinalDirectionsList[0];
                var distance = Vector2.Distance(position, destination);
                foreach (var direction in Direction2D.cardinalDirectionsList)
                {
                    if (distance > Vector2.Distance(position + direction, destination))
                    {
                        directionNext = direction;
                        distance = Vector2.Distance(directionNext + position, destination);
                    }

                }
                for (int length = (int)(-corridorWidth / 2); length <= (int)(corridorWidth / 2); length++)
                {
                    foreach (var dir in Direction2D.cardinalDirectionsList)
                    {
                        if (dir != directionNext)
                            corridor.Add(position + dir * length);
                    }
                }
                position = position + directionNext;

            }
            corridors[0].UnionWith(corridor);
        }
        else
        {
            while (position.x != destination.x)
            {
                int corridorWidth = RandomManager.Instance.GetRandomInt(corridorWidthMin, corridorWidthMax + 1);
                Vector2Int directionNext = position.x > destination.x ? new Vector2Int(-1, 0) : new Vector2Int(1, 0);
                for (int length = (int)(-corridorWidth / 2); length <= (int)(corridorWidth / 2); length++)
                {
                    foreach (var dir in Direction2D.cardinalDirectionsList)
                    {
                        if (dir != directionNext)
                            corridor.Add(position + dir * length);
                    }
                }
                position = position + directionNext;
            }
            corridors[0].UnionWith(corridor);
            corridor.Clear();
            while (position.y != destination.y)
            {
                Vector2Int directionNext = position.y > destination.y ? new Vector2Int(0, -1) : new Vector2Int(0, 1);
                corridor.Add(position + directionNext);
                position = position + directionNext;
            }
            corridors[1].UnionWith(corridor);
            var lowerRoom = currentCenter.y > destination.y ? destination : currentCenter;
            if (ladderPosiitonInRoom.ContainsKey(lowerRoom))
            {
                ladderPosiitonInRoom[lowerRoom] = Tuple.Create(ladderPosiitonInRoom[lowerRoom].Item1, destination.x);
            }
            else
            {
                ladderPosiitonInRoom.Add(lowerRoom, Tuple.Create(destination.x, int.MinValue));
            }
        }
    }

    private Vector2Int FindClosestPointTo(Vector2Int currentCenter, List<Vector2Int> roomCenters)
    {
        Vector2Int closestCorridor = Vector2Int.zero;
        float length = float.MaxValue;
        foreach (var center in roomCenters)
        {
            float currentDistance = Vector2.Distance(currentCenter, center);
            if (length > currentDistance)
            {
                length = currentDistance;
                closestCorridor = center;
            }
        }
        return closestCorridor;
    }


}
