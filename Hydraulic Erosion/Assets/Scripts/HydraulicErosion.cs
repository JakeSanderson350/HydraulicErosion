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
    private int mMaxParticles = 1000;
    private int currentParticle = 0;

    //Erosion data
    float depositionRate = 0.1f;
    float evaporationRate = 0.001f;

    struct Particle
    {
        public Particle(Vector2 _pos)
        {
            mPos = _pos;
            mSpeed = Vector2.zero;

            mVolume = 1.0f;
            mSediment = 0.0f;
        }

        public Vector2 mPos;
        public Vector2 mSpeed;

        public float mVolume;
        public float mSediment;
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
        if (currentParticle < mMaxParticles && mIsEroding)
        {
            Erode();
            currentParticle++;
        }
    }

    private void Erode()
    {
        Vector2 startPos = new Vector2(Random.Range(1.0f, (float)mSideSize - 1), Random.Range(1.0f, (float)mSideSize - 1));
        Particle drop = new Particle(startPos);

        //While there is still water in drop
        while (drop.mVolume > 0.01f)
        {
            //Get floored position and check if in bounds
            Vector2 currentPos = new Vector2(Mathf.FloorToInt(drop.mPos.x), Mathf.FloorToInt(drop.mPos.y));

            if (currentPos.x <= 0 || currentPos.y <= 0 || currentPos.x >= mSideSize || currentPos.y >= mSideSize) break;

            //Check for lowest neighbor
            Vector2 nextLowest = lowestNeighbor(currentPos);
            Vector2 lowestDirection = nextLowest - currentPos;

            //Change particle speed and pos
            drop.mSpeed += Time.deltaTime * (lowestDirection / drop.mVolume); //a = F/m
            drop.mPos += Time.deltaTime * drop.mSpeed;

            if (currentPos.x <= 0 || currentPos.y <= 0 || currentPos.x >= mSideSize || currentPos.y >= mSideSize) break;

            //Calculate sediment capacity
            float maxSed = drop.mVolume * drop.mSpeed.magnitude * (mHeights[(int)currentPos.y, (int)currentPos.x] - mHeights[(int)drop.mPos.y, (int)drop.mPos.x]);
            if (maxSed < 0.0f)
            {
                maxSed = 0.0f;
            }

            float sedDiff = maxSed - drop.mSediment;

            //Change heightmap and drop properties
            drop.mSediment += Time.deltaTime * sedDiff * depositionRate;
            mHeights[(int)currentPos.y, (int)currentPos.x] -= Time.deltaTime * drop.mVolume * sedDiff * depositionRate;
            Debug.Log("Changed (" + (int)currentPos.x + ", " + (int)currentPos.y + ") by " + (Time.deltaTime * drop.mVolume * sedDiff * depositionRate));

            //Evaporate drop
            drop.mVolume *= (1.0f - Time.deltaTime * evaporationRate);
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
}
