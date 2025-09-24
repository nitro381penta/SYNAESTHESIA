using UnityEngine;

public class FollowCameraUI : MonoBehaviour
{
    public Transform cameraTransform;         
    public float distanceFromCamera = 3.0f;   
    public float heightOffset =-0.3f;       
    public bool repositionOnEnable = true;  

    void OnEnable()
    {
        if (repositionOnEnable)
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;

            PositionCanvasOnce();
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Always face the camera
        Vector3 lookDirection = transform.position - cameraTransform.position;
        lookDirection.y = 0; // Keep upright
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    void PositionCanvasOnce()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 targetPosition = cameraTransform.position + forward * distanceFromCamera;
        targetPosition.y += heightOffset;

        transform.position = targetPosition;
    }
}
