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
    private int mSideSize;

    //Particle data
    [SerializeField]
    private int mMaxParticles;
    private int currentParticle = 0;

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
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("e"))
        {
            if (mIsEroding)
            {
                mIsEroding = false;
            }
            else
            {
                mIsEroding = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (currentParticle < mMaxParticles)
        {
            Erode();
            currentParticle++;
        }
    }

    private void Erode()
    {
        Vector2 startPos = new Vector2(Random.Range(1.0f, (float)mSideSize), Random.Range(1.0f, (float)mSideSize));
        Particle drop = new Particle(startPos);

        //While there is still water in drop
        while (drop.mVolume > 0.01f)
        {
            //Get floored position and check if in bounds
            Vector2 currentPos = new Vector2(Mathf.FloorToInt(startPos.x), Mathf.FloorToInt(startPos.y));

            if (currentPos.x <= 0 || currentPos.y <= 0 || currentPos.x >= mSideSize || currentPos.y >= mSideSize) break;

            //Check for lowest neighbor


            //Change particle speed and pos


            //Calculate sediment capacity


            //Change heightmap and drop properties
        }
    }

    private Vector2 lowestNeighbor(Vector2 _pos)
    {
        Vector2 lowestNeighbor = Vector2.zero;
        float[,] heights = new float[mSideSize, mSideSize];
        //mTerrain.terrainData.

        return lowestNeighbor;
    }
}
