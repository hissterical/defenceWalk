//using UnityEngine;
//using UnityEditor;

//[RequireComponent(typeof(BuildingGenerator))]
//public class BuildingJsonLoader : MonoBehaviour
//{
//    [Header("JSON Source")]
//    public TextAsset buildingJsonAsset;

//    [Header("Settings")]
//    [Tooltip("Enable to create openings in floors for stairs")]
//    public bool createStairOpenings = true;

//    [Tooltip("Extra margin around stair openings")]
//    public float openingMargin = 0.5f;

//    private BuildingGenerator buildingGenerator;

//    void Awake()
//    {
//        buildingGenerator = GetComponent<BuildingGenerator>();
//    }

//    void Start()
//    {
//        ReloadBuilding();
//    }

//    public void ReloadBuilding()
//    {
//        if (buildingJsonAsset == null || buildingGenerator == null)
//        {
//            Debug.LogWarning("Missing building JSON asset or BuildingGenerator component!");
//            return;
//        }

//        // Update generator settings
//        buildingGenerator.createStairOpenings = createStairOpenings;
//        buildingGenerator.openingMargin = openingMargin;

//        // Apply JSON
//        buildingGenerator.buildingJson = buildingJsonAsset.text;

//        // Generate the building
//        buildingGenerator.GenerateBuildingFromJson();
//    }
//}

//#if UNITY_EDITOR
//[CustomEditor(typeof(BuildingJsonLoader))]
//public class BuildingJsonLoaderEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();

//        BuildingJsonLoader loader = (BuildingJsonLoader)target;

//        GUILayout.Space(10);

//        if (GUILayout.Button("Load and Generate Building"))
//        {
//            if (!Application.isPlaying)
//            {
//                EditorUtility.DisplayDialog(
//                    "Play Mode Required",
//                    "Enter Play Mode before generating.",
//                    "OK"
//                );
//                return;
//            }

//            if (loader.buildingJsonAsset == null)
//            {
//                EditorUtility.DisplayDialog(
//                    "Missing JSON",
//                    "Please assign a TextAsset with your building JSON.",
//                    "OK"
//                );
//                return;
//            }

//            loader.ReloadBuilding();
//        }

//        GUILayout.Space(5);
//        EditorGUILayout.HelpBox(
//            "This component requires a BuildingGenerator component to function. " +
//            "Make sure materials are assigned in the BuildingGenerator component.",
//            MessageType.Info
//        );
//    }
//}
//#endif