using TMPro;
using UnityEngine;

public class SatelliteLabelUI : MonoBehaviour
{
    public TextMeshProUGUI labelText;
    public Transform target;
    public Camera mainCamera;

    public void SetTarget(Transform satellite, string name)
    {
        target = satellite;
        labelText.text = name;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        target = null;
    }

    private void Update()
    {
        if (target == null || mainCamera == null)
        {
            Hide();
            return;
        }
    }
}
