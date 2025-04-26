using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;

[RequireComponent(typeof(CesiumGlobeAnchor))]
public class AltitudeClamp : MonoBehaviour
{
    [Tooltip("Minimale Höhe über dem Ellipsoid in Metern")]
    public float minHeightMeters = 200f;

    private CesiumGlobeAnchor _anchor;

    void Awake()
    {
        _anchor = GetComponent<CesiumGlobeAnchor>();
    }

    void LateUpdate()
    {
        // Hol die aktuelle LLH (Longitude/Latitude/Height)
        double3 llh = _anchor.longitudeLatitudeHeight;

        // Falls wir unter das Minimum fallen, clampe die Höhe
        if (llh.z < minHeightMeters)
        {
            llh.z = minHeightMeters;
            _anchor.longitudeLatitudeHeight = llh;
        }
    }
}