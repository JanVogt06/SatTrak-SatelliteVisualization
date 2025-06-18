using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class CameraAccessMonitor : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float lastFOV;
    private Color lastBackgroundColor;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastFOV = cam.fieldOfView;
        lastBackgroundColor = cam.backgroundColor;
    }

    void Update()
    {
        if (transform.position != lastPosition)
        {
            ReportChange("Position");
            lastPosition = transform.position;
        }

        if (transform.rotation != lastRotation)
        {
            ReportChange("Rotation");
            lastRotation = transform.rotation;
        }

        if (cam.fieldOfView != lastFOV)
        {
            ReportChange("Field of View");
            lastFOV = cam.fieldOfView;
        }

        if (cam.backgroundColor != lastBackgroundColor)
        {
            ReportChange("Background Color");
            lastBackgroundColor = cam.backgroundColor;
        }
    }

    void ReportChange(string propertyName)
    {
        StackTrace trace = new StackTrace(2, true); // 2 Frames überspringen (Update + ReportChange)
        Debug.Log($"[{Time.frameCount}] Änderung an Kamera-{propertyName} erkannt.\nUrsprung:\n{trace}");
    }
}
