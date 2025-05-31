using UnityEngine;
using System.Collections;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine.UI;
using Satellites;
using UnityEngine.Experimental.GlobalIllumination;

public class CesiumZoomController : MonoBehaviour
{
    public CesiumGlobeAnchor globeAnchor;

    public double3 spaceView = new double3(11.666985, 51.217959, 300);

    public Vector3 spaceRotation = new Vector3(45f, 0f, 0f);
    public Vector3 earthRotation = new Vector3(25f, 0f, 0f);

    [Header("FOV Transition")]
    public Camera targetCamera;
    public float earthFov = 60f;
    public float spaceFov = 80f;
    public float fovTransitionDuration = 2.3f;
    public GlobeRotationController orbitController;

    private Coroutine zoomRoutine;

    public Button spaceButton;

    public GameObject search;

    public FreeFlyCamera freeFlyCameraScript;

    public CesiumGeoreference georeference;

    public Light directionalLight;

    [Header("Screen Fade")]
    public Image fadePanel;

    private bool insideSat;


    private void Start()
    {
        insideSat = false;
        directionalLight.transform.eulerAngles = new Vector3(90f, 0f, 0f);
        directionalLight.intensity = 25;
        search.SetActive(true);
        spaceButton.interactable = false;
        ZoomToSpace();
    }

    public void ZoomToEarth(double3 earthView)
    {
        search.SetActive(false);
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = null;
        zoomRoutine = StartCoroutine(ZoomToPosition(earthView, Quaternion.Euler(earthRotation), false, 2.3f));
        StartCoroutine(AnimateFOV(spaceFov, earthFov));
    }

    public void SnapToSatellit(Satellite view)
    {
        search.SetActive(false);
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);

        zoomRoutine = StartCoroutine(ZoomToPositionSatellite(view, Quaternion.Euler(earthRotation), false));

        targetCamera.fieldOfView = 60f;
    }

    public void ZoomToSpace()
    {
        if (insideSat)
        {
            StartCoroutine(FadeToBlack());
            StartCoroutine(BlackSpace());
        }
        else
        {
            search.SetActive(false);
            spaceButton.interactable = false;
            if (zoomRoutine != null) StopCoroutine(zoomRoutine);
            zoomRoutine = StartCoroutine(ZoomToPosition(spaceView, Quaternion.Euler(spaceRotation), true, 2.3f));
            georeference.latitude = 51.21796;
            georeference.longitude = 11.66699;
            georeference.height = 400;
            StartCoroutine(AnimateFOV(earthFov, spaceFov));
        }
    }

    public IEnumerator BlackSpace()
    {
        yield return new WaitForSeconds(1.2f);

        search.SetActive(false);
        spaceButton.interactable = false;
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomToPosition(spaceView, Quaternion.Euler(spaceRotation), true, 0.5f));
        georeference.latitude = 51.21796;
        georeference.longitude = 11.66699;
        georeference.height = 400;
        StartCoroutine(AnimateFOV(earthFov, spaceFov));

        yield return new WaitForSeconds(2f);
        StartCoroutine(FadeFromBlack());

        search.SetActive(true);
        insideSat = false;
    }

    public IEnumerator ZoomToPositionSatellite(Satellite sat, Quaternion targetRotation, bool space)
    {
        yield return new WaitForSeconds(0.5f);

        if (sat == null) yield break;

        double3 startLLH = globeAnchor.longitudeLatitudeHeight;
        Quaternion startRotation = transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0;
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            // Aktuelle Position des Satelliten in Unity-Koordinaten
            Vector3 satWorldPos = sat.transform.position;
            double3 targetLLH = new double3(satWorldPos.x, satWorldPos.y, satWorldPos.z);

            // Interpolierter LLH-Wert
            double3 currentLLH = math.lerp(startLLH, targetLLH, smoothedT);
            globeAnchor.longitudeLatitudeHeight = currentLLH;

            // Interpolierte Rotation
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothedT);

            yield return null;
        }

        // Endposition exakt setzen
        Vector3 finalPos = sat.transform.position;
        globeAnchor.longitudeLatitudeHeight = new double3(finalPos.x, finalPos.y, finalPos.z);
        transform.rotation = targetRotation;

        freeFlyCameraScript.SyncInitTransform();

        if (orbitController != null)
        {
            orbitController.InitializeOrbit();
        }

        search.SetActive(true);

        directionalLight.intensity = 1.5f;
        insideSat = true;

        yield return new WaitForSeconds(1f);

        StartCoroutine(FadeFromBlack());

        if (!space)
        {
            StartCoroutine(SpaceButton(space));
        }
    }

    public IEnumerator SpaceButton(bool space)
    {
        yield return new WaitForSeconds(2f);

        spaceButton.interactable = true;
    }

    void LateUpdate()
    {
        if (insideSat == true)
        {
            directionalLight.transform.rotation = targetCamera.transform.rotation;
            search.SetActive(false);
        }
        else
        {
            search.SetActive(true);
        }
    }

    IEnumerator ZoomToPosition(double3 targetLLH, Quaternion targetRotation, bool space, float zoomDuration)
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

        freeFlyCameraScript.SyncInitTransform();

        if (this.orbitController != null)
        {
            this.orbitController.InitializeOrbit();
        }

        if (!space)
        {
            spaceButton.interactable = true;
            insideSat = false;
            directionalLight.intensity = 5;
            directionalLight.transform.eulerAngles = new Vector3(-90f, 0f, 0f);
        }
        else
        {
            insideSat = false;
            directionalLight.intensity = 25;
            directionalLight.transform.eulerAngles = new Vector3(90f, 0f, 0f);
        }

        search.SetActive(true);
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

    public IEnumerator FadeToBlack()
    {
        if (fadePanel == null) yield break;
        float t = 0f;
        Color c = fadePanel.color;
        while (t < 1f)
        {
            t += Time.deltaTime / 1f;
            c.a = Mathf.Lerp(0f, 1f, t);
            fadePanel.color = c;
            yield return null;
        }
        c.a = 1f;
        fadePanel.color = c;
    }

    public IEnumerator FadeFromBlack()
    {
        if (fadePanel == null) yield break;
        float t = 0f;
        Color c = fadePanel.color;
        while (t < 1f)
        {
            t += Time.deltaTime / 2f;
            c.a = Mathf.Lerp(1f, 0f, t);
            fadePanel.color = c;
            yield return null;
        }
        c.a = 0f;
        fadePanel.color = c;
    }

}