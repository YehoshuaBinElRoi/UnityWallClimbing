using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossSectionKnife : MonoBehaviour {

    public bool triggerEnabled;
    public bool processingComplete;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    /*private void OnTriggerEnter(Collider p_collider)
    {
        if (triggerEnabled)
        {
            Debug.Log("Triggered");
            triggerEnabled = false;
            gameObject.GetComponent<MeshCollider>().isTrigger = false;
            processingComplete = true;
        }
    }*/

    void OnCollisionEnter(Collision p_collision)
    {
        if (triggerEnabled)
        {
            Debug.Log("Triggered");
            triggerEnabled = false;
            //gameObject.GetComponent<MeshCollider>().isTrigger = false;
            processingComplete = true;

            foreach (ContactPoint contact in p_collision.contacts)
            {
                Debug.Log("Contact");
                Debug.DrawRay(contact.point, contact.normal * 10.0f, Color.red,3.0f);
            }

        }
    }
}
