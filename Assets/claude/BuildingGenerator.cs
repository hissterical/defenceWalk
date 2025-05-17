//using System.Collections.Generic;
//using UnityEngine;
//using System.IO;
//using System.Linq;

//#region Data Models
//[System.Serializable]
//public class Point
//{
//    public float x, y;

//    public Vector3 ToVector3(float elevation = 0)
//    {
//        return new Vector3(x, elevation, y);
//    }
//}

//[System.Serializable]
//public class Wall
//{
//    public string id;
//    public Point start, end;
//    public float height, thickness;
//}

//[System.Serializable]
//public class Door
//{
//    public string id;
//    public string wall_id;
//    public float width, height;
//    public Point position;
//}

//[System.Serializable]
//public class Window
//{
//    public string id;
//    public string wall_id;
//    public float width, height;
//    public Point position;
//    public float sill_height;
//}

//[System.Serializable]
//public class BuildingDimensions
//{
//    public float length;
//    public float width;
//    public float reference_height;
//}

//[System.Serializable]
//public class Floor
//{
//    public string id, name;
//    public float elevation, height, thickness;
//    public List<Point> boundaries;
//    public List<Wall> walls;
//    public List<Door> doors;
//    public List<Window> windows;
//}

//[System.Serializable]
//public class Stair
//{
//    public string id;
//    public string bottom_floor, top_floor;
//    public Point bottom_point, top_point;
//    public float width, length;
//    public string direction;
//}

//[System.Serializable]
//public class Building
//{
//    public string name;
//    public BuildingDimensions dimensions;
//    public List<Floor> floors;
//    public List<Stair> stairs;
//}

//[System.Serializable]
//public class BuildingData
//{
//    public Building building;
//}
//#endregion

//public class BuildingGenerator : MonoBehaviour
//{
//    [Header("Building JSON")]
//    [TextArea(10, 20)] public string buildingJson;

//    [Header("Materials")]
//    public Material wallMaterial;
//    public Material floorMaterial;
//    public Material stairMaterial;
//    public Material doorMaterial;
//    public Material windowMaterial;

//    [Header("Prefabs")]
//    public GameObject stairPrefab;
//    public GameObject doorPrefab;
//    public GameObject windowPrefab;

//    [Header("Settings")]
//    public bool createStairOpenings = true;
//    public float openingMargin = 0.5f; // Extra space around stair openings
//    public bool applyScaling = false;
//    public float scaleFactor = 1.0f;
//    public float referenceDoorWidth = 0.9f; // Standard door width in meters

//    [Header("Door Settings")]
//    public bool cutDoorsInWalls = true;
//    public float doorFrameThickness = 0.1f;

//    private GameObject buildingContainer;
//    private Dictionary<string, Floor> floorDictionary = new Dictionary<string, Floor>();
//    private Dictionary<string, Wall> wallDictionary = new Dictionary<string, Wall>();

//    void Start() => GenerateBuildingFromJson();

//    #region Public Methods
//    public void GenerateBuildingFromJson()
//    {
//        if (string.IsNullOrEmpty(buildingJson))
//        {
//            Debug.LogError("Building JSON is empty or null!");
//            return;
//        }

//        CleanupExistingBuilding();

//        try
//        {
//            var data = JsonUtility.FromJson<BuildingData>(buildingJson);
//            if (data == null || data.building == null)
//            {
//                Debug.LogError("Failed to parse building JSON!");
//                return;
//            }

//            // Create floor and wall lookup dictionaries
//            floorDictionary.Clear();
//            wallDictionary.Clear();
//            foreach (var floor in data.building.floors)
//            {
//                floorDictionary[floor.id] = floor;

//                if (floor.walls != null)
//                {
//                    foreach (var wall in floor.walls)
//                    {
//                        wallDictionary[wall.id] = wall;
//                    }
//                }
//            }

//            // Generate building structure
//            CreateBuildingStructure(data.building);
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"Error generating building: {e.Message}\n{e.StackTrace}");
//        }
//    }

//    public void LoadJsonFromFile(string path)
//    {
//        if (File.Exists(path))
//        {
//            buildingJson = File.ReadAllText(path);
//            GenerateBuildingFromJson();
//        }
//        else Debug.LogError($"File not found: {path}");
//    }
//    #endregion

//    #region Building Generation
//    private void CleanupExistingBuilding()
//    {
//        if (buildingContainer != null)
//        {
//            if (Application.isPlaying)
//                Destroy(buildingContainer);
//            else
//                DestroyImmediate(buildingContainer);
//        }

//        buildingContainer = new GameObject("Building");
//        buildingContainer.transform.SetParent(transform);
//    }

//    private void CreateBuildingStructure(Building building)
//    {
//        // Calculate scaling factor if needed
//        float scale = 1.0f;
//        if (applyScaling)
//        {
//            if (building.dimensions != null)
//            {
//                // Calculate based on building dimensions
//                Debug.Log($"Building dimensions: {building.dimensions.length}x{building.dimensions.width}");
//                // Let user specify scaling through the inspector
//                scale = scaleFactor;
//            }
//            else
//            {
//                // Try to calculate based on doors
//                scale = CalculateScaleFromDoors(building);
//            }

