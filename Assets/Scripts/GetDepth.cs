using UnityEngine;
using Meta.XR;
using Meta.XR.EnvironmentDepth;
using TMPro;
using UnityEngine.UI;

public class GetDepth : MonoBehaviour
{
    public Transform rightController;
    public EnvironmentRaycastManager raycastManager;
    // public TextMeshPro text;
    public Transform text;

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            var ray = new Ray(rightController.position, rightController.forward);
            if (raycastManager.Raycast(ray, out EnvironmentRaycastHit hit, 100f))
            {
                Debug.Log("Hit depth at distance: " + hit.point);
                text.GetComponent<TMP_Text>().text = "Depth information: " + hit.point.ToString("F2");
            }
            else
            {
                Debug.Log("No depth hit detected.");
                text.GetComponent<TMP_Text>().text = "No depth hit detected.";
            }
        }

    }
}
