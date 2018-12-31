using UnityEngine;
using System.Collections;

public class SettingClasses : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

public struct LedgeDetails
{
    public bool ledgeFound;
    public LedgeType ledgeType;
    public Vector3 ledgeLocation;
    public Quaternion targetRotation;
    public GameObject ledgeObject;

    public LedgeDetails(bool p_found, LedgeType p_type, Vector3 p_location, Quaternion p_rotation, GameObject p_object)
    {
        ledgeFound = p_found;
        ledgeType = p_type;
        ledgeLocation = p_location;
        targetRotation = p_rotation;
        ledgeObject = p_object;
    }
}

public struct EdgePoint
{
    public int index;
    public RaycastHit hit;
}















