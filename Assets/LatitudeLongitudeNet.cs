using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// This script plots latitude-longitude coordinates net on 3D model of Earth.
/// How to create a net:
/// 1. Find scriptable object in the folder of this asset or create it by clicking right mouse button in Project menu -> Instruments/Latitude-Longitude Net;
/// 2. Set parameters of net (plot steps for latitude and longitude (must be multiple of accuracy steps), calculation steps (must always be less than plot steps));
/// 3. Set prefab with LineRenderer component if scriptable object was created in step 1;
/// 4. Click "Plot Net" button.
/// 5. That's it, now you have a net, you can keep it in scene or save as a new prefab.
/// </summary>

[CreateAssetMenu(fileName = "Latitude-Longitude Net", menuName = "Instruments/Latitude-Longitude Net")]
public class LatitudeLongitudeNet : ScriptableObject
{
    private enum Axes { x_Axis, y_Axis, z_Axis }

    private const float EarthRadius = 6371f;
	
    // Calculation step of coordinates net for latitude and longitude.
    [Serializable]
    public class Accuracies
    {
        public float latitude = 1;
        public float longitude = 1;
    }

    // Plot step of coordinates net for latitude and longitude.
    [Serializable]
    public class Steps
    {
        public int latitude = 5;
        public int longitude = 5;
    }

    [Tooltip("Accuracy of calculating in degrees")]
    public Accuracies accuracies;
    [Tooltip("Plot step in degrees")]
    public Steps steps;
    [Tooltip("Additional rotate vector")] // Rotate vector for Unity axis (y is up)
    public Vector3 rotAngles = new Vector3(0, -90, 90);
    [Tooltip("Line prefab")]
    public GameObject objCoordLine;

    public float scaleCoefficient = 0.01f;

    private class NetCoordinates
    {
        public Vector3[] coords;
    }

    private List<NetCoordinates> latCoords = new List<NetCoordinates>();
    private List<NetCoordinates> longCoords = new List<NetCoordinates>();

    private int latVectLength, longVectLength;


    public void CalculateNet()
    {
        // ###Create vectors of latitude lines and longitude lines###

        // 2 semicircles around Earth:
        latVectLength = (int)(180 / accuracies.latitude) * 2;
        longVectLength = (int)(360 / accuracies.longitude) * 2;

        float[] latVect = new float[latVectLength];
        float[] longVect = new float[longVectLength];

        for (int i = 0; i < latVect.Length; i++)
        {
            latVect[i] = (float)i * accuracies.latitude;
        }
            
        for (int i = 0; i < longVect.Length; i++)
        {
            longVect[i] = (float)i * accuracies.longitude;
        }
            

        for (int i = 0; i < longVectLength; i++)
        {
            NetCoordinates coords_0 = new NetCoordinates();
            coords_0.coords = new Vector3[latVectLength];
            latCoords.Add(coords_0);
        }

        for (int j = 0; j < latVectLength; j++)
        {
            NetCoordinates coords_0 = new NetCoordinates();
            coords_0.coords = new Vector3[longVectLength];
            longCoords.Add(coords_0);
        }

        GameObject objVectorRotator = new GameObject("Rotator");

        // ###Calculate XYZ positions of net###
        for (int i = 0; i < latVectLength; i++)
        {
            for (int j = 0; j < longVectLength; j++)
            {
                float rEarthAdd = 12f; // for plotting net a little above the object.

                float x = (EarthRadius + rEarthAdd) * Mathf.Cos((latVect[i]) * Mathf.PI / 180f) *
                    Mathf.Cos((longVect[j]) * Mathf.PI / 180f) * scaleCoefficient;
                float y = (EarthRadius + rEarthAdd) * Mathf.Cos((latVect[i]) * Mathf.PI / 180f) *
                    Mathf.Sin((longVect[j]) * Mathf.PI / 180f) * scaleCoefficient;
                float z = (EarthRadius + rEarthAdd) * Mathf.Sin((latVect[i]) * Mathf.PI / 180f) * scaleCoefficient;

                latCoords[j].coords[i].x = x;
                latCoords[j].coords[i].y = y;
                latCoords[j].coords[i].z = z;

                objVectorRotator.transform.position = latCoords[j].coords[i];
                // Rotate for Unity axis.
                latCoords[j].coords[i] = PosRotateObject(objVectorRotator.transform, rotAngles);

                longCoords[i].coords[j].x = x;
                longCoords[i].coords[j].y = y;
                longCoords[i].coords[j].z = z;

                objVectorRotator.transform.position = longCoords[i].coords[j];
                // Rotate for Unity axis.
                longCoords[i].coords[j] = PosRotateObject(objVectorRotator.transform, rotAngles);
            }
        }

        DestroyImmediate(objVectorRotator);
    }

    public void PlotNet()
    {
        GameObject parentObj = new GameObject("Latitude-Longitude Net: " + steps.latitude + ":" + steps.longitude);

        for (int i = 0; i < longVectLength; i += steps.latitude)
        {
            GameObject objNewLine = Instantiate(objCoordLine) as GameObject;
            objNewLine.name = "Latitude № " + i;
            LineRenderer _linR = objNewLine.GetComponent<LineRenderer>();
            _linR.positionCount = latCoords[i].coords.Length;
            _linR.SetPositions(latCoords[i].coords);
            objNewLine.transform.SetParent(parentObj.transform);
        }

        for (int i = 0; i < latVectLength; i += steps.longitude)
        {
            GameObject objNewLine = Instantiate(objCoordLine) as GameObject;
            objNewLine.name = "Longitude № " + i;
            LineRenderer _linR = objNewLine.GetComponent<LineRenderer>();
            _linR.positionCount = longCoords[i].coords.Length;
            _linR.SetPositions(longCoords[i].coords);
            objNewLine.transform.SetParent(parentObj.transform);
        }
    }

    private Vector3 PosRotateObject(Transform _trObj, Vector3 rotAngles)
    {
        RotateObject(_trObj, rotAngles.x, Axes.x_Axis);
        RotateObject(_trObj, rotAngles.y, Axes.y_Axis);
        RotateObject(_trObj, rotAngles.z, Axes.z_Axis);

        return _trObj.position;
    }

    private void RotateObject(Transform _trObj, float _angle, Axes _axes)
    {
        Vector3 _rotAround = Vector3.zero;

        switch (_axes)
        {
            case Axes.x_Axis:
                _rotAround = Vector3.right;
                break;
            case Axes.y_Axis:
                _rotAround = Vector3.up;
                break;
            case Axes.z_Axis:
                _rotAround = Vector3.forward;
                break;
        }

        _trObj.RotateAround(Vector3.zero, _rotAround, _angle);
    }
}


[CustomEditor(typeof(LatitudeLongitudeNet))]
public class CoordinatesEditor : Editor
{
    LatitudeLongitudeNet o;
    Editor lineRendererEditor;

    private void OnEnable()
    {
        o = (LatitudeLongitudeNet)target;
        
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (o.objCoordLine == null)
        {
            GUILayout.Label("Choose line prefab!", EditorStyles.boldLabel);
            return;
        }

        if (o.objCoordLine.GetComponent<LineRenderer>() == null)
        {
            GUILayout.Label("Prefab doesn't have a LineRenderer component!", EditorStyles.boldLabel);
            return;
        }

        lineRendererEditor = CreateEditor(o.objCoordLine.GetComponent<LineRenderer>());

        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Plot Net", "LargeButton"))
        {
            o.CalculateNet();
            o.PlotNet();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Space(20);
        GUILayout.Label("LineRenderer settings:", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        lineRendererEditor.OnInspectorGUI();
    }
}
