using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    [Header("Tile Configuration")]
    public GameObject tilePrefab;
    public float tileSpacing = 1.5f;
    public float activationDistance = 1.2f;
    public int tilesAhead = 8;
    public int tilesKeepBehind = 3;

    [Header("Movement Tracking")]
    public Transform playerCamera;
    public float directionSmoothness = 0.3f;
    public float minMoveDistance = 0.2f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Vector3 lastPlayerPosition;
    private Vector3 smoothedDirection;
    private List<GameObject> activeTiles = new List<GameObject>();
    private HashSet<Vector3> occupiedGrid = new HashSet<Vector3>();

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
            Debug.Log($"[PathManager] Player camera set to: {playerCamera.name}");
        }

        if (playerCamera == null)
        {
            Debug.LogError("[PathManager] No player camera found. Assign manually.");
            return;
        }

        lastPlayerPosition = playerCamera.position;
        smoothedDirection = playerCamera.forward;

        SpawnTilesAround(playerCamera.position);
    }

    void Update()
    {
        if (playerCamera == null) return;

        Vector3 currentPos = playerCamera.position;
        Vector3 delta = currentPos - lastPlayerPosition;

        if (delta.magnitude >= minMoveDistance)
        {
            smoothedDirection = Vector3.Slerp(smoothedDirection, delta.normalized, directionSmoothness);
            SpawnTilesForward(currentPos, smoothedDirection);
            SpawnTilesAround(currentPos);
            lastPlayerPosition = currentPos;
        }

        ActivateNearbyTiles(currentPos);
        CleanupFarTiles(currentPos);

    }

    void SpawnTilesForward(Vector3 position, Vector3 direction)
    {
        for (int i = 1; i <= tilesAhead; i++)
        {
            Vector3 pos = position + direction * i * tileSpacing;
            Vector3 gridPos = ToGrid(pos);

            if (!occupiedGrid.Contains(gridPos))
                SpawnTileAt(gridPos);
        }
    }

    void SpawnTilesAround(Vector3 position)
    {
        Vector3 center = ToGrid(position);
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                Vector3 offset = new Vector3(x * tileSpacing, 0, z * tileSpacing);
                Vector3 tilePos = center + offset;

                if (!occupiedGrid.Contains(tilePos))
                    SpawnTileAt(tilePos);
            }
        }
    }

    void SpawnTileAt(Vector3 position)
    {
        if (tilePrefab == null) return;

        GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
        tile.name = $"Tile_{position.x}_{position.z}";

        activeTiles.Add(tile);
        occupiedGrid.Add(position);
    }

    Vector3 ToGrid(Vector3 pos)
    {
        float gx = Mathf.Round(pos.x / tileSpacing) * tileSpacing;
        float gz = Mathf.Round(pos.z / tileSpacing) * tileSpacing;
        return new Vector3(gx, 0f, gz);
    }

    void ActivateNearbyTiles(Vector3 playerPos)
    {
        Vector3 playerGrid = ToGrid(playerPos);

        foreach (GameObject tile in activeTiles)
        {
            if (!tile) continue;

            Vector3 tileGrid = ToGrid(tile.transform.position);
            float dist = Vector3.Distance(tileGrid, playerGrid);
            PathTile pathTile = tile.GetComponent<PathTile>();

            if (dist <= activationDistance)
            {
                if (pathTile != null && !pathTile.IsActivated)
                    pathTile.ActivateTile();
            }
            else
            {
                if (pathTile != null && pathTile.IsActivated)
                    pathTile.DeactivateTile();
            }
        }
    }

    void CleanupFarTiles(Vector3 playerPos)
    {
        float maxDistance = (tilesAhead + tilesKeepBehind) * tileSpacing;
        List<GameObject> toRemove = new List<GameObject>();

        foreach (GameObject tile in activeTiles)
        {
            if (!tile) continue;

            float dist = Vector3.Distance(playerPos, tile.transform.position);
            if (dist > maxDistance)
                toRemove.Add(tile);
        }

        foreach (GameObject tile in toRemove)
        {
            Vector3 gridPos = ToGrid(tile.transform.position);
            occupiedGrid.Remove(gridPos);
            activeTiles.Remove(tile);
            Destroy(tile);
        }
    }
}
