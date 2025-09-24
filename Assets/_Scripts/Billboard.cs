using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Transform playerCamera;

    void LateUpdate()
    {
        if (playerCamera != null)
        {
            Vector3 lookDirection = transform.position - playerCamera.position;
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}
