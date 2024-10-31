using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();

        TerrainData data = terrain.terrainData;
        int size = data.heightmapResolution;
        float[,] heights = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)

            {
                float value = 0;

                //noise octaves
                for (int octave = 1; octave <= 2; octave *= 2)
                {
                    value += (1.0f / octave) + Mathf.PerlinNoise(x * ((float)octave / (size / 2)), y * ((float)octave / (size / 2)));
                }

                //island behavior
                Vector2 center = new Vector2(size / 2, size / 2);
                Vector2 pos = new Vector2(x, y);
                value -= 1 * (center - pos).magnitude / size;

                heights[x, y] = value / 8;
            }
        }

        terrain.terrainData.SetHeights(0, 0, heights);
        //terrain.terrainData.UpdateDirtyRegion(0, 0, size, size, true);

        RectInt region = new RectInt(0, 0, size, size);
        terrain.terrainData.DirtyHeightmapRegion(region, TerrainHeightmapSyncControl.HeightAndLod);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        
    }
}
