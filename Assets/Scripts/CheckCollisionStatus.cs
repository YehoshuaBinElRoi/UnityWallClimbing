using UnityEngine;
using System.Collections;

public class CheckCollisionStatus : MonoBehaviour
{
    public bool objectTangled;

    LayerMask ledgeLayers;


	// Use this for initialization
	void Start ()
    {
        ledgeLayers = GetComponentInParent<WallClimbing>().ledgeLayers;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider collider)
    {
        //if (collider.gameObject.layer == ledgeLayers)
        //if (ledgeLayers == (ledgeLayers | (1 << collider.gameObject.layer)))
        {
            //When the hand is inside any of the /*ledge*/ object
            //There should be no scan for ledge
            //Debug.Log(gameObject.name + " inside another");
            objectTangled = true;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        //if (ledgeLayers == (ledgeLayers | (1 << collider.gameObject.layer)))
        {
           objectTangled = false;
        }
    }

}

