using UnityEngine;

public class FloatBubble : MonoBehaviour
{
    public float amplitude = 0.2f;     // Floating amplitude
    public float frequency = 1f;       // Floating frequency

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        Vector3 newPos = startPos;
        newPos.y += Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = newPos;
    }
}
