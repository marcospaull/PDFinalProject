using UnityEngine;

public class ModelInteraction : MonoBehaviour
{
    // Code from assignment instructions
    public float Speed = 5;
    public Camera mainCamera;
    private float cameraZDistance;

    void Start()
    {
        // Code from assignment instructions
        mainCamera = Camera.main;
        cameraZDistance = mainCamera.WorldToScreenPoint(transform.position).z;
    }

    void Update()
    {
        // Code from assignment instructions - Mouse left click for changing the object's rotation
        if (Input.GetMouseButton(0))
        {
            transform.eulerAngles += Speed * new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        }

        // Code from assignment instructions - Mouse right click for changing the object's position
        if (Input.GetMouseButton(1))
        {
            Vector3 screenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraZDistance);
            Vector3 newWorldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            transform.position = newWorldPosition;
        }
    }
}