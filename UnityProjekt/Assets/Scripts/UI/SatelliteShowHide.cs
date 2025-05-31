using Satellites;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SatelliteShowHide : MonoBehaviour
{
    public Toggle showHide;
    public GameObject satelliteParent;

    private void Start()
    {
        showHide.isOn = true;
        satelliteParent.SetActive(true);
    }

    public void ShowHideSatellites()
    {
        bool state = showHide.isOn;

        // Sichtbarkeit
        satelliteParent.SetActive(state);

        // Logik stoppen
        SatelliteManager.Instance.satellitesActive = state;
    }

}
