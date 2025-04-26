using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Collections;

/// <summary>
/// Always starts in space view and plays a one‑time spin around the Earth with easing.
/// After the intro, you can enable your orbit or fly scripts as needed.
/// </summary>
[RequireComponent(typeof(Camera))]
public class IntroOrbitCamera : MonoBehaviour
{
    [Header("Space Start")]
    [Tooltip("Field‑of‑View for Space Mode at Start")]
    public float spaceFov = 80f;

    [Header("Intro Spin")]
    [Tooltip("How many seconds the initial orbit takes")]
    public float orbitDuration = 5f;
    [Tooltip("Easing curve (0→1) for slowing down the spin")]
    public AnimationCurve orbitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References (optional)")]
    [Tooltip("If you have an orbit controller to re‑enable after intro")]
    public MonoBehaviour orbitController;
    [Tooltip("If you have a freefly camera to disable during intro")]
    public MonoBehaviour freeFlyController;

    private Camera _camera;
    private Vector3 _pivot;
    private float _radius;

    void Start()
    {
        _camera = GetComponent<Camera>();

        // Always start in space FOV
        _camera.fieldOfView = this.spaceFov;

        // Disable user control during intro
        if (this.orbitController != null) this.orbitController.enabled = false;
        if (this.freeFlyController != null) this.freeFlyController.enabled = false;

        // Compute pivot = Earth center in Unity coords
        var georef = FindObjectOfType<CesiumGeoreference>();
        var ecef = new double3(0, 0, 0);
        var unityPos = georef.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
        _pivot = new Vector3((float)unityPos.x, (float)unityPos.y, (float)unityPos.z);

        // Compute radius = distance from camera to pivot
        _radius = Vector3.Distance(transform.position, _pivot);

        // Start the spin
        StartCoroutine(PlayIntroOrbit());
    }

    IEnumerator PlayIntroOrbit()
    {
        float elapsed = 0f;
        while (elapsed < this.orbitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / this.orbitDuration);
            float eased = this.orbitCurve.Evaluate(t);

            // Angle from 0 to 360 over time with easing
            float angle = Mathf.Lerp(0f, 360f, eased);

            // Compute position on circle
            Quaternion rot = Quaternion.Euler(0f, angle, 0f);
            Vector3 offset = new Vector3(0f, 0f, -_radius);
            transform.position = _pivot + rot * offset;

            // Always look to center
            transform.LookAt(_pivot);

            yield return null;
        }

        // Re-enable controllers after intro
        if (this.orbitController != null) this.orbitController.enabled = true;
        if (this.freeFlyController != null) this.freeFlyController.enabled = true;
    }
}
