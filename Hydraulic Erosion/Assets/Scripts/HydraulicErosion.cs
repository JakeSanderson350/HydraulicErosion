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
    private float[,] mHeights; //Make sure to setHeights after changing this
    private int mSideSize;

    //Particle data
    [SerializeField]
    private int mMaxParticles;

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
        }

        public Vector2 mPos;
        public Vector2 mVelocity;
        public float mSpeed;

        public float mVolume;
        public float mSediment;
    }

    // Start is called before the first frame update
    void Start()
    {
        mTerrain = GetComponent<Terrain>();
        mSideSize = mTerrain.terrainData.heightmapResolution;
        mHeights = mTerrain.terrainData.GetHeights(0, 0, mSideSize, mSideSize);

        SimulateErosion();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("e"))
        {
            if (mIsEroding)
            {
                mIsEroding = false;
                Debug.Log("Not Eroding");
            }
            else
            {
                mIsEroding = true;
                Debug.Log("Eroding");
            }
        }
    }

    void FixedUpdate()
    {
    }

    private void SimulateErosion()
    {
        for (int i = 0; i < mMaxParticles; i++)
        {
            Erode();
        }
    }

    private void Erode()
    {
        Vector2 startPos = new Vector2(Random.Range(1.0f, (float)mSideSize - 1), Random.Range(1.0f, (float)mSideSize - 1));
        Particle drop = new Particle(startPos);

        //While there is still water in drop
        while (drop.mVolume > 0.1f)
        {
            Vector2 currentPos = new Vector2(drop.mPos.x, drop.mPos.y);

            if (currentPos.x <= 1 || currentPos.y <= 1 || currentPos.x >= mSideSize - 1 || currentPos.y >= mSideSize - 1) break;

            //Calculate steepest direction
            Vector2 lowestDirection = calcFlowDirection(currentPos);
            Debug.Log("Lowest Direction: " + lowestDirection);

            //if (lowestDirection == Vector2.zero) break;

            //Change particle speed and pos
            drop.mVelocity = /*Time.deltaTime * */(lowestDirection); //a = F/m
            drop.mPos += /*Time.deltaTime **/ drop.mVelocity * drop.mSpeed;

            if (drop.mPos.x <= 0 || drop.mPos.y <= 0 || drop.mPos.x >= mSideSize || drop.mPos.y >= mSideSize) break;

            //Calculate sediment capacity
            float oldHeight = calcInterpolatedHeight(currentPos);
            float newHeight = calcInterpolatedHeight(drop.mPos);
            float deltaHeight = oldHeight - newHeight;
            Debug.Log("DeltaH: " + deltaHeight);

            float sedCapacity = Mathf.Max(deltaHeight * drop.mSpeed * drop.mVolume, minSedCapacity);

            //Change heightmap and drop properties
            if (drop.mSediment < sedCapacity)
            {
                //Erode terrain
                float amountToErode = Mathf.Min(sedCapacity - drop.mSediment, -(deltaHeight));

                drop.mSediment += amountToErode;
                mHeights[(int)currentPos.y, (int)currentPos.x] -= amountToErode;
            }
            else
            {
                //Desposit sediment
                float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, drop.mSediment) : drop.mSediment - sedCapacity;
                drop.mSediment -= amountToDeposit;
                mHeights[(int)currentPos.y, (int)currentPos.x] += amountToDeposit;
            }

            //Evaporate drop
            drop.mSpeed = Mathf.Sqrt(drop.mSpeed * drop.mSpeed + deltaHeight * 4); //Magic number is "gravity" multiplier
            drop.mVolume *= (1.0f - 1 * (evaporationRate));
        }

        mTerrain.terrainData.SetHeights(0, 0, mHeights);
    }

    private Vector2 lowestNeighbor(Vector2 _pos)
    {
        Vector2 lowestNeighbor = _pos;

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                //Check against bounds
                if (_pos.x + x < 0 || _pos.y + y < 0 || _pos.x + x >= mSideSize || _pos.y + y >= mSideSize) continue;

                //Check if neighbor is lower than the current lowest
                if ((x != 0 && y != 0) && mHeights[(int)_pos.y + y, (int)_pos.x + x] < mHeights[(int)lowestNeighbor.y, (int)lowestNeighbor.x])
                {
                    Vector2 tmpVec = new Vector2(x, y);
                    lowestNeighbor = tmpVec + _pos;
                }
            }
        }

        return lowestNeighbor;
    }

    private Vector2 calcFlowDirection(Vector2 _pos)
    {
        Vector2Int coordPos = new Vector2Int((int)_pos.x, (int)_pos.y);

        if (coordPos.x <= 1 || coordPos.y <= 1 || coordPos.x >= mSideSize - 2 || coordPos.y >= mSideSize - 2) return Vector2.zero;

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

        if (coordPos.x <= 1 || coordPos.y <= 1 || coordPos.x >= mSideSize - 2 || coordPos.y >= mSideSize - 2) return 0.0f; //Probably need to change this

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
}


