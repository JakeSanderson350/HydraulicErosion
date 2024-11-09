using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class HydraulicErosion : MonoBehaviour
{
    private bool mIsEroding = false;

    //Terrain data
    private Terrain mTerrain;
    private float[,] mHeights; //indexed as [y, x]
    private int mSideSize;

    //Particle data
    [SerializeField]
    private int mMaxParticles;
    public static int mNumIterations = 100;
    private int mCurrentParticle = 0;

    //Erosion data
    [Range(0f, 1f)]
    public float minSedCapacity = 0.01f;
    [Range(0f, 1f)]
    public float depositionRate = 0.1f;
    [Range(0f, 1f)]
    public float evaporationRate = 0.1f;

    struct Particle
    {
        public Particle(Vector2 _pos)
        {
            mPos = _pos;
            mVelocity = Vector2.zero;
            mSpeed = 1.0f;

            mVolume = 1.0f;
            mSediment = 0.0f;
            mIterations = mNumIterations;
        }

        public Vector2 mPos;
        public Vector2 mVelocity;
        public float mSpeed;

        public float mVolume;
        public float mSediment;
        public float mIterations;
    }

    // Start is called before the first frame update
    void Start()
    {
        mTerrain = GetComponent<Terrain>();
        mSideSize = mTerrain.terrainData.heightmapResolution;
        mHeights = mTerrain.terrainData.GetHeights(0, 0, mSideSize, mSideSize);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("e"))
        {
            mIsEroding = !mIsEroding;
            Debug.Log(mIsEroding ? "Eroding" : "Not Eroding");
        }
    }

    void FixedUpdate()
    {
        if (mCurrentParticle < mMaxParticles && mIsEroding)
        {
            Erode();
            if (mCurrentParticle % 50 == 0) Debug.Log("Particle Num: " +  mCurrentParticle);
            mCurrentParticle++;
        }
        else if (mCurrentParticle == mMaxParticles)
        {
            SmoothTerrainGaussianBlur();
            mCurrentParticle++;
        }
    }

    private void Erode()
    {
        Vector2 startPos = new Vector2(Random.Range(1.0f, (float)mSideSize - 1), Random.Range(1.0f, (float)mSideSize - 1));
        Particle drop = new Particle(startPos);
        HashSet<Vector2> visitedPositions = new HashSet<Vector2>();

        //While there is still water in drop
        while (drop.mIterations > 0)
        {
            Vector2 currentPos = new Vector2(drop.mPos.x, drop.mPos.y);

            if (currentPos.x <= 1 || currentPos.y <= 1 || currentPos.x >= mSideSize - 1 || currentPos.y >= mSideSize - 1) break;

            //Calculate steepest direction
            Vector2 lowestDirection = calcFlowDirection(currentPos);

            if (lowestDirection == Vector2.zero) break;

            //Change particle speed and pos
            drop.mVelocity =lowestDirection;
            drop.mPos += drop.mVelocity * drop.mSpeed;

            if (drop.mPos.x <= 0 || drop.mPos.y <= 0 || drop.mPos.x >= mSideSize || drop.mPos.y >= mSideSize) break;

            //Check if visited already to prevent spikes and pits
            if (visitedPositions.Contains(new Vector2Int((int)drop.mPos.x, (int)drop.mPos.y))) break;
            visitedPositions.Add(new Vector2Int((int)drop.mPos.x, (int)drop.mPos.y));

            //Calculate sediment capacity
            float terrainHeight = mTerrain.terrainData.size.y;
            float oldHeight = calcInterpolatedHeight(currentPos);
            float newHeight = calcInterpolatedHeight(drop.mPos);
            float deltaHeight = oldHeight - newHeight;

            float sedCapacity = Mathf.Max(-deltaHeight * drop.mSpeed * drop.mVolume, minSedCapacity);

            //Change heightmap and drop properties
            if (drop.mSediment < sedCapacity)
            {
                //Erode terrain
                float amountToErode = Mathf.Min(sedCapacity - drop.mSediment, -(deltaHeight));
                if (amountToErode > 0.001f) amountToErode = 0.001f;

                drop.mSediment += amountToErode;
                mHeights[(int)currentPos.y, (int)currentPos.x] -= amountToErode;
            }
            else
            {
                //Desposit sediment
                float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, drop.mSediment) : drop.mSediment - sedCapacity;
                if (amountToDeposit  > 0.001f) amountToDeposit = 0.001f;

                drop.mSediment -= amountToDeposit;
                mHeights[(int)currentPos.y, (int)currentPos.x] += amountToDeposit;
            }

            //Evaporate drop
            drop.mSpeed = Mathf.Sqrt(drop.mSpeed * drop.mSpeed + deltaHeight * 4); //Magic number is "gravity" multiplier
            drop.mVolume *= (1.0f - 1 * (evaporationRate));
            drop.mIterations--;
        }

        mTerrain.terrainData.SetHeights(0, 0, mHeights);
    }
    private Vector2 calcFlowDirection(Vector2 _pos)
    {
        Vector2Int coordPos = new Vector2Int((int)_pos.x, (int)_pos.y);

        if (coordPos.x <= 1 || coordPos.y <= 1 || coordPos.x >= mSideSize - 2 || coordPos.y >= mSideSize - 2) return new Vector2(
            Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));

        //_pos offset within cell 0-1
        float x = _pos.x - coordPos.x;
        float y = _pos.y - coordPos.y;

        //Height of four corners of cell
        float topLeftHeight = mHeights[coordPos.y, coordPos.x];
        float topRightHeight = mHeights[coordPos.y, coordPos.x + 1];
        float bottomLeftHeight = mHeights[coordPos.y + 1, coordPos.x];
        float bottomRightHeight = mHeights[coordPos.y + 1, coordPos.x + 1];

        //Calculate directions using bilinear interpolation
        float directionX = (topRightHeight - topLeftHeight) * (1 - y) + (bottomRightHeight - bottomLeftHeight) * y;
        float directionY = (bottomLeftHeight - topLeftHeight) * (1 - x) + (bottomRightHeight - topRightHeight) * x;

        Vector2 dirVector = new Vector2(directionX, directionY);
        return dirVector.normalized;
    }

    private float calcInterpolatedHeight(Vector2 _pos)
    {
        Vector2Int coordPos = new Vector2Int((int)_pos.x, (int)_pos.y);

        if (coordPos.x <= 1 || coordPos.y <= 1 || coordPos.x >= mSideSize - 2 || coordPos.y >= mSideSize - 2)
        {
            int newX = coordPos.x, newY = coordPos.y;
            //If not gonna be in bounds get height of point next to it
            if (coordPos.x <= 1)
            {
                newX = 2;
            }
            if (coordPos.y <= 1)
            {
                newY = 2;
            }
            if (coordPos.x >= mSideSize - 2)
            {
                newX = mSideSize - 3;
            }
            if (coordPos.y >= mSideSize - 2)
            {
                newY = mSideSize - 3;
            }

            return mHeights[newY, newX];
        }


        //_pos offset within cell 0-1
        float x = _pos.x - coordPos.x;
        float y = _pos.y - coordPos.y;

        //Height of four corners of cell
        float topLeftHeight = mHeights[coordPos.y, coordPos.x];
        float topRightHeight = mHeights[coordPos.y, coordPos.x + 1];
        float bottomLeftHeight = mHeights[coordPos.y + 1, coordPos.x];
        float bottomRightHeight = mHeights[coordPos.y + 1, coordPos.x + 1];

        float height = topLeftHeight * (1 - x) * (1 - y) + topRightHeight * x * (1 - y) + bottomLeftHeight * (1 - x) * y + bottomRightHeight * x * y;

        return height;
    }

    private void SmoothTerrain()
    {
        for (int y = 1; y < mSideSize - 1; y++)
        {
            for (int x = 1; x < mSideSize - 1; x++)
            {
                float avgHeight = 0;

                for (int i = -1; i <= 1; i++)
                {
                    for(int j = -1; j <= 1; j++)
                    {
                        avgHeight += mHeights[y + i, x + j]/* - mHeights[y, x]*/;
                    }
                }

                avgHeight /= 9;
                mHeights[y, x] = avgHeight;
            }
        }

        mTerrain.terrainData.SetHeights(0, 0, mHeights);
        Debug.Log("Smoothed");
    }

    private void SmoothTerrainGaussianBlur()
    {
        List<(int index, float value)> filterKernel = new List<(int, float)>
        {
            (-3, 0.006f), (-2, 0.061f), (-1, 0.242f),
            (0, 0.383f), (1, 0.242f), (2, 0.061f), (3, 0.006f)
        };

        float[,] tmpArray = new float[mSideSize, mSideSize];

        //Filter horizantally
        for (int y = 0; y < mSideSize; y++)
        {
            for (int x = 0; x < mSideSize; x++)
            {
                tmpArray[y, x] = computeXvalue(mHeights, x, y, filterKernel);
            }
        }

        //Filter vertically
        for (int x = 0; x < mSideSize; x++)
        {
            for(int y = 0; y < mSideSize; y++)
            {
                mHeights[y, x] = computeYvalue(tmpArray, x, y, filterKernel);
            }
        }

        mTerrain.terrainData.SetHeights(0, 0, mHeights);
        Debug.Log("Smoothed");
    }

    private float computeXvalue(float[,] _array, int x, int y, List<(int index, float value)> filterKernel)
    {
        int offset = 0;
        float computedValue = 0.0f;

        foreach (var filterPair in filterKernel)
        {
            if (isFilterInBounds(x, mSideSize, filterPair.index))
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

    private float computeYvalue(float[,] _array, int x, int y, List<(int index, float value)> filterKernel)
    {
        int offset = 0;
        float computedValue = 0.0f;

        foreach (var filterPair in filterKernel)
        {
            if (isFilterInBounds(y, mSideSize, filterPair.index))
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