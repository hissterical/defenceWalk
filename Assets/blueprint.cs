using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class BlueprintProcessor : MonoBehaviour
{
    // Configuration
    [Header("Blueprint Configuration")]
    [Tooltip("Path to the JSON file (relative to Assets folder or absolute)")]
    public string jsonFilePath = "blueprint.json";

    [Tooltip("Standard wall height in Unity units")]
    public float wallHeight = 2.5f;

    [Tooltip("Units per pixel conversion factor")]
    public float unitsPerPixel = 0.01f;

    [Tooltip("Material for walls")]
    public Material wallMaterial;

    [Tooltip("Material for floors")]
    public Material floorMaterial;

    [Header("Visualization")]
    [Tooltip("Show wall IDs in scene")]
    public bool showWallIds = true;

    [Tooltip("Create floors based on room polygons")]
    public bool createFloors = true;

    // Internal data storage
    private BlueprintData blueprintData;
    private GameObject wallsContainer;
    private GameObject floorsContainer;

    // Unity lifecycle methods
    private void Start()
    {
        LoadBlueprintData();
        GenerateWalls();
        if (createFloors)
        {
            GenerateFloors();
        }
    }

    // Loads blueprint data from the JSON file
    public void LoadBlueprintData()
    {
        string fullPath = jsonFilePath;
        if (!Path.IsPathRooted(jsonFilePath))
        {
            fullPath = Path.Combine(Application.dataPath, jsonFilePath);
        }

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Blueprint JSON file not found at {fullPath}");
            return;
        }

        string jsonContent = File.ReadAllText(fullPath);
        try
        {
            BlueprintWrapper wrapper = JsonConvert.DeserializeObject<BlueprintWrapper>(jsonContent);
            if (wrapper != null && wrapper.data != null)
            {
                blueprintData = wrapper.data;
                Debug.Log($"Successfully loaded blueprint data with {blueprintData.walls?.Count ?? 0} walls " +
                          $"and {blueprintData.rooms?.Count ?? 0} rooms");
            }
            else
            {
                Debug.LogError("Failed to parse blueprint data: Null result");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse blueprint JSON: {e.Message}");
        }
    }

    // Process the wall data from the blueprint and generate 3D walls
    public void GenerateWalls()
    {
        if (blueprintData == null)
        {
            Debug.LogError("No blueprint data loaded");
            return;
        }

        // Create container for walls if needed
        if (wallsContainer == null)
        {
            wallsContainer = new GameObject("Walls");
            wallsContainer.transform.SetParent(transform);
        }

        // Process walls based on wall_segments if available (more precise)
        if (blueprintData.wall_segments != null && blueprintData.wall_segments.Count > 0)
        {
            ProcessWallSegments();
        }
        // Otherwise use the polygon-based walls
        else if (blueprintData.walls != null && blueprintData.walls.Count > 0)
        {
            ProcessWallPolygons();
        }
        else
        {
            Debug.LogWarning("No wall data found in blueprint");
        }
    }

    // Process wall segments (line segments) from the blueprint
    private void ProcessWallSegments()
    {
        foreach (var segment in blueprintData.wall_segments)
        {
            // Convert pixel coordinates to Unity world position
            Vector3 startPos = PixelToUnityPosition(segment.start[0], segment.start[1]);
            Vector3 endPos = PixelToUnityPosition(segment.end[0], segment.end[1]);

            // Create the wall geometry
            CreateWallFromLine(startPos, endPos, segment.id);
        }
    }

    // Process wall polygons from the blueprint
    private void ProcessWallPolygons()
    {
        foreach (var wall in blueprintData.walls)
        {
            if (wall.polygon == null || wall.polygon.Count < 2)
            {
                Debug.LogWarning($"Skipping invalid wall {wall.id}: Insufficient polygon points");
                continue;
            }

            // Determine if this is a simple wall (like a line) or a complex polygon
            bool isSimpleWall = IsSimpleWall(wall);

            if (isSimpleWall)
            {
                // For simple walls, use start and end points to create a wall
                Vector3 start = PixelToUnityPosition(wall.polygon[0][0], wall.polygon[0][1]);
                Vector3 end = PixelToUnityPosition(wall.polygon[wall.polygon.Count - 1][0], wall.polygon[wall.polygon.Count - 1][1]);
                CreateWallFromLine(start, end, wall.id);
            }
            else
            {
                // For complex walls, create a full 3D polygon
                CreateWallFromPolygon(wall);
            }
        }
    }

    // Determines if a wall is a simple line or a complex polygon
    private bool IsSimpleWall(Wall wall)
    {
        // A wall is simple if it's very thin in one dimension
        if (wall.bounds != null)
        {
            float aspectRatio = (float)wall.bounds.width / wall.bounds.height;
            return aspectRatio > 5.0f || aspectRatio < 0.2f; // Very wide or very tall
        }

        // If no bounds available, check if it's basically a line
        if (wall.polygon.Count <= 4)
        {
            return true; // Small number of points likely means it's simple
        }

        return false;
    }

    // Creates a wall mesh between two points
    private void CreateWallFromLine(Vector3 start, Vector3 end, string wallId = null)
    {
        // Calculate wall properties
        Vector3 wallDirection = end - start;
        float wallLength = wallDirection.magnitude;

        // Ignore very small walls (likely errors)
        if (wallLength < 0.05f)
        {
            return;
        }

        // Create wall game object
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = string.IsNullOrEmpty(wallId) ? "Wall" : wallId;
        wall.transform.SetParent(wallsContainer.transform);

        // Position wall between the start and end points
        wall.transform.position = (start + end) / 2;

        // Rotate wall to align with direction
        wall.transform.LookAt(wall.transform.position + Vector3.Cross(wallDirection, Vector3.up));
        wall.transform.Rotate(Vector3.up, 90); // Adjust rotation to align with wall direction

        // Scale the wall
        float wallThickness = 0.2f; // Thickness of wall in Unity units
        wall.transform.localScale = new Vector3(wallThickness, wallHeight, wallLength);

        // Apply material
        if (wallMaterial != null)
        {
            wall.GetComponent<Renderer>().material = wallMaterial;
        }

        // Add wall ID text (optional)
        if (showWallIds && !string.IsNullOrEmpty(wallId))
        {
            CreateWallLabel(wall, wallId);
        }
    }

    // Creates a complex wall from a polygon shape
    private void CreateWallFromPolygon(Wall wall)
    {
        // Create wall container
        GameObject wallObj = new GameObject(wall.id ?? "ComplexWall");
        wallObj.transform.SetParent(wallsContainer.transform);

        // Set up mesh components
        MeshFilter meshFilter = wallObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = wallObj.AddComponent<MeshRenderer>();

        // Create the mesh from polygon points
        meshFilter.mesh = CreateWallMesh(wall.polygon);

        // Apply material
        if (wallMaterial != null)
        {
            meshRenderer.material = wallMaterial;
        }

        // Add collider
        wallObj.AddComponent<MeshCollider>();

        // Add wall ID (optional)
        if (showWallIds && !string.IsNullOrEmpty(wall.id))
        {
            CreateWallLabel(wallObj, wall.id);
        }
    }

    // Creates a 3D mesh from a 2D polygon (extruding up)
    private Mesh CreateWallMesh(List<int[]> polygonPoints)
    {
        Mesh mesh = new Mesh();

        // Create vertices
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Create bottom and top vertices for each point
        for (int i = 0; i < polygonPoints.Count; i++)
        {
            Vector3 point = PixelToUnityPosition(polygonPoints[i][0], polygonPoints[i][1]);
            vertices.Add(point); // Bottom vertex
            vertices.Add(point + Vector3.up * wallHeight); // Top vertex
        }

        // Create side quads for the wall
        for (int i = 0; i < polygonPoints.Count; i++)
        {
            int nextIdx = (i + 1) % polygonPoints.Count;

            int baseIdx = i * 2;
            int nextBaseIdx = nextIdx * 2;

            // First triangle of the quad (bottom-right, top-right, top-left)
            triangles.Add(baseIdx);
            triangles.Add(nextBaseIdx + 1);
            triangles.Add(baseIdx + 1);

            // Second triangle of the quad (bottom-right, bottom-left, top-right)
            triangles.Add(baseIdx);
            triangles.Add(nextBaseIdx);
            triangles.Add(nextBaseIdx + 1);
        }

        // Assign to mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // Calculate normals
        mesh.RecalculateNormals();

        return mesh;
    }

    // Creates a label for the wall ID
    private void CreateWallLabel(GameObject wall, string wallId)
    {
        GameObject textObj = new GameObject($"{wallId}_Label");
        textObj.transform.SetParent(wall.transform);
        textObj.transform.localPosition = new Vector3(0, wallHeight / 2, 0);

        // Look at camera (if we're in editor, look at scene view camera)
        textObj.transform.LookAt(textObj.transform.position + Camera.main.transform.forward);

        // Create TextMesh component
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = wallId;
        textMesh.fontSize = 12;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.black;

        // Scale text appropriately
        textObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }

    // Generate floors based on room polygons
    public void GenerateFloors()
    {
        if (blueprintData == null || blueprintData.rooms == null || blueprintData.rooms.Count == 0)
        {
            Debug.LogWarning("No room data available for floor generation");
            return;
        }

        // Create container for floors
        if (floorsContainer == null)
        {
            floorsContainer = new GameObject("Floors");
            floorsContainer.transform.SetParent(transform);
        }

        // Process each room to create floors
        foreach (var room in blueprintData.rooms)
        {
            if (room.polygon == null || room.polygon.Count < 3)
            {
                Debug.LogWarning($"Skipping room {room.id}: Invalid polygon");
                continue;
            }

            CreateFloor(room);
        }
    }

    // Creates a floor mesh from room data
    private void CreateFloor(Room room)
    {
        GameObject floorObj = new GameObject(room.id ?? "Floor");
        floorObj.transform.SetParent(floorsContainer.transform);

        // Position slightly below walls to avoid z-fighting
        floorObj.transform.position = new Vector3(0, -0.01f, 0);

        // Set up mesh components
        MeshFilter meshFilter = floorObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = floorObj.AddComponent<MeshRenderer>();

        // Create floor mesh from room polygon
        meshFilter.mesh = CreateFloorMesh(room.polygon);

        // Apply material
        if (floorMaterial != null)
        {
            meshRenderer.material = floorMaterial;
        }

        // Add collider
        floorObj.AddComponent<MeshCollider>();
    }

    // Create a floor mesh from a 2D polygon
    private Mesh CreateFloorMesh(List<int[]> polygonPoints)
    {
        Mesh mesh = new Mesh();

        // Convert polygon points to Vector3 (all on the same Y level)
        Vector3[] vertices = new Vector3[polygonPoints.Count];
        for (int i = 0; i < polygonPoints.Count; i++)
        {
            vertices[i] = PixelToUnityPosition(polygonPoints[i][0], polygonPoints[i][1]);
        }

        // Triangulate the polygon
        int[] triangles = Triangulate(vertices);

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Calculate normals
        mesh.RecalculateNormals();

        return mesh;
    }

    // Simple triangulation for convex polygons
    private int[] Triangulate(Vector3[] vertices)
    {
        // For simple convex polygons, we can create a fan triangulation
        List<int> triangles = new List<int>();

        for (int i = 1; i < vertices.Length - 1; i++)
        {
            triangles.Add(0);         // First vertex as the pivot
            triangles.Add(i);         // Second vertex
            triangles.Add(i + 1);     // Third vertex
        }

        return triangles.ToArray();
    }

    // Converts image pixel coordinates to Unity world coordinates
    private Vector3 PixelToUnityPosition(int x, int y)
    {
        // Get image dimensions
        int imageWidth = blueprintData.image_dimensions?.width ?? 1000;
        int imageHeight = blueprintData.image_dimensions?.height ?? 1000;

        // Center the blueprint around the origin and scale
        float unitX = (x - imageWidth / 2f) * unitsPerPixel;

        // Flip Y coordinate (image Y is down, Unity Y is up)
        // We're using Z as the second floor dimension since we're building on the XZ plane
        float unitZ = (imageHeight / 2f - y) * unitsPerPixel;

        return new Vector3(unitX, 0, unitZ);
    }
}

// JSON data structures for deserializing the blueprint data
[System.Serializable]
public class BlueprintWrapper
{
    public BlueprintData data;
}

[System.Serializable]
public class BlueprintData
{
    public List<Wall> walls;
    public List<WallSegment> wall_segments;
    public List<Room> rooms;
    public ImageDimensions image_dimensions;
}

[System.Serializable]
public class Wall
{
    public string id;
    public string type;  // "horizontal" or "vertical"
    public List<int[]> polygon;
    public float area;
    public WallBounds bounds;
}

[System.Serializable]
public class WallBounds
{
    public int x;
    public int y;
    public int width;
    public int height;
}

[System.Serializable]
public class WallSegment
{
    public string id;
    public int[] start;
    public int[] end;
    public float length;
}

[System.Serializable]
public class Room
{
    public string id;
    public List<int[]> polygon;
    public float area;
}

[System.Serializable]
public class ImageDimensions
{
    public int width;
    public int height;
}