using UnityEngine;
public class SlowSpinner : MonoBehaviour
{
    public Vector3 degreesPerSecond = new Vector3(0, 12f, 0);
    void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
}