//            if (scale != 1.0f)
//            {
//                Debug.Log($"Applied building scale factor: {scale}");
//            }
//        }

//        // First, identify where stairs connect to floors
//        Dictionary<string, List<StairOpening>> floorOpenings = IdentifyStairOpenings(building.stairs, scale);

//        // Create all floors
//        Dictionary<string, GameObject> floorObjects = new Dictionary<string, GameObject>();
//        foreach (var floor in building.floors)
//        {
//            GameObject floorObj = CreateFloor(floor, floorOpenings, scale);
//            floorObjects[floor.id] = floorObj;
//        }

//        // Create all stairs
//        if (building.stairs != null)
//        {
//            foreach (var stair in building.stairs)
//            {
//                CreateStair(stair, floorObjects, scale);
//            }
//        }

//        // Create doors and windows
//        foreach (var floor in building.floors)
//        {
//            if (floor.doors != null && floor.doors.Count > 0)
//            {
//                foreach (var door in floor.doors)
//                {
//                    CreateDoor(door, floor, floorObjects[floor.id].transform, scale);
//                }
//            }

//            if (floor.windows != null && floor.windows.Count > 0)
//            {
//                foreach (var window in floor.windows)
//                {
//                    CreateWindow(window, floor, floorObjects[floor.id].transform, scale);
//                }
//            }
//        }
//    }

//    private float CalculateScaleFromDoors(Building building)
//    {
//        float scale = 1.0f;
//        float avgDoorWidth = 0f;
//        int doorCount = 0;

//        // Gather door widths from all floors
//        foreach (var floor in building.floors)
//        {
//            if (floor.doors != null && floor.doors.Count > 0)
//            {
//                foreach (var door in floor.doors)
//                {
//                    avgDoorWidth += door.width;
//                    doorCount++;
//                }
//            }
//        }

//        // If we have doors, calculate scale factor based on reference door width
//        if (doorCount > 0)
//        {
//            avgDoorWidth /= doorCount;
//            scale = referenceDoorWidth / avgDoorWidth;
//        }

//        return scale;
//    }

//    private GameObject CreateFloor(Floor floor, Dictionary<string, List<StairOpening>> floorOpenings, float scale = 1.0f)
//    {
//        var floorObj = new GameObject($"Floor_{floor.id}");
//        floorObj.transform.SetParent(buildingContainer.transform);

//        // Create floor and ceiling slabs
//        if (floor.boundaries != null && floor.boundaries.Count >= 3)
//        {
//            // Apply scaling to boundaries
//            List<Point> scaledBoundaries = ScalePoints(floor.boundaries, scale);
//            float scaledElevation = floor.elevation * scale;
//            float scaledThickness = floor.thickness * scale;

//            // Create floor slab with openings
//            List<StairOpening> openings = new List<StairOpening>();
//            if (floorOpenings.ContainsKey(floor.id))
//            {
//                openings = ScaleOpenings(floorOpenings[floor.id], scale);
//            }

//            CreateFloorSlab(scaledBoundaries, scaledElevation, scaledThickness,
//                floorMaterial, floorObj.transform, $"FloorSlab_{floor.id}", openings);

//            // Create ceiling slab with openings (only for openings marked as top)
//            List<StairOpening> ceilingOpenings = openings.Where(o => o.isTopOpening).ToList();
//            CreateFloorSlab(scaledBoundaries, scaledElevation + floor.height * scale, scaledThickness,
//                floorMaterial, floorObj.transform, $"CeilingSlab_{floor.id}", ceilingOpenings);
//        }
//        else
//        {
//            Debug.LogWarning($"Floor {floor.id} has invalid boundaries!");
//        }

//        // Create walls
//        if (floor.walls != null)
//        {
//            foreach (var wall in floor.walls)
//            {
//                CreateWall(wall, floor.elevation, floorObj.transform, scale);
//            }
//        }

//        return floorObj;
//    }

//    private List<Point> ScalePoints(List<Point> points, float scale)
//    {
//        if (scale == 1.0f) return points;

//        List<Point> scaled = new List<Point>();
//        foreach (var p in points)
//        {
//            scaled.Add(new Point { x = p.x * scale, y = p.y * scale });
//        }
//        return scaled;
//    }

//    private List<StairOpening> ScaleOpenings(List<StairOpening> openings, float scale)
//    {
//        if (scale == 1.0f) return openings;

//        List<StairOpening> scaled = new List<StairOpening>();
//        foreach (var opening in openings)
//        {
//            scaled.Add(new StairOpening
//            {
//                stairId = opening.stairId,
//                bounds = new Rect(
//                    opening.bounds.x * scale,
//                    opening.bounds.y * scale,
//                    opening.bounds.width * scale,
//                    opening.bounds.height * scale
//                ),
//                isTopOpening = opening.isTopOpening
//            });
//        }
//        return scaled;
//    }

//    private void CreateWall(Wall wall, float elevation, Transform parent, float scale = 1.0f)
//    {
//        if (wall.start == null || wall.end == null)
//        {
//            Debug.LogWarning($"Wall {wall.id} has null points!");
//            return;
//        }

//        // Apply scaling
//        Vector3 startPos = new Vector3(wall.start.x * scale, elevation * scale, wall.start.y * scale);
//        Vector3 endPos = new Vector3(wall.end.x * scale, elevation * scale, wall.end.y * scale);
//        float scaledHeight = wall.height * scale;
//        float scaledThickness = wall.thickness * scale;

//        float length = Vector3.Distance(startPos, endPos);

//        if (length < 0.001f)
//        {
//            Debug.LogWarning($"Wall {wall.id} has zero length!");
//            return;
//        }

//        Vector3 center = (startPos + endPos) / 2;
//        center.y += scaledHeight / 2;

//        // Store wall info for door cutouts
//        WallInfo wallInfo = new WallInfo
//        {
//            id = wall.id,
//            startPos = startPos,
//            endPos = endPos,
//            height = scaledHeight,
//            thickness = scaledThickness,
//            direction = Vector3.Normalize(endPos - startPos),
//            length = length
//        };

//        // Find doors in this wall
//        List<DoorInfo> doorsInWall = new List<DoorInfo>();
//        if (cutDoorsInWalls)
//        {
//            // Find all doors that belong to this wall
//            foreach (var floor in floorDictionary.Values)
//            {
//                if (floor.doors != null)
//                {
//                    foreach (var door in floor.doors)
//                    {
//                        if (door.wall_id == wall.id)
//                        {
//                            doorsInWall.Add(new DoorInfo
//                            {
//                                width = door.width * scale,
//                                height = door.height * scale,
//                                position = new Vector3(door.position.x * scale, elevation * scale, door.position.y * scale)
//                            });
//                        }
//                    }
//                }
//            }
//        }

//        if (doorsInWall.Count > 0 && cutDoorsInWalls)
//        {
//            CreateWallWithDoorCutouts(wallInfo, doorsInWall, parent);
//        }
//        else
//        {
//            // Create simple wall
//            var wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            wallObj.name = $"Wall_{wall.id}";
//            wallObj.transform.SetParent(parent);
//            wallObj.transform.position = center;

//            // Calculate wall rotation
//            float angle = Mathf.Atan2(endPos.z - startPos.z, endPos.x - startPos.x) * Mathf.Rad2Deg;
//            wallObj.transform.rotation = Quaternion.Euler(0, angle, 0);

//            // Set wall scale
//            wallObj.transform.localScale = new Vector3(length, scaledHeight, scaledThickness);

//            // Apply material
//            if (wallMaterial != null)
//            {
//                wallObj.GetComponent<MeshRenderer>().material = wallMaterial;
//            }
//        }
//    }

//    private struct WallInfo
//    {
//        public string id;
//        public Vector3 startPos;
//        public Vector3 endPos;
//        public float height;
//        public float thickness;
//        public Vector3 direction;
//        public float length;
//    }

//    private struct DoorInfo
//    {
//        public float width;
//        public float height;
//        public Vector3 position;
//    }

//    private void CreateWallWithDoorCutouts(WallInfo wall, List<DoorInfo> doors, Transform parent)
//    {
//        GameObject wallContainer = new GameObject($"Wall_{wall.id}_WithDoors");
//        wallContainer.transform.SetParent(parent);

//        // Calculate wall rotation
//        float angle = Mathf.Atan2(wall.endPos.z - wall.startPos.z, wall.endPos.x - wall.startPos.x) * Mathf.Rad2Deg;
//        Quaternion wallRotation = Quaternion.Euler(0, angle, 0);

//        // Sort doors by position along the wall
//        doors.Sort((a, b) => {
//            float distA = Vector3.Distance(wall.startPos, a.position);
//            float distB = Vector3.Distance(wall.startPos, b.position);
//            return distA.CompareTo(distB);
//        });

//        // Create wall segments between doors
//        float currentLength = 0;
//        Vector3 basePos = wall.startPos;
//        basePos.y += wall.height / 2;

//        for (int i = 0; i <= doors.Count; i++)
//        {
//            float segmentLength;

//            if (i == 0)
//            {
//                // First segment (from wall start to first door)
//                if (doors.Count > 0)
//                {
//                    float doorPos = GetPositionAlongWall(wall, doors[0].position);
//                    segmentLength = doorPos - doors[0].width / 2 - currentLength;
//                }
//                else
//                {
//                    segmentLength = wall.length;
//                }
//            }
//            else if (i == doors.Count)
//            {
//                // Last segment (from last door to wall end)
//                float doorPos = GetPositionAlongWall(wall, doors[i - 1].position);
//                segmentLength = wall.length - (doorPos + doors[i - 1].width / 2);
//            }
//            else
//            {
//                // Middle segment (between two doors)
//                float prevDoorPos = GetPositionAlongWall(wall, doors[i - 1].position);
//                float nextDoorPos = GetPositionAlongWall(wall, doors[i].position);
//                segmentLength = (nextDoorPos - doors[i].width / 2) - (prevDoorPos + doors[i - 1].width / 2);
//            }

//            // Create segment if it has positive length
//            if (segmentLength > 0.001f)
//            {
//                Vector3 segmentCenter = basePos + wall.direction * (currentLength + segmentLength / 2);

//                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                segment.name = $"WallSegment_{i}";
//                segment.transform.SetParent(wallContainer.transform);
//                segment.transform.position = segmentCenter;
//                segment.transform.rotation = wallRotation;
//                segment.transform.localScale = new Vector3(segmentLength, wall.height, wall.thickness);

//                if (wallMaterial != null)
//                {
//                    segment.GetComponent<MeshRenderer>().material = wallMaterial;
//                }
//            }

//            // Move current position past this door
//            if (i < doors.Count)
//            {
//                float doorPos = GetPositionAlongWall(wall, doors[i].position);
//                currentLength = doorPos + doors[i].width / 2;

//                // Create door frame (top part)
//                Vector3 framePos = basePos + wall.direction * doorPos;
//                framePos.y += (wall.height - doors[i].height) / 2;

//                GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                doorFrame.name = $"DoorFrame_Top_{i}";
//                doorFrame.transform.SetParent(wallContainer.transform);
//                doorFrame.transform.position = framePos;
//                doorFrame.transform.rotation = wallRotation;
//                doorFrame.transform.localScale = new Vector3(doors[i].width, wall.height - doors[i].height, wall.thickness);

//                if (wallMaterial != null)
//                {
//                    doorFrame.GetComponent<MeshRenderer>().material = wallMaterial;
//                }
//            }
//        }
//    }

//    private float GetPositionAlongWall(WallInfo wall, Vector3 position)
//    {
//        Vector3 toPosition = position - wall.startPos;
//        return Vector3.Dot(toPosition, wall.direction);
//    }

//    private void CreateDoor(Door door, Floor floor, Transform parent, float scale = 1.0f)
//    {
//        if (door.position == null)
//        {
//            Debug.LogWarning($"Door {door.id} has null position!");
//            return;
//        }

//        // Scale dimensions
//        float scaledWidth = door.width * scale;
//        float scaledHeight = door.height * scale;
//        Vector3 position = new Vector3(
//            door.position.x * scale,
//            floor.elevation * scale + scaledHeight / 2,
//            door.position.y * scale
//        );

//        if (doorPrefab != null)
//        {
//            // Use door prefab
//            GameObject doorObj = Instantiate(doorPrefab, position, Quaternion.identity, parent);
//            doorObj.name = $"Door_{door.id}";

//            // Determine rotation based on wall_id if available
//            if (!string.IsNullOrEmpty(door.wall_id))
//            {
//                float rotation = GetDoorRotation(door.wall_id, door, floor);
//                doorObj.transform.rotation = Quaternion.Euler(0, rotation, 0);
//            }

//            // Scale appropriately
//            doorObj.transform.localScale = new Vector3(scaledWidth, scaledHeight, doorFrameThickness * scale);
//        }
//        else
//        {
//            // Create simple door representation
//            GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            doorObj.name = $"Door_{door.id}";
//            doorObj.transform.SetParent(parent);
//            doorObj.transform.position = position;

//            // Determine rotation based on wall_id
//            if (!string.IsNullOrEmpty(door.wall_id))
//            {
//                float rotation = GetDoorRotation(door.wall_id, door, floor);
//                doorObj.transform.rotation = Quaternion.Euler(0, rotation, 0);
//                doorObj.transform.localScale = new Vector3(scaledWidth, scaledHeight, doorFrameThickness * scale);
//            }
//            else
//            {
//                // Default orientation
//                doorObj.transform.localScale = new Vector3(scaledWidth, scaledHeight, doorFrameThickness * scale);
//            }

//            // Apply material
//            if (doorMaterial != null)
//            {
//                doorObj.GetComponent<MeshRenderer>().material = doorMaterial;
//            }
//            else
//            {
//                // Use a different colored material from wall material
//                Material tempDoorMat = new Material(Shader.Find("Standard"));
//                tempDoorMat.color = new Color(0.7f, 0.4f, 0.2f); // Brown door color
//                doorObj.GetComponent<MeshRenderer>().material = tempDoorMat;
//            }
//        }
//    }

//    private void CreateWindow(Window window, Floor floor, Transform parent, float scale = 1.0f)
//    {
//        if (window.position == null)
//        {
//            Debug.LogWarning($"Window {window.id} has null position!");
//            return;
//        }

//        // Scale dimensions
//        float scaledWidth = window.width * scale;
//        float scaledHeight = window.height * scale;
//        float scaledSillHeight = window.sill_height * scale;

//        Vector3 position = new Vector3(
//            window.position.x * scale,
//            floor.elevation * scale + scaledSillHeight + scaledHeight / 2,
//            window.position.y * scale
//        );

//        if (windowPrefab != null)
//        {
//            // Use window prefab
//            GameObject windowObj = Instantiate(windowPrefab, position, Quaternion.identity, parent);
//            windowObj.name = $"Window_{window.id}";

//            // Determine rotation based on wall_id
//            if (!string.IsNullOrEmpty(window.wall_id))
//            {
//                float rotation = GetWallRotation(window.wall_id);
//                windowObj.transform.rotation = Quaternion.Euler(0, rotation, 0);
//            }

//            // Scale appropriately
//            windowObj.transform.localScale = new Vector3(scaledWidth, scaledHeight, 0.1f * scale);
//        }
//        else
//        {
//            // Create simple window representation
//            GameObject windowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            windowObj.name = $"Window_{window.id}";
//            windowObj.transform.SetParent(parent);
//            windowObj.transform.position = position;

//            // Determine rotation based on wall_id
//            if (!string.IsNullOrEmpty(window.wall_id))
//            {
//                float rotation = GetWallRotation(window.wall_id);
//                windowObj.transform.rotation = Quaternion.Euler(0, rotation, 0);
//            }

//            // Set scale
//            windowObj.transform.localScale = new Vector3(scaledWidth, scaledHeight, 0.05f * scale);

//            // Apply material
//            if (windowMaterial != null)
//            {
//                windowObj.GetComponent<MeshRenderer>().material = windowMaterial;
//            }
//            else
//            {
//                // Use a transparent blue material
//                Material tempWindowMat = new Material(Shader.Find("Standard"));
//                tempWindowMat.color = new Color(0.5f, 0.7f, 0.9f, 0.7f);
//                tempWindowMat.SetFloat("_Mode", 3); // Transparent mode
//                tempWindowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
//                tempWindowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
//                tempWindowMat.SetInt("_ZWrite", 0);
//                tempWindowMat.DisableKeyword("_ALPHATEST_ON");
//                tempWindowMat.EnableKeyword("_ALPHABLEND_ON");
//                tempWindowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
//                tempWindowMat.renderQueue = 3000;

//                windowObj.GetComponent<MeshRenderer>().material = tempWindowMat;
//            }
//        }
//    }

//    private float GetDoorRotation(string wallId, Door door, Floor floor)
//    {
//        // First try to get exact rotation based on wall geometry
//        if (wallDictionary.ContainsKey(wallId))
//        {
//            Wall wall = wallDictionary[wallId];
//            Vector3 wallDir = new Vector3(wall.end.x - wall.start.x, 0, wall.end.y - wall.start.y).normalized;
//            float exactAngle = Mathf.Atan2(wallDir.z, wallDir.x) * Mathf.Rad2Deg;
//            return exactAngle;
//        }

//        // Try to determine door rotation based on wall ID naming conventions
//        string wallIdLower = wallId.ToLower();

//        if (wallIdLower.Contains("north")) return 0f;
//        if (wallIdLower.Contains("east")) return 90f;
//        if (wallIdLower.Contains("south")) return 180f;
//        if (wallIdLower.Contains("west")) return 270f;

//        return 0f; // Default to north
//    }

//    private float GetWallRotation(string wallId)
//    {
//        // First try to get exact rotation based on wall geometry
//        if (wallDictionary.ContainsKey(wallId))
//        {
//            Wall wall = wallDictionary[wallId];
//            Vector3 wallDir = new Vector3(wall.end.x - wall.start.x, 0, wall.end.y - wall.start.y).normalized;
//            float exactAngle = Mathf.Atan2(wallDir.z, wallDir.x) * Mathf.Rad2Deg;
//            return exactAngle;
//        }

//        // Try to determine rotation based on wall ID naming conventions
//        string wallIdLower = wallId.ToLower();

//        if (wallIdLower.Contains("north")) return 0f;
//        if (wallIdLower.Contains("east")) return 90f;
//        if (wallIdLower.Contains("south")) return 180f;
//        if (wallIdLower.Contains("west")) return 270f;

//        return 0f; // Default to north
//    }
//    #endregion

//    #region Stair Generation
//    private struct StairOpening
//    {
//        public string stairId;
//        public Rect bounds;
//        public bool isTopOpening;
//    }

//    private Dictionary<string, List<StairOpening>> IdentifyStairOpenings(List<Stair> stairs, float scale)
//    {
//        Dictionary<string, List<StairOpening>> floorOpenings = new Dictionary<string, List<StairOpening>>();

//        if (stairs == null) return floorOpenings;

//        foreach (var stair in stairs)
//        {
//            if (!floorDictionary.ContainsKey(stair.bottom_floor) || !floorDictionary.ContainsKey(stair.top_floor))
//            {
//                Debug.LogWarning($"Stair {stair.id} references non-existent floors!");
//                continue;
//            }

//            // Calculate stair bounds
//            Rect stairBounds = CalculateStairBounds(stair, scale);

//            // Add bottom opening
//            if (!floorOpenings.ContainsKey(stair.top_floor))
//                floorOpenings[stair.top_floor] = new List<StairOpening>();

//            floorOpenings[stair.top_floor].Add(new StairOpening
//            {
//                stairId = stair.id,
//                bounds = stairBounds,
//                isTopOpening = false
//            });

//            // Add top opening (for ceiling of the top floor)
//            if (!floorOpenings.ContainsKey(stair.bottom_floor))
//                floorOpenings[stair.bottom_floor] = new List<StairOpening>();

//            floorOpenings[stair.bottom_floor].Add(new StairOpening
//            {
//                stairId = stair.id,
//                bounds = stairBounds,
//                isTopOpening = true
//            });
//        }

//        return floorOpenings;
//    }

//    private Rect CalculateStairBounds(Stair stair, float scale)
//    {
//        float halfWidth = (stair.width * scale) / 2 + openingMargin;
//        float halfLength = (stair.length * scale) / 2 + openingMargin;

//        float centerX, centerZ;

//        // Use center point between bottom and top
//        centerX = (stair.bottom_point.x + stair.top_point.x) / 2 * scale;
//        centerZ = (stair.bottom_point.y + stair.top_point.y) / 2 * scale;

//        // Adjust based on direction
//        switch (stair.direction)
//        {
//            case "north":
//            case "south":
//                return new Rect(centerX - halfWidth, centerZ - halfLength, stair.width * scale + openingMargin * 2, stair.length * scale + openingMargin * 2);
//            case "east":
//            case "west":
//                return new Rect(centerX - halfLength, centerZ - halfWidth, stair.length * scale + openingMargin * 2, stair.width * scale + openingMargin * 2);
//            default:
//                return new Rect(centerX - halfWidth, centerZ - halfWidth, stair.width * scale + openingMargin * 2, stair.width * scale + openingMargin * 2);
//        }
//    }

//    private void CreateStair(Stair stair, Dictionary<string, GameObject> floorObjects, float scale)
//    {
//        // Validate stair data
//        if (!floorDictionary.ContainsKey(stair.bottom_floor) || !floorDictionary.ContainsKey(stair.top_floor))
//        {
//            Debug.LogWarning($"Stair {stair.id} references non-existent floors!");
//            return;
//        }

//        var bottomFloor = floorDictionary[stair.bottom_floor];
//        var topFloor = floorDictionary[stair.top_floor];

//        // Get scaled stair dimensions
//        float scaledWidth = stair.width * scale;
//        float scaledLength = stair.length * scale;
//        Vector3 bottomPos = new Vector3(stair.bottom_point.x * scale, bottomFloor.elevation * scale, stair.bottom_point.y * scale);
//        Vector3 topPos = new Vector3(stair.top_point.x * scale, topFloor.elevation * scale, stair.top_point.y * scale);

//        // Create stair container
//        GameObject stairObj;
//        if (stairPrefab != null)
//        {
//            stairObj = Instantiate(stairPrefab, Vector3.zero, Quaternion.identity);
//            // Configure prefab as needed
//        }
//        else
//        {
//            stairObj = new GameObject($"Stair_{stair.id}");
//        }

//        stairObj.transform.SetParent(buildingContainer.transform);

//        // Calculate stair properties
//        float heightDifference = topPos.y - bottomPos.y;
//        float horizontalDistance = Vector3.Distance(new Vector3(bottomPos.x, 0, bottomPos.z), new Vector3(topPos.x, 0, topPos.z));
//        Vector3 stairDirection = new Vector3(topPos.x - bottomPos.x, 0, topPos.z - bottomPos.z).normalized;

//        float stairAngle = Mathf.Atan2(stairDirection.z, stairDirection.x) * Mathf.Rad2Deg;

//        // Default values if not specified
//        if (scaledLength < 0.1f) scaledLength = horizontalDistance;

//        // Create the actual stair object based on direction
//        Vector3 center = (bottomPos + topPos) / 2;
//        center.y = bottomPos.y + heightDifference / 2;

//        // Create stair mesh
//        GameObject stairMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
//        stairMesh.name = "StairMesh";
//        stairMesh.transform.SetParent(stairObj.transform);
//        stairMesh.transform.position = center;
//        stairMesh.transform.rotation = Quaternion.Euler(0, stairAngle, 0);

//        // Calculate appropriate scaling for the stair mesh
//        float stairSlope = Mathf.Atan2(heightDifference, horizontalDistance);
//        stairMesh.transform.rotation *= Quaternion.Euler(-stairSlope * Mathf.Rad2Deg, 0, 0);
//        stairMesh.transform.localScale = new Vector3(scaledWidth, 0.2f * scale, Vector3.Distance(bottomPos, topPos));

//        // Apply material
//        if (stairMaterial != null)
//        {
//            stairMesh.GetComponent<MeshRenderer>().material = stairMaterial;
//        }
//        else
//        {
//            // Create a default stair material
//            Material tempMaterial = new Material(Shader.Find("Standard"));
//            tempMaterial.color = new Color(0.6f, 0.6f, 0.6f); // Gray color
//            stairMesh.GetComponent<MeshRenderer>().material = tempMaterial;
//        }

//        // Add handrails (optional)
//        CreateStairHandrails(stairObj.transform, bottomPos, topPos, scaledWidth, scale);
//    }

//    private void CreateStairHandrails(Transform parent, Vector3 bottomPos, Vector3 topPos, float stairWidth, float scale)
//    {
//        // Calculate handrail positions
//        Vector3 direction = new Vector3(topPos.x - bottomPos.x, 0, topPos.z - bottomPos.z).normalized;
//        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

//        Vector3 leftStart = bottomPos + right * (stairWidth / 2);
//        Vector3 leftEnd = topPos + right * (stairWidth / 2);
//        Vector3 rightStart = bottomPos - right * (stairWidth / 2);
//        Vector3 rightEnd = topPos - right * (stairWidth / 2);

//        // Create left handrail
//        CreateHandrail(parent, leftStart, leftEnd, scale);

//        // Create right handrail
//        CreateHandrail(parent, rightStart, rightEnd, scale);
//    }

//    private void CreateHandrail(Transform parent, Vector3 start, Vector3 end, float scale)
//    {
//        Vector3 direction = end - start;
//        float length = direction.magnitude;
//        Vector3 center = (start + end) / 2;
//        center.y += 0.5f * scale; // Raise handrail to appropriate height

//        // Create handrail post at both ends
//        CreateHandrailPost(parent, start, scale);
//        CreateHandrailPost(parent, end, scale);

//        // Create horizontal rail
//        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//        rail.name = "Handrail";
//        rail.transform.SetParent(parent);
//        rail.transform.position = center;

//        // Rotate cylinder to align with direction
//        rail.transform.up = direction.normalized;
//        rail.transform.localScale = new Vector3(0.05f * scale, length / 2, 0.05f * scale);

//        // Apply material
//        if (stairMaterial != null)
//        {
//            rail.GetComponent<MeshRenderer>().material = stairMaterial;
//        }
//    }

//    private void CreateHandrailPost(Transform parent, Vector3 position, float scale)
//    {
//        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//        post.name = "HandrailPost";
//        post.transform.SetParent(parent);

//        // Position post
//        Vector3 postPosition = position;
//        postPosition.y += 0.5f * scale; // Half height of post
//        post.transform.position = postPosition;

//        // Scale post
//        post.transform.localScale = new Vector3(0.05f * scale, 1f * scale, 0.05f * scale);

//        // Apply material
//        if (stairMaterial != null)
//        {
//            post.GetComponent<MeshRenderer>().material = stairMaterial;
//        }
//    }

//    private void CreateFloorSlab(List<Point> boundaries, float elevation, float thickness, Material material, Transform parent, string name, List<StairOpening> openings = null)
//    {
//        if (boundaries == null || boundaries.Count < 3)
//        {
//            Debug.LogWarning($"Cannot create {name} with less than 3 boundary points!");
//            return;
//        }

//        GameObject slabContainer = new GameObject(name);
//        slabContainer.transform.SetParent(parent);

//        // If no openings, create a simple mesh
//        if (openings == null || openings.Count == 0 || !createStairOpenings)
//        {
//            // Create a mesh directly from boundaries
//            CreateSimpleFloorMesh(boundaries, elevation, thickness, material, slabContainer.transform);
//            return;
//        }

//        // Attempt to create a complex mesh with cutouts for stair openings
//        bool success = CreateComplexFloorMeshWithOpenings(boundaries, elevation, thickness, material, slabContainer.transform, openings);

//        if (!success)
//        {
//            // Fall back to creating individual objects (less efficient but more reliable)
//            CreateFloorWithCutoutsAsObjects(boundaries, elevation, thickness, material, slabContainer.transform, openings);
//        }
//    }

//    private bool CreateComplexFloorMeshWithOpenings(List<Point> boundaries, float elevation, float thickness, Material material, Transform parent, List<StairOpening> openings)
//    {
//        try
//        {
//            // Implementation for creating complex mesh with cutouts would go here
//            // This is a placeholder - for a complete implementation, you would need a 
//            // mesh generation library that handles CSG (Constructive Solid Geometry) operations

//            Debug.LogWarning("Complex floor mesh generation with cutouts is not implemented yet.");
//            return false; // Fall back to the object-based approach
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"Error creating complex floor mesh: {e.Message}");
//            return false;
//        }
//    }

//    private void CreateFloorWithCutoutsAsObjects(List<Point> boundaries, float elevation, float thickness, Material material, Transform parent, List<StairOpening> openings)
//    {
//        // Convert boundaries to 2D space for easier processing
//        List<Vector2> boundary2D = new List<Vector2>();
//        foreach (var p in boundaries)
//        {
//            boundary2D.Add(new Vector2(p.x, p.y));
//        }

//        // Create the main floor slab first
//        GameObject mainSlab = CreateSimpleFloorMesh(boundaries, elevation, thickness, material, parent);

//        // Create cutout objects for each opening (these will use negative physics materials)
//        foreach (var opening in openings)
//        {
//            // Create cutout primitive
//            GameObject cutout = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            cutout.name = $"Cutout_{opening.stairId}";
//            cutout.transform.SetParent(parent);

//            // Position cutout
//            Vector3 cutoutPos = new Vector3(
//                opening.bounds.x + opening.bounds.width / 2,
//                elevation + thickness / 2,
//                opening.bounds.y + opening.bounds.height / 2
//            );
//            cutout.transform.position = cutoutPos;

//            // Scale cutout
//            cutout.transform.localScale = new Vector3(
//                opening.bounds.width,
//                thickness * 1.1f, // Slightly thicker to ensure complete penetration
//                opening.bounds.height
//            );

//            // Set cutout material to render invisible
//            Material cutoutMaterial = new Material(Shader.Find("Standard"));
//            cutoutMaterial.color = new Color(1, 1, 1, 0);
//            cutoutMaterial.SetFloat("_Mode", 3); // Transparent mode
//            cutoutMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
//            cutoutMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
//            cutoutMaterial.SetInt("_ZWrite", 0);
//            cutoutMaterial.DisableKeyword("_ALPHATEST_ON");
//            cutoutMaterial.EnableKeyword("_ALPHABLEND_ON");
//            cutoutMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
//            cutoutMaterial.renderQueue = 3000;

//            cutout.GetComponent<MeshRenderer>().material = cutoutMaterial;

//            // Configure for Boolean subtraction in physics
//            // Note: For actual Boolean mesh operations, you would need a CSG library
//            cutout.AddComponent<BoxCollider>().isTrigger = true;
//        }
//    }

//    private GameObject CreateSimpleFloorMesh(List<Point> boundaries, float elevation, float thickness, Material material, Transform parent)
//    {
//        // Convert boundaries to Vector3 points
//        Vector3[] verts = new Vector3[boundaries.Count];
//        for (int i = 0; i < boundaries.Count; i++)
//        {
//            verts[i] = new Vector3(boundaries[i].x, elevation, boundaries[i].y);
//        }

//        // Create top face
//        GameObject floorObj = new GameObject("FloorMesh");
//        floorObj.transform.SetParent(parent);

//        // Create a simple extruded mesh from the floor plan
//        MeshFilter mf = floorObj.AddComponent<MeshFilter>();
//        MeshRenderer mr = floorObj.AddComponent<MeshRenderer>();

//        // Create mesh through triangulation
//        Mesh mesh = CreateExtrudedMesh(verts, thickness);
//        mf.mesh = mesh;

//        // Apply material
//        if (material != null)
//        {
//            mr.material = material;
//        }
//        else
//        {
//            // Create default material
//            Material defaultMat = new Material(Shader.Find("Standard"));
//            defaultMat.color = new Color(0.7f, 0.7f, 0.7f);
//            mr.material = defaultMat;
//        }

//        // Add collider
//        floorObj.AddComponent<MeshCollider>();

//        return floorObj;
//    }

//    private Mesh CreateExtrudedMesh(Vector3[] topVerts, float height)
//    {
//        int vertCount = topVerts.Length;

//        // Create bottom verts
//        Vector3[] bottomVerts = new Vector3[vertCount];
//        for (int i = 0; i < vertCount; i++)
//        {
//            bottomVerts[i] = topVerts[i] - Vector3.up * height;
//        }

//        // Combine verts
//        Vector3[] allVerts = new Vector3[vertCount * 2];
//        System.Array.Copy(topVerts, 0, allVerts, 0, vertCount);
//        System.Array.Copy(bottomVerts, 0, allVerts, vertCount, vertCount);

//        // Create triangles
//        List<int> triangles = new List<int>();

//        // Top face (note: CCW winding for normal facing up)
//        for (int i = 2; i < vertCount; i++)
//        {
//            triangles.Add(0);
//            triangles.Add(i - 1);
//            triangles.Add(i);
//        }

//        // Bottom face (note: CW winding for normal facing down)
//        for (int i = 2; i < vertCount; i++)
//        {
//            triangles.Add(vertCount);
//            triangles.Add(vertCount + i);
//            triangles.Add(vertCount + i - 1);
//        }

//        // Side faces
//        for (int i = 0; i < vertCount; i++)
//        {
//            int next = (i + 1) % vertCount;

//            // First triangle
//            triangles.Add(i);
//            triangles.Add(next);
//            triangles.Add(i + vertCount);

//            // Second triangle
//            triangles.Add(next);
//            triangles.Add(next + vertCount);
//            triangles.Add(i + vertCount);
//        }

//        // Create mesh
//        Mesh mesh = new Mesh();
//        mesh.vertices = allVerts;
//        mesh.triangles = triangles.ToArray();
//        mesh.RecalculateNormals();
//        mesh.RecalculateBounds();

//        return mesh;
//    }
//    #endregion

//    // Add additional utility methods as needed
//    #region Editor Methods
//#if UNITY_EDITOR
//    // Editor-specific methods for testing/debugging
//    public void LoadJsonFromResources(string resourcePath)
//    {
//        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
//        if (textAsset != null)
//        {
//            buildingJson = textAsset.text;
//            GenerateBuildingFromJson();
//        }
//        else
//        {
//            Debug.LogError($"Resource not found: {resourcePath}");
//        }
//    }

//    // Method for editor inspector button
//    public void GenerateFromCurrentJson()
//    {
//        GenerateBuildingFromJson();
//    }
//#endif
//    #endregion
//}