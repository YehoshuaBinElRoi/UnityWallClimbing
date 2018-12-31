using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ELRoi_CamController : MonoBehaviour
{
    [SerializeField]
    InputTranslator inputTranslator;
    [SerializeField]
    GameObject lookTarget;

    float xRotation = 0.0f;
    float yRotation = 0.0f;

    public float angleBetweenPlayerAndCamera;
    Vector3 playerForward;
    Vector3 cameraForward;

    // Use this for initialization
    void Start () {
        //transform.position = lookTarget.transform.position + (lookTarget.transform.forward * -2.0f);
    }
	
	// Update is called once per frame
	void LateUpdate ()
    {
        /*transform.LookAt(lookTarget.transform);

        //Spin the camera around the character, as per the mouse input
        if (inputTranslator.horizontalRotation != 0.0f)
        {
            transform.RotateAround(lookTarget.gameObject.transform.position, Vector3.up, -inputTranslator.horizontalRotation);
        }
        if (inputTranslator.verticalRotation != 0.0f)
        {
            transform.RotateAround(lookTarget.gameObject.transform.position,
                                    transform.right,//transform.TransformDirection(Vector3.right), 
                                    -inputTranslator.verticalRotation);
        }*/

        xRotation += inputTranslator.verticalRotation;Mathf.Clamp(xRotation,-90.0f,90.0f);
        //Using '-' to prevent axis invert
        yRotation -= inputTranslator.horizontalRotation; Mathf.Clamp(yRotation, -90.0f, 90.0f);

        Quaternion rotation = Quaternion.Euler(xRotation, yRotation,0.0f);
        Vector3 distance = new Vector3(0.0f,0.0f,-2.0f);
        Vector3 position = rotation * distance + lookTarget.transform.position;

        transform.position = position;
        transform.rotation = rotation;

        //transform.position = new Vector3(transform.position.x, 1.5f, transform.position.z);

        //Place the camera few units behind the character
        //transform.position += transform.rotation *
        //                         lookTarget.transform.forward *
        //                             2.0f;

        playerForward = lookTarget.transform.forward;
        playerForward.y = 0;

        cameraForward = transform.forward;
        cameraForward.y = 0;
        angleBetweenPlayerAndCamera = Vector3.SignedAngle(playerForward, cameraForward, Vector3.up);
        //Debug.Log(angleBetweenPlayerAndCamera);

    }
}
