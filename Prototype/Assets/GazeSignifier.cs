using System.Collections;
using System.Collections.Generic;
using Tobii.Gaming;
using UnityEngine;


public class GazeSignifier : MonoBehaviour {
    public float visualizationDistance = 10.0f;
    private bool _hasHistoricPoint;
    private Vector3 _historicPoint;
    public float filterSmoothingFactor = 0.5f;

    void Update() {
        if (!TobiiAPI.IsConnected) return;
        GazePoint gazePoint = TobiiAPI.GetGazePoint();
        Vector3 gazePointInWorld = ProjectToPlaneInWorld(gazePoint);
        transform.position = Smoothify(gazePointInWorld);
    }

    private Vector3 ProjectToPlaneInWorld(GazePoint gazePoint) {
        Vector3 gazeOnScreen = gazePoint.Screen;
        gazeOnScreen += (transform.forward * visualizationDistance);
        return Camera.main.ScreenToWorldPoint(gazeOnScreen);
    }

    private Vector3 Smoothify(Vector3 point) {
        if (!_hasHistoricPoint) {
            _historicPoint = point;
            _hasHistoricPoint = true;
        }

        var smoothedPoint = new Vector3(
            point.x * (1.0f - filterSmoothingFactor) + _historicPoint.x * filterSmoothingFactor,
            point.y * (1.0f - filterSmoothingFactor) + _historicPoint.y * filterSmoothingFactor,
            point.z * (1.0f - filterSmoothingFactor) + _historicPoint.z * filterSmoothingFactor);

        _historicPoint = smoothedPoint;

        return smoothedPoint;
    }
}