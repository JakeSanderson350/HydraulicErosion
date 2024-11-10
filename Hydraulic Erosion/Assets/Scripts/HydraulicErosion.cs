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
    private int mOctaves = 1;
    private float[,] mHeights; //indexed as [y, x]
    private float[,] mUnsmoothedHeights; //indexed as [y, x]
    private int mSideSize;

    //Erosion data
    [Range(0f, 1f)]
    public float minSedCapacity = 0.01f;

    //Smoothing data
    private bool mHasSmoothing = false;
    TerrainSmoothing.SmoothingType mSmoothType = TerrainSmoothing.SmoothingType.NoSmooth;

    //Particle data
    [SerializeField]
    private int mMaxParticles;
    public static int mNumIterations = 500;
    private int mCurrentParticle = 0;

    struct Particle
    {
        public Particle(Vector2 _pos)
        {
            mPos = _pos;
            mVelocity = Vector2.zero;
            mSpeed = 1.0f;

            mSediment = 0.0f;
            mIterations = mNumIterations;
        }

        public Vector2 mPos;
        public Vector2 mVelocity;
        public float mSpeed;

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

    void FixedUpdate()
    {
        if (mCurrentParticle < mMaxParticles && mIsEroding)
        {
            Erode();
            if (mCurrentParticle % 50 == 0) Debug.Log("Particle Num: " +  mCurrentParticle);
            mCurrentParticle++;
        }
        else if (mCurrentParticle == mMaxParticles) //Done eroding
        {
            Debug.Log("Erosion finished");
            mIsEroding = false;
            mUnsmoothedHeights = mHeights;
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

            float sedCapacity = Mathf.Max(-deltaHeight * drop.mSpeed, minSedCapacity);

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

    private void OnGUI()
    {
        //Background Box
        GUI.Box(new Rect(10, 10, 150, 200), "Erosion Menu");

        //Make new terrain
        if (GUI.Button(new Rect(10, 30, 110, 20), "Generate Terrain"))
        {
            mHeights = TerrainGeneration.GenerateTerrain(mTerrain, Random.Range(-1000, 1000), mOctaves);
            mTerrain.terrainData.SetHeights(0, 0, mHeights);
        }

        mOctaves = (int)GUI.HorizontalSlider(new Rect(10, 80, 110, 20), mOctaves, 1, 10);
        GUI.Box(new Rect(10, 60, 130, 20), "Octaves: " + mOctaves);

        //Change number of particles to simulate
        if (!mIsEroding) //Only show up when erosion isn't happening
        {
            mMaxParticles = (int)GUI.HorizontalSlider(new Rect(10, 110, 110, 20), mMaxParticles, 0, 1000);
            GUI.Box(new Rect(10, 90, 130, 20), "Max Particles: " + mMaxParticles);
        }

        //Select smoothing method
        if (mCurrentParticle > mMaxParticles) //Only show up when erosion is complete
        {
            mHasSmoothing = GUI.Toggle(new Rect(10, 180, 130, 20), mHasSmoothing, "Toggle Smoothing");
            if (mHasSmoothing)
            {
                if (GUI.Button(new Rect(10, 200, 70, 20), "Averages"))
                {
                    mSmoothType = TerrainSmoothing.SmoothingType.AverageHeights;

                    mHeights = TerrainSmoothing.SmoothTerrain(mUnsmoothedHeights, mSmoothType);
                    mTerrain.terrainData.SetHeights(0, 0, mHeights);
                }
                if (GUI.Button(new Rect(90, 200, 70, 20), "Gaussian"))
                {
                    mSmoothType = TerrainSmoothing.SmoothingType.GaussianBlur;

                    mHeights = TerrainSmoothing.SmoothTerrain(mUnsmoothedHeights, mSmoothType);
                    mTerrain.terrainData.SetHeights(0, 0, mHeights);
                }
            }
            else
            {
                mSmoothType = TerrainSmoothing.SmoothingType.NoSmooth;

                mHeights = mUnsmoothedHeights;
                mTerrain.terrainData.SetHeights(0, 0, mHeights);
            }
        }

        //Start/pause/reset erosion
        if (GUI.Button(new Rect(10, 130, 130, 20), "Toggle Erosion"))
        {
            mIsEroding = !mIsEroding;
            Debug.Log(mIsEroding ? "Eroding" : "Not Eroding");
        }
        if (GUI.Button(new Rect(10, 150, 130, 20), "Reset Erosion"))
        {
            mHeights = TerrainGeneration.GenerateTerrain(mTerrain, Random.Range(-1000, 1000), mOctaves);
            mTerrain.terrainData.SetHeights(0, 0, mHeights);
            mCurrentParticle = 0;
            mIsEroding = false;
        }
    }
}