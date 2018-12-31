using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeControl : MonoBehaviour {

    [SerializeField]
    GameObject block;

    Bounds blockBounds;
    public LedgeType ledgeType;
    LedgeType prevLedgeType;

    [System.Serializable]
    public class LedgeSettings
    {
        //The minimum Z length of the ledge, for it to be qualified as a ledge
        public float ledgeQualifierSize;
        //The minimum Z length of the ledge, for it to be qualified as a Plateau
        public float plateauQualifierSize;
    }

    public LedgeSettings ledgeSettings = new LedgeSettings();

    // Use this for initialization
    void Start () {
        blockBounds = block.GetComponent<Renderer>().bounds;
        //Debug.Log("Extents:"+blockBounds.extents);
        //Debug.Log("Size:" + blockBounds.size);
        //Debug.Log("Centre:" + blockBounds.center);

        PositionBlock();

        prevLedgeType = ledgeType;
    }

    // Update is called once per frame
    void Update ()
    {
		//if (prevLedgeType != ledgeType)
        //{
        //    PositionBlock();
        //    prevLedgeType = ledgeType;
        //}
	}

    void PositionBlock ()
    {
        //This script calculates the retracted position, ledgeable position, plateaubale position of the attached block
        //The node will be on the surface of the hill/wall, with Z pointing out. Use a placement tool to place it there

        //Get the X,Y,Z bounds/dimension of the block

        float x, y, z;

        block.transform.position = transform.position;


        //The top of the block should always be right were the node is
        y = block.transform.localPosition.y - blockBounds.extents.y;

        //Start with Retracted position
        z = block.transform.localPosition.z - blockBounds.extents.z;

        //Debug.Log(block.transform.localPosition.z + "," + blockBounds.extents.z);

        if (ledgeType == LedgeType.Ledge)
        {
            //Ledgeable position
            z = z + Random.Range(ledgeSettings.ledgeQualifierSize, ledgeSettings.plateauQualifierSize);
        }
        else if (ledgeType == LedgeType.Plateau)
        {
            //Plateauable position
            z = z + Random.Range(ledgeSettings.plateauQualifierSize, blockBounds.extents.z);
        }



        block.transform.localPosition = new Vector3(block.transform.localPosition.x, y, z);
    }
}
