using UnityEngine;

public class CanvasLookCam : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        if (mainCamera != null)
        {
            // Set the camera to look at the canvas
            transform.LookAt(mainCamera.transform);
            // Optionally, you can also set the rotation to be flat
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
        else
        {
            Debug.LogWarning("Main camera not found. Please assign a camera to the CanvasLookCam script.");
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (mainCamera != null)
        {
            // Continuously look at the camera
            transform.LookAt(mainCamera.transform);
            // Keep the rotation flat
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
        else
        {
            Debug.LogWarning("Main camera not found. Please assign a camera to the CanvasLookCam script.");
        }


    }
}
