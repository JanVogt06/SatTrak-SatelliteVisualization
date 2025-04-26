using UnityEngine;
using System.Collections;
using CesiumForUnity;
using Unity.Mathematics;

public class CesiumZoomController : MonoBehaviour
{
    public CesiumGlobeAnchor globeAnchor;
    public float zoomDuration = 2f;

    public double3 spaceView = new double3(11.666985, 51.217959, 300); 
    public double3 earthView = new double3(11.666985, 51.217959, 200);     

    public Vector3 spaceRotation = new Vector3(45f, 0f, 0f);   
    public Vector3 earthRotation = new Vector3(25f, 0f, 0f);   

    [Header("FOV Transition")]
    public Camera targetCamera;
    public float earthFov = 60f;
    public float spaceFov = 80f;
    public float fovTransitionDuration = 1.5f;
    public GlobeRotationController orbitController;

    private Coroutine zoomRoutine;

    private void Start()
    {
        ZoomToSpace();
    }

    public void ZoomToEarth()
    {
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomToPosition(earthView, Quaternion.Euler(earthRotation)));
        StartCoroutine(AnimateFOV(spaceFov, earthFov));
    }

    public void ZoomToSpace()
    {
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomToPosition(spaceView, Quaternion.Euler(spaceRotation)));
        StartCoroutine(AnimateFOV(earthFov, spaceFov));
    }

    IEnumerator ZoomToPosition(double3 targetLLH, Quaternion targetRotation)
    {
        double3 startLLH = globeAnchor.longitudeLatitudeHeight;
        Quaternion startRotation = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / zoomDuration;
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            double3 currentLLH = math.lerp(startLLH, targetLLH, smoothedT);
            globeAnchor.longitudeLatitudeHeight = currentLLH;

            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothedT);

            yield return null;
        }

        globeAnchor.longitudeLatitudeHeight = targetLLH;
        transform.rotation = targetRotation;

        // <- hier Initialisierung für den Orbit-Controller:
        if (this.orbitController != null)
        {
            this.orbitController.InitializeOrbit();
        }         
    }

    IEnumerator AnimateFOV(float from, float to)
    {
        if (targetCamera == null)
            yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fovTransitionDuration;
            float eased = Mathf.SmoothStep(0, 1, t);
            targetCamera.fieldOfView = Mathf.Lerp(from, to, eased);
            yield return null;
        }

        targetCamera.fieldOfView = to;
    }
}