using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

[CanEditMultipleObjects]
public class SnapperTool : EditorWindow
{

    const float MIN_SCALE = 0.1f;
    const float MIN_DIMENSION = 1f;

    public float width = 10;
    public float height = 2;
    public float depth = 10;
    public float xScale = 1;
    public float yScale = 1;
    public float zScale = 1;
    public float radius = 10;
    public float radiusScale = 1;
    public float angularScale = 10;


    public GridOriginOptions gridOrigin = GridOriginOptions.World;
    public GridShapeOptions gridShape = GridShapeOptions.Cartesian;




    SerializedProperty propWidth;
    SerializedProperty propHeight;
    SerializedProperty propDepth;
    SerializedProperty propXScale;
    SerializedProperty propYScale;
    SerializedProperty propZScale;
    SerializedProperty propRadius;
    SerializedProperty propRadiusScale;
    SerializedProperty propAngularScale;
    SerializedProperty propGridOrigin;
    SerializedProperty propGridShape;

    SerializedObject so;

    public enum GridOriginOptions
    {
        Selection,
        World,
    }

    public enum GridShapeOptions
    {
        Radial,
        Cartesian,
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);

        propWidth = so.FindProperty("width");
        propHeight = so.FindProperty("height");
        propDepth = so.FindProperty("depth");
        propXScale = so.FindProperty("xScale");
        propYScale = so.FindProperty("yScale");
        propZScale = so.FindProperty("zScale");
        propRadius = so.FindProperty("radius");
        propRadiusScale = so.FindProperty("radiusScale");
        propAngularScale = so.FindProperty("angularScale");
        propGridOrigin = so.FindProperty("gridOrigin");
        propGridShape = so.FindProperty("gridShape");

        Selection.selectionChanged += Repaint;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        Handles.zTest = CompareFunction.LessEqual;
        if (Event.current.type == EventType.Repaint)
        {
            if (gridShape == GridShapeOptions.Cartesian) DisplayCartesianGrid(GetGridOrigin());
            if (gridShape == GridShapeOptions.Radial) DisplayRadialGrid(GetGridOrigin());
        }
    }



    private void OnDisable()
    {
        Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    [MenuItem("Tools/Snapper")]
    private static void ShowWindow()
    {
        var window = GetWindow<SnapperTool>();
        window.titleContent = new GUIContent("Snapper");
        window.Show();
    }

    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propGridShape);
        so.ApplyModifiedProperties();

        so.Update();
        switch (gridShape)
        {
            case GridShapeOptions.Cartesian:
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Dimensions:");
                    EditorGUILayout.PropertyField(propWidth);
                    EditorGUILayout.PropertyField(propHeight);
                    EditorGUILayout.PropertyField(propDepth);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Scale:");
                    EditorGUILayout.PropertyField(propXScale);
                    EditorGUILayout.PropertyField(propYScale);
                    EditorGUILayout.PropertyField(propZScale);
                }

                propWidth.floatValue = Mathf.Max(propWidth.floatValue, MIN_DIMENSION);
                propHeight.floatValue = Mathf.Max(propHeight.floatValue, MIN_DIMENSION);
                propDepth.floatValue = Mathf.Max(propDepth.floatValue, MIN_DIMENSION);
                propXScale.floatValue = Mathf.Max(Mathf.Min(propXScale.floatValue, propWidth.floatValue / 2), MIN_SCALE);
                propYScale.floatValue = Mathf.Max(Mathf.Min(propYScale.floatValue, propHeight.floatValue / 2), MIN_SCALE);
                propZScale.floatValue = Mathf.Max(Mathf.Min(propZScale.floatValue, propDepth.floatValue / 2), MIN_SCALE);
                break;

            case GridShapeOptions.Radial:

                EditorGUILayout.PropertyField(propRadius);
                EditorGUILayout.PropertyField(propRadiusScale);
                EditorGUILayout.PropertyField(propAngularScale);


                propRadius.floatValue = Mathf.Max(propRadius.floatValue, MIN_DIMENSION);
                propRadiusScale.floatValue = Mathf.Max(Mathf.Min(propRadiusScale.floatValue, propRadius.floatValue / 2), MIN_SCALE);
                propAngularScale.floatValue = Mathf.Max(Mathf.Min(propAngularScale.floatValue, 180), MIN_SCALE);
                break;
        }



        EditorGUILayout.PropertyField(propGridOrigin);

        so.ApplyModifiedProperties();



        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Snap Selection"))
            {
                SnapSelection(gridShape);
            }
        }
    }

    private void DisplayCartesianGrid(Vector3 origin)
    {

        Handles.color = Color.blue;
        Handles.DrawLine(new Vector3(width / 2, 0, depth / 2) + origin, new Vector3(-width / 2, 0, depth / 2) + origin);
        Handles.DrawLine(new Vector3(-width / 2, 0, depth / 2) + origin, new Vector3(-width / 2, 0, -depth / 2) + origin);
        Handles.DrawLine(new Vector3(-width / 2, 0, -depth / 2) + origin, new Vector3(width / 2, 0, -depth / 2) + origin);
        Handles.DrawLine(new Vector3(width / 2, 0, -depth / 2) + origin, new Vector3(width / 2, 0, depth / 2) + origin);


        Handles.color = Color.white;

        for (int i = 0; i < Mathf.Floor(height / (2 * yScale)); i++)
        {
            float yOffset = i * yScale;

            for (int j = 0; j < Mathf.Floor((width) / (2 * xScale)) + 1; j++)
            {
                float xOffset = j * xScale;
                Handles.DrawLine(new Vector3(xOffset, yOffset, -depth / 2) + origin, new Vector3(xOffset, yOffset, depth / 2) + origin);
                Handles.DrawLine(new Vector3(-xOffset, yOffset, -depth / 2) + origin, new Vector3(-xOffset, yOffset, depth / 2) + origin);
                if (yOffset == 0) continue;
                Handles.DrawLine(new Vector3(xOffset, -yOffset, -depth / 2) + origin, new Vector3(xOffset, -yOffset, depth / 2) + origin);
                Handles.DrawLine(new Vector3(-xOffset, -yOffset, -depth / 2) + origin, new Vector3(-xOffset, -yOffset, depth / 2) + origin);
            }

            for (int j = 0; j < Mathf.Floor((depth) / (2 * zScale)) + 1; j++)
            {
                float zOffset = j * zScale;
                Handles.DrawLine(new Vector3(-width / 2, yOffset, zOffset) + origin, new Vector3(width / 2, yOffset, zOffset) + origin);
                Handles.DrawLine(new Vector3(-width / 2, yOffset, -zOffset) + origin, new Vector3(width / 2, yOffset, -zOffset) + origin);
                if (yOffset == 0) continue;
                Handles.DrawLine(new Vector3(-width / 2, -yOffset, zOffset) + origin, new Vector3(width / 2, -yOffset, zOffset) + origin);
                Handles.DrawLine(new Vector3(-width / 2, -yOffset, -zOffset) + origin, new Vector3(width / 2, -yOffset, -zOffset) + origin);
            }
            HandleUtility.Repaint();
        }
    }


    private void DisplayRadialGrid(Vector3 origin)
    {
        Handles.color = Color.blue;
        Handles.DrawWireArc(origin, Vector3.up, Vector3.forward, 360, radius);


        Handles.color = Color.white;
        for (int i = 1; i < Mathf.Floor((radius) / (radiusScale)); i++)
        {
            float radiusOffset = i * radiusScale;
            Handles.DrawWireArc(origin, Vector3.up, Vector3.forward, 360, radiusOffset);
        }


        for (int i = 0; i < Mathf.Floor((180) / (angularScale)) + 1; i++)
        {
            float angularOffset = i * angularScale;
            Handles.DrawLine(origin + Quaternion.Euler(0, angularOffset, 0) * Vector3.forward * radius, origin + Quaternion.Euler(0, angularOffset, 0) * Vector3.forward * -radius);
        }

        HandleUtility.Repaint();
    }

    private Vector3 GetGridOrigin()
    {
        switch (gridOrigin)
        {
            case GridOriginOptions.Selection:

                Vector3 meanVector = Vector3.zero;

                foreach (GameObject go in Selection.gameObjects)
                {
                    meanVector += go.transform.position;
                }
                meanVector /= Selection.gameObjects.Length;

                if (gridShape == GridShapeOptions.Cartesian) return SnapVector(meanVector, gridShape);
                return meanVector;

            case GridOriginOptions.World:
                return Vector3.zero;
            default: return Vector3.zero;
        }
    }


    private void SnapSelection(GridShapeOptions gridShape)
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, "Snap Selection");
            go.transform.position = SnapVector(go.transform.position, gridShape);
        }
    }

    private Vector3 SnapVector(Vector3 v, GridShapeOptions gridShape)
    {
        switch (gridShape)
        {
            case GridShapeOptions.Cartesian:
                float x = RoundToMultiple(v.x, xScale);
                float y = RoundToMultiple(v.y, yScale);
                float z = RoundToMultiple(v.z, zScale);

                return new Vector3(x, y, z);

            case GridShapeOptions.Radial:


                float magnitude = RoundToMultiple(v.magnitude, radiusScale);

                float angle = Vector3.Angle(new Vector3(v.x, 0, v.z), Vector3.forward);
                float roundedAngle = RoundToMultiple(Mathf.Abs(angle), angularScale) * Mathf.Sign(angle);

                return Quaternion.Euler(0, roundedAngle, 0) * (Vector3.forward * magnitude);

            default: return Vector3.zero;
        }
    }

    private float RoundToMultiple(float number, float multipleOf)
    {
        return Mathf.Round(number / multipleOf) * multipleOf;
    }
}
