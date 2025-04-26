using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

public class GlobeRotationController : MonoBehaviour
{
    [Header("Mode Switch")]
    public FreeFlyCamera freeFlyCamera;
    public CesiumGeoreference georeference;
    public float fovThreshold = 70f;

    [Header("Orbit Settings")]
    public float rotationSpeed = 0.2f;
    public float inertiaDamping = 3f;
    public float maxPitch = 85f;

    [Header("Smoothing")]
    [Tooltip("Wie stark die Kamera der Zielrotation nachgleitet")]
    public float smoothFactor = 5f;

    private Vector2 lastMouse;
    private Vector2 inertia;
    private bool dragging;

    private Vector3 pivot;
    private float targetYaw, targetPitch;
    private float currentYaw, currentPitch;
    private float distance;

    void Start()
    {
        // Anfangs-Pivot und Kameradistanz ermitteln
        UpdatePivot();
        Vector3 dir = (transform.position - pivot);
        distance = dir.magnitude;
        // Start-Yaw/Pitch aus der aktuellen Rotation
        Quaternion init = transform.rotation;
        Vector3 e = init.eulerAngles;
        targetYaw = currentYaw = e.y;
        targetPitch = currentPitch = e.x;
    }

    void UpdatePivot()
    {
        // Wandelt ECEF-(0,0,0) in Unity‐Space um
        var u = georeference.TransformEarthCenteredEarthFixedPositionToUnity(new double3(0, 0, 0));
        pivot = new Vector3((float)u.x, (float)u.y, (float)u.z);
    }

    void Update()
    {
        Camera cam = GetComponent<Camera>();
        bool spaceMode = cam.fieldOfView >= fovThreshold;
        freeFlyCamera.enabled = !spaceMode;
        if (!spaceMode)
            return;

        // Eingabe
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            lastMouse = Input.mousePosition;
            inertia = Vector2.zero;
        }

        if (Input.GetMouseButton(0) && dragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMouse;
            lastMouse = Input.mousePosition;

            // Ziele anpassen
            targetYaw += delta.x * rotationSpeed;
            targetPitch -= delta.y * rotationSpeed;
            targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch);

            inertia = delta / Time.deltaTime;
        }

        if (Input.GetMouseButtonUp(0))
            dragging = false;

        // Inertia weiterdrehen
        if (!dragging && inertia.magnitude > 0.01f)
        {
            targetYaw += inertia.x * rotationSpeed * Time.deltaTime;
            targetPitch -= inertia.y * rotationSpeed * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch, -maxPitch, maxPitch);

            inertia = Vector2.Lerp(inertia, Vector2.zero, inertiaDamping * Time.deltaTime);
        }

        // Smooth auf die Ziel-Yaw/Pitch gleiten lassen
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, smoothFactor * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, smoothFactor * Time.deltaTime);

        // Kamera neu positionieren und drehen
        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 dir = rot * Vector3.forward;
        transform.position = pivot - dir * distance;
        transform.rotation = rot;
    }

    public void InitializeOrbit()
    {
        // 1) Pivot neu holen (falls sich georeference verschoben hat)
        UpdatePivot();

        // 2) Abstand von der Kamera zum Pivot merken
        Vector3 dir = transform.position - pivot;
        distance = dir.magnitude;

        // 3) Aktuelle Rotation als Ausgangswinkel übernehmen
        Vector3 e = transform.rotation.eulerAngles;
        targetYaw = currentYaw = e.y;
        targetPitch = currentPitch = e.x;
    }
}