using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class PrefabPlacer : EditorWindow
{
    private const float MIN_RADIUS = 0.1f;
    private const float OFFSET = 2;
    private const float RADIUS_SCROLL_INCREMENT = 0.1f;
    private const int DISC_RESOLUTION = 50;
    private const float DISC_WIDTH = 1f;
    private const float TEST_RAY_DISTANCE_LIMIT = 40;

    public float circleRadius = 10;
    public int count = 10;
    public float spawnRadius = 2;
    public Material previewMaterial;
    public Transform parent;


    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propCount;
    SerializedProperty propPreviewMaterial;
    SerializedProperty propParent;
    SerializedProperty propSpawnRadius;



    class CirclePoint
    {
        public Vector2 point;
        public bool used;

        public CirclePoint(Vector2 point, bool used)
        {
            this.point = point;
            this.used = used;
        }
    }


    List<CirclePoint> pointsInCircle;
    List<CirclePoint> randomPointsInCircle;

    GameObjectSpawnInfo[] gameObjectsSpawnInfo;

    Dictionary<GameObject, MeshFilter[]> meshesByPrefab;
    Dictionary<GameObject, bool> isPrefabSelected;
    bool gameObjectsSelected;

    [MenuItem("Tools/Prefab Placer")]
    private static void ShowWindow()
    {

        var window = GetWindow<PrefabPlacer>();
        window.titleContent = new GUIContent("Prefab Placer");
        window.Show();
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
        meshesByPrefab = new Dictionary<GameObject, MeshFilter[]>();
        isPrefabSelected = new Dictionary<GameObject, bool>();

        propRadius = so.FindProperty("circleRadius");
        propCount = so.FindProperty("count");
        propPreviewMaterial = so.FindProperty("previewMaterial");
        propParent = so.FindProperty("parent");
        propSpawnRadius = so.FindProperty("spawnRadius");


        SceneView.duringSceneGui += DuringSceneGUI;

        GenerateRandomPointsInCircle();
        SelectRandomPointsInCircle();
        LoadPrefabs();
    }


    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }


    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        EditorGUILayout.PropertyField(propSpawnRadius);

        propRadius.floatValue = Mathf.Max(propRadius.floatValue, MIN_RADIUS);
        propSpawnRadius.floatValue = Mathf.Min(propSpawnRadius.floatValue, propRadius.floatValue);
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPointsInCircle();
            SelectRandomPointsInCircle();
        }

        so.Update();
        EditorGUILayout.PropertyField(propCount);
        propCount.intValue = Mathf.Max(propCount.intValue, 0);
        if (so.ApplyModifiedProperties())
        {
            SelectRandomPointsInCircle();
        }


        so.Update();
        EditorGUILayout.PropertyField(propPreviewMaterial);
        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        };

        so.Update();
        EditorGUILayout.PropertyField(propParent);
        so.ApplyModifiedProperties();


        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            DeselectUI();
        }

        if (!gameObjectsSelected)
        {
            EditorGUILayout.HelpBox("No GameObjects assigned", MessageType.Warning, true);
        }
    }



    private void DuringSceneGUI(SceneView sceneView)
    {

        Handles.BeginGUI();
        Rect rect = new Rect(8, 8, 50, 50);
        foreach (GameObject gameObject in meshesByPrefab.Keys)
        {
            Texture icon = AssetPreview.GetAssetPreview(gameObject);
            isPrefabSelected[gameObject] = GUI.Toggle(rect, isPrefabSelected[gameObject], icon);
            if (GUI.changed)
            {
                gameObjectsSelected = AreGameObjectsSelected();
                SelectRandomPointsInCircle();
                Repaint();
            }

            rect.x += rect.width + 5;
        }

        Handles.EndGUI();

        if (gameObjectsSelected)
        {
            DisplaySceneGUI(sceneView);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                InstantiateGameObjects();
                SelectRandomPointsInCircle();
                Event.current.Use();
            }

            PreviewGameObjects(sceneView.camera);
        }

        //Increase radius with input
        TryIncreaseRadius();
    }

    private void DisplaySceneGUI(SceneView sceneView)
    {
        Handles.zTest = CompareFunction.LessEqual;
        Transform camTf = sceneView.camera.transform;
        Ray rayToMouse = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(rayToMouse, out RaycastHit mouseRayHitInfo))
        {
            Vector3 normal = mouseRayHitInfo.normal;
            Vector3 tangent = Vector3.Cross(normal, camTf.up).normalized;
            Vector3 biTangent = Vector3.Cross(normal, tangent);

            DrawDiscOnSurface(mouseRayHitInfo.point, circleRadius, normal, tangent);


            for (int i = 0; i < randomPointsInCircle.Count; i++)
            {
                Vector3 pointInRadius =
                    mouseRayHitInfo.point + (randomPointsInCircle[i].point.x * tangent + randomPointsInCircle[i].point.y * biTangent);


                Ray testRay = GetOffsetRayTo(pointInRadius, mouseRayHitInfo.normal);

                if (Physics.Raycast(testRay, out RaycastHit surfaceInfo, TEST_RAY_DISTANCE_LIMIT))
                {
                    if (gameObjectsSpawnInfo[i] == null)
                    {
                        gameObjectsSpawnInfo[i] = new GameObjectSpawnInfo(surfaceInfo, SelectRandomGameObject());
                        DrawSphere(surfaceInfo.point, spawnRadius);

                    }
                    else gameObjectsSpawnInfo[i].surfaceInfo = surfaceInfo;
                }
            }

            HandleUtility.Repaint();
        }
    }

    private void DrawDiscOnSurface(Vector3 origin, float radius, Vector3 normal, Vector3 tangent)
    {
        Handles.color = Color.white;
        Vector3[] points = new Vector3[DISC_RESOLUTION];
        float angle = 0;
        for (int i = 0; i < DISC_RESOLUTION; i++)
        {
            Vector3 testRayOrigin = origin + (Quaternion.AngleAxis(angle, normal) * tangent * radius) + normal * OFFSET;
            Ray testRay = new Ray(testRayOrigin, -normal);
            if (Physics.Raycast(testRay, out RaycastHit surfaceHit, TEST_RAY_DISTANCE_LIMIT))
            {
                points[i] = surfaceHit.point;
            }
            angle += 360 / DISC_RESOLUTION;
        }

        Handles.DrawAAPolyLine(DISC_WIDTH, points);
    }

    private void InstantiateGameObjects()
    {
        for (int i = 0; i < gameObjectsSpawnInfo.Length; i++)
        {
            if (!IsValidPosition(gameObjectsSpawnInfo[i])) continue;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(gameObjectsSpawnInfo[i].gameObject);
            Undo.RegisterCreatedObjectUndo(go, "Spawn Prefab");
            go.transform.position = gameObjectsSpawnInfo[i].surfaceInfo.point;
            go.transform.rotation = gameObjectsSpawnInfo[i].rotation;
            go.transform.parent = parent;
        }
    }

    private void PreviewGameObjects(Camera cam)
    {
        if (gameObjectsSpawnInfo == null) return;

        for (int i = 0; i < gameObjectsSpawnInfo.Length; i++)
        {
            if (gameObjectsSpawnInfo[i] == null) continue;
            if (meshesByPrefab[gameObjectsSpawnInfo[i].gameObject] == null) continue;

            Quaternion rotation = gameObjectsSpawnInfo[i].rotation;


            previewMaterial.SetPass(0);

            foreach (MeshFilter filter in meshesByPrefab[gameObjectsSpawnInfo[i].gameObject])
            {
                if (!IsValidPosition(gameObjectsSpawnInfo[i])) continue;

                if (filter.sharedMesh == null) continue;

                Matrix4x4 rootToWorld =
                    Matrix4x4.TRS(
                    gameObjectsSpawnInfo[i].surfaceInfo.point,
                    gameObjectsSpawnInfo[i].rotation,
                    Vector3.one
                    );

                Matrix4x4 localToRoot = filter.transform.localToWorldMatrix;

                Matrix4x4 localToWorld = rootToWorld * localToRoot;

                Graphics.DrawMesh(filter.sharedMesh, localToWorld, previewMaterial, 0, cam);
            }

        }
    }

    private void TryIncreaseRadius()
    {
        so.Update();
        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        if (Event.current.type == EventType.ScrollWheel && holdingAlt)
        {
            propRadius.floatValue *= 1 + Mathf.Sign(Event.current.delta.y) * RADIUS_SCROLL_INCREMENT;
            Event.current.Use();
            Repaint();
        }
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPointsInCircle();
            SelectRandomPointsInCircle();
        }
    }

    private void GenerateRandomPointsInCircle()
    {
        pointsInCircle = new List<CirclePoint>();
        float radius = 0;
        float angle = 0;
        Vector2 testPoint = new Vector2();

        int numberOfRadiusIncreases = 0;


        while (IsValidPointInCircle(radius, angle, out testPoint))
        {
            CirclePoint circlePoint = new CirclePoint(testPoint, false);
            pointsInCircle.Add(circlePoint);

            if (angle >= 360 * Mathf.Deg2Rad || radius == 0)
            {
                angle = 0;
                radius += 2 * spawnRadius;
                numberOfRadiusIncreases++;
            }
            else
            {
                angle += (60 * Mathf.Deg2Rad) / numberOfRadiusIncreases;
            }
        }

    }

    private void SelectRandomPointsInCircle()
    {
        if (randomPointsInCircle != null)
        {
            foreach (CirclePoint cp in randomPointsInCircle)
            {
                cp.used = false;
            }
        }

        randomPointsInCircle = new List<CirclePoint>();
        gameObjectsSpawnInfo = new GameObjectSpawnInfo[count];

        int pointsSelected = 0;
        int maxNumberOfPoints = Mathf.Min(count, pointsInCircle.Count);
        while (pointsSelected < maxNumberOfPoints)
        {
            CirclePoint randPoint = pointsInCircle[Random.Range(0, pointsInCircle.Count)];
            if (!randPoint.used)
            {
                pointsSelected++;
                randPoint.used = true;
                randomPointsInCircle.Add(randPoint);
            }
        }

    }

    private bool IsValidPointInCircle(float radius, float angle, out Vector2 point)
    {
        point = radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        bool insideCircle = (point.x * point.x + point.y * point.y) <= (circleRadius * circleRadius);
        if (insideCircle) return true;
        return false;
    }

    private void LoadPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:prefab", new string[] { "Assets/Prefabs" });
        string[] paths = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            meshesByPrefab.Add(prefab, prefab.GetComponentsInChildren<MeshFilter>());
            isPrefabSelected.Add(prefab, false);
        }
    }

    private static Ray GetOffsetRayTo(Vector3 origin, Vector3 direction)
    {
        return new Ray(origin + direction * OFFSET, -direction);
    }

    private bool AreGameObjectsSelected()
    {
        return isPrefabSelected.ContainsValue(true);
    }

    private GameObject SelectRandomGameObject()
    {
        List<GameObject> prefabs = new List<GameObject>(isPrefabSelected.Keys);
        int randomIndex = Random.Range(0, prefabs.Count);
        GameObject selectedGO = prefabs[randomIndex];
        if (!isPrefabSelected[selectedGO]) return SelectRandomGameObject();
        return selectedGO;
    }

    private bool IsValidPosition(GameObjectSpawnInfo spawnInfo)
    {
        Ray ray = new Ray(spawnInfo.surfaceInfo.point, spawnInfo.surfaceInfo.normal);
        float meshHeight = CalculateHeight(spawnInfo.gameObject);
        return !Physics.Raycast(ray, meshHeight);
    }

    private float CalculateHeight(GameObject gameObject)
    {
        float totalHeight = 0;
        foreach (MeshRenderer renderer in gameObject.GetComponentsInChildren<MeshRenderer>())
        {
            float meshHeight = (renderer.bounds.center.y + renderer.bounds.extents.y);
            if (meshHeight <= totalHeight) continue;
            float extraHeight = (meshHeight - totalHeight);
            totalHeight += extraHeight;
        }
        return totalHeight;
    }

    private void DeselectUI()
    {
        GUI.FocusControl(null);
        Repaint();
    }

    private void DrawSphere(Vector3 pointPos, float radius)
    {
        Handles.SphereHandleCap(1, pointPos, Quaternion.identity, radius, EventType.Repaint);
    }
}


class GameObjectSpawnInfo
{
    public RaycastHit surfaceInfo
    {
        get
        {
            return _hitInfo;
        }
        set
        {
            _hitInfo = value;
            GenerateRotation();
        }

    }

    public GameObject gameObject;
    public Quaternion rotation;
    public float height;

    private Vector3 randVec;
    private RaycastHit _hitInfo;


    public GameObjectSpawnInfo(RaycastHit hitInfo, GameObject gameObject)
    {
        randVec = Random.insideUnitSphere;
        this.surfaceInfo = hitInfo;
        this.gameObject = gameObject;
    }

    private void GenerateRotation()
    {
        Vector3 randForward = Vector3.Cross(surfaceInfo.normal, randVec);
        rotation = Quaternion.LookRotation(randForward, surfaceInfo.normal);
    }
}