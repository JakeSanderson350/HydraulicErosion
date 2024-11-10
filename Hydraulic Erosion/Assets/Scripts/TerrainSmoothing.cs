using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainSmoothing
{
    public enum SmoothingType
    {
        NoSmooth,
        AverageHeights,
        GaussianBlur
    }

    public static float[,] SmoothTerrain(float[,] _heights, SmoothingType _smoothType)
    {
        if (_smoothType == SmoothingType.AverageHeights)
        {
            return SmoothTerrainAverageHeights(_heights);
        }
        else if (_smoothType == SmoothingType.GaussianBlur)
        {
            return SmoothTerrainGaussianBlur(_heights);
        }
        else
        {
            return _heights;
        }
    }

    //Average heights smoothing
    private static float[,] SmoothTerrainAverageHeights(float[,] _heights)
    {
        int sideSize = _heights.GetLength(0);
        float[,] newHeights = new float[sideSize, sideSize];

        for (int y = 1; y < sideSize - 1; y++)
        {
            for (int x = 1; x < sideSize - 1; x++)
            {
                float avgHeight = 0;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        avgHeight += _heights[y + i, x + j];
                    }
                }

                avgHeight /= 9;
                newHeights[y, x] = avgHeight;
            }
        }

        Debug.Log("Avg Smoothed");
        return newHeights;
    }

    //Gaussion Blur smoothing

    private static List<(int index, float value)> filterKernel = new List<(int, float)>
        {
            (-3, 0.006f), (-2, 0.061f), (-1, 0.242f),
            (0, 0.383f), (1, 0.242f), (2, 0.061f), (3, 0.006f)
        };

    private static float[,] SmoothTerrainGaussianBlur(float[,] _heights)
    {
        int sideSize = _heights.GetLength(0);
        float[,] tmpArray = new float[sideSize, sideSize];
        float[,] newArray = new float[sideSize, sideSize];

        //Filter horizantally
        for (int y = 0; y < sideSize; y++)
        {
            for (int x = 0; x < sideSize; x++)
            {
                tmpArray[y, x] = computeXvalue(_heights, sideSize, x, y, filterKernel);
            }
        }

        //Filter vertically
        for (int x = 0; x < sideSize; x++)
        {
            for (int y = 0; y < sideSize; y++)
            {
                newArray[y, x] = computeYvalue(tmpArray, sideSize, x, y, filterKernel);
            }
        }

        Debug.Log("Gauss Smoothed");
        return newArray;
    }

    private static float computeXvalue(float[,] _array, int _size, int x, int y, List<(int index, float value)> filterKernel)
    {
        int offset = 0;
        float computedValue = 0.0f;

        foreach (var filterPair in filterKernel)
        {
            if (isFilterInBounds(x, _size, filterPair.index))
            {
                offset = 0;
            }
            else
            {
                offset = filterPair.index;
            }

            computedValue += filterPair.value * _array[y, x + offset];
        }

        return computedValue;
    }

    private static float computeYvalue(float[,] _array, int _size, int x, int y, List<(int index, float value)> filterKernel)
    {
        int offset = 0;
        float computedValue = 0.0f;

        foreach (var filterPair in filterKernel)
        {
            if (isFilterInBounds(y, _size, filterPair.index))
            {
                offset = 0;
            }
            else
            {
                offset = filterPair.index;
            }

            computedValue += filterPair.value * _array[y + offset, x];
        }

        return computedValue;
    }

    private static bool isFilterInBounds(int _index, int _size, int _offset)
    {
        return _index + _offset >= _size || _index + _offset <= 0;
    }
}
