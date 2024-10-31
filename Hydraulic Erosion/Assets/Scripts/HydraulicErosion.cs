using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HydraulicErosion : MonoBehaviour
{
    public bool mIsEroding = false;

    private Terrain mTerrain;

    [SerializeField]
    public int maxParticles;
    public int currentParticle = 0;

    // Start is called before the first frame update
    void Start()
    {
        mTerrain = GetComponent<Terrain>();
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
        if (currentParticle < maxParticles)
        {
            Erode();
        }
    }

    private void Erode()
    {

    }
}
