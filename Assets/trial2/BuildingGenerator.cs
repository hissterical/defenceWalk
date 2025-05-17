using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject rampPrefab; // Add a ramp prefab in Unity

    [Header("Settings")]
    public float wallHeight = 2.5f;
    public float floorHeight = 3.5f;
    public float floorThickness = 0.1f;

    [TextArea(5, 15)]
    public string jsonData;

    private void Start()
    {
        BuildingData buildingData = JsonUtility.FromJson<BuildingData>(jsonData);

        for (int i = 0; i < buildingData.floors.Count; i++)
        {
            FloorData floor = buildingData.floors[i];
            float yPos = i * floorHeight;

            GenerateFloorWithHoles(floor, yPos);
            GenerateWalls(floor, yPos);

            // Add ramp connecting to next floor (not on last floor)
            if (i < buildingData.floors.Count - 1 && floor.stairs != null)
            {
                foreach (var stair in floor.stairs)
                {
                    AddRamp(stair, yPos);
                }
            }
        }
    }

    void GenerateFloorWithHoles(FloorData floor, float yPos)
    {
        List<Rect> holes = new();
        foreach (var stair in floor.stairs ?? new())
        {
            holes.Add(new Rect(stair.x, stair.y, stair.width, stair.length));
        }

        if (holes.Count == 0)
        {
            GameObject baseFloor = Instantiate(floorPrefab, new Vector3(floor.width / 2, yPos, floor.length / 2), Quaternion.identity, transform);
            baseFloor.transform.localScale = new Vector3(floor.width, floorThickness, floor.length);
            baseFloor.name = $"Floor_{yPos}";
            return;
        }

        Rect hole = holes[0]; // Only handling one for now

        // Bottom
        if (hole.y > 0)
        {
            float height = hole.y;
            Vector3 pos = new Vector3(floor.width / 2, yPos, height / 2);
            GameObject part = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
            part.transform.localScale = new Vector3(floor.width, floorThickness, height);
        }

        // Top
        if (hole.y + hole.height < floor.length)
        {
            float height = floor.length - (hole.y + hole.height);
            float zPos = hole.y + hole.height + height / 2;
            Vector3 pos = new Vector3(floor.width / 2, yPos, zPos);
            GameObject part = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
            part.transform.localScale = new Vector3(floor.width, floorThickness, height);
        }

        // Left
        if (hole.x > 0)
        {
            float width = hole.x;
            float xPos = width / 2;
            float zPos = hole.y + hole.height / 2;
            GameObject part = Instantiate(floorPrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, transform);
            part.transform.localScale = new Vector3(width, floorThickness, hole.height);
        }

        // Right
        if (hole.x + hole.width < floor.width)
        {
            float width = floor.width - (hole.x + hole.width);
            float xPos = hole.x + hole.width + width / 2;
            float zPos = hole.y + hole.height / 2;
            GameObject part = Instantiate(floorPrefab, new Vector3(xPos, yPos, zPos), Quaternion.identity, transform);
            part.transform.localScale = new Vector3(width, floorThickness, hole.height);
        }
    }

    void GenerateWalls(FloorData floor, float yPos)
    {
        GenerateWallSide(floor, yPos, Side.South);
        GenerateWallSide(floor, yPos, Side.North);
        GenerateWallSide(floor, yPos, Side.West);
        GenerateWallSide(floor, yPos, Side.East);
    }

    enum Side { South, North, West, East }

    void GenerateWallSide(FloorData floor, float yPos, Side side)
    {
        float thickness = 0.1f;
        float yCenter = yPos + floorThickness + (wallHeight / 2);
        bool horizontal = (side == Side.South || side == Side.North);
        float totalLength = horizontal ? floor.width : floor.length;

        List<(float start, float end)> segments = new();
        float cursor = 0f;

        foreach (var door in floor.doors ?? new())
        {
            bool match = (side == Side.South && Mathf.Approximately(door.y, 0f))
                      || (side == Side.North && Mathf.Approximately(door.y, floor.length))
                      || (side == Side.West && Mathf.Approximately(door.x, 0f))
                      || (side == Side.East && Mathf.Approximately(door.x, floor.width));
            if (!match) continue;

            float doorStart = horizontal ? door.x : door.y;
            float doorLen = horizontal ? door.width : door.length;

            segments.Add((cursor, doorStart));
            cursor = doorStart + doorLen;
        }

        if (cursor < totalLength)
        {
            segments.Add((cursor, totalLength));
        }

        foreach (var (segStart, segEnd) in segments)
        {
            float segLen = segEnd - segStart;
            if (segLen < 0.01f) continue;

            Vector3 pos, scale;
            if (horizontal)
            {
                float zPos = (side == Side.South) ? -thickness / 2 : floor.length + thickness / 2;
                float xCenter = segStart + segLen / 2;
                pos = new Vector3(xCenter, yCenter, zPos);
                scale = new Vector3(segLen + 0.2f, wallHeight, thickness);
            }
            else
            {
                float xPos = (side == Side.West) ? -thickness / 2 : floor.width + thickness / 2;
                float zCenter = segStart + segLen / 2;
                pos = new Vector3(xPos, yCenter, zCenter);
                scale = new Vector3(thickness, wallHeight, segLen + 0.2f);
            }

            GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
            wall.transform.localScale = scale;
        }
    }

    void AddRamp(StairData stair, float yPos)
    {
        Vector3 start = new Vector3(stair.x + stair.width / 2, yPos, stair.y + stair.length / 2);
        Vector3 end = new Vector3(start.x, yPos + floorHeight, start.z);

        Vector3 mid = (start + end) / 2;
        float height = floorHeight;
        float length = Mathf.Sqrt(height * height + stair.length * stair.length);

        GameObject ramp = Instantiate(rampPrefab, mid, Quaternion.identity, transform);
        ramp.transform.localScale = new Vector3(stair.width, 0.2f, length);
        ramp.transform.rotation = Quaternion.Euler(Mathf.Atan2(height, stair.length) * Mathf.Rad2Deg, 0, 0);
    }
}

#region Data Classes
[System.Serializable]
public class BuildingData
{
    public List<FloorData> floors;
}

[System.Serializable]
public class FloorData
{
    public float width;
    public float length;
    public List<DoorData> doors;
    public List<StairData> stairs;
}

[System.Serializable]
public class DoorData
{
    public float x, y, width, length;
}

[System.Serializable]
public class StairData
{
    public float x, y, width, length;
}
#endregion
