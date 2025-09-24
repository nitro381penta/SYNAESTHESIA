using UnityEngine;

public class TileBehavior : MonoBehaviour
{
    private Renderer rend;

    [Header("Glow Materials")]
    public Material inactiveEmission;
    public Material activeEmission;

    private void Start()
    {
        rend = GetComponent<Renderer>();

        if (rend == null)
        {
            Debug.LogError("TileBehavior: No Renderer found on " + gameObject.name);
            return;
        }

        if (inactiveEmission != null)
        {
            rend.material = inactiveEmission;
        }
        else
        {
            Debug.LogWarning("TileBehavior: Inactive Emission material not assigned.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Tile triggered by: " + other.name);

            if (rend != null && activeEmission != null)
            {
                rend.material = activeEmission;
                Debug.Log("Active emission applied.");
            }
            else
            {
                Debug.LogWarning("TileBehavior: Missing renderer or active material.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (rend != null && inactiveEmission != null)
            {
                rend.material = inactiveEmission;
                Debug.Log("Inactive emission reapplied.");
            }
            else
            {
                Debug.LogWarning("TileBehavior: Missing renderer or inactive material.");
            }
        }
    }
}
