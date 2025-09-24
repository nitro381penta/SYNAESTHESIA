using UnityEngine;

public class FloorGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public Transform player;
    public int range = 5;

    private Vector3 lastPlayerTilePos;

    void Start()
    {
        if (tilePrefab == null || player == null)
        {
            Debug.LogError("FloorGenerator: Assign tilePrefab and player!");
            return;
        }

        lastPlayerTilePos = GetPlayerTilePos();
        GenerateTilesAroundPlayer();
    }

    void Update()
    {
        Vector3 currentTilePos = GetPlayerTilePos();
        if (currentTilePos != lastPlayerTilePos)
        {
            lastPlayerTilePos = currentTilePos;
            GenerateTilesAroundPlayer();
        }
    }

    Vector3 GetPlayerTilePos()
    {
        Vector3 pos = player.position;
        return new Vector3(Mathf.Round(pos.x), 0, Mathf.Round(pos.z));
    }

    void GenerateTilesAroundPlayer()
    {
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3 tilePos = lastPlayerTilePos + new Vector3(x, 0, z);

                if (!Physics.CheckBox(tilePos, new Vector3(0.4f, 0.1f, 0.4f)))
                {
                    Instantiate(tilePrefab, tilePos, Quaternion.identity);
                }
            }
        }
    }
}
