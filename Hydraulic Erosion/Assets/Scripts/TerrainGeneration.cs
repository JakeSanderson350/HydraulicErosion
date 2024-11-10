using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainGeneration : MonoBehaviour
{
    public static float[,] GenerateTerrain(Terrain _terrain, int _seed, int _octaves)
    {
        int size = _terrain.terrainData.heightmapResolution;
        float[,] heights = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)

            {
                float value = 0;

                //noise octaves
                for (int octave = 1; octave <= Mathf.Pow(2, _octaves); octave *= 2)
                {
                    value += (1.0f / octave) + Mathf.PerlinNoise((x + _seed) * ((float)octave / (size / 2)), (y + _seed) * ((float)octave / (size / 2)));
                }

                //island behavior
                Vector2 center = new Vector2(size / 2, size / 2);
                Vector2 pos = new Vector2(x, y);
                value -= 1 * (center - pos).magnitude / size;

                heights[x, y] = value / 8;
            }
        }

        return heights;
    }
}
