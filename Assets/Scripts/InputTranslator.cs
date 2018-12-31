using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputTranslator : MonoBehaviour {

    [System.Serializable]
    public class InputMapping
    {
        public string Jump = "Space";
        public string DropOffLedge = "LeftControl";
        public string frontBackMove = "Vertical";
        public string rightLeftMove = "Horizontal";
        public string horizontalRotate = "Mouse X";
        public string verticalRotate = "Mouse Y";
    }

    [SerializeField]
    InputMapping inputMap = new InputMapping();

    public bool jump_Pressed;
    public bool dropOffLedge_Pressed;
    public float frontBackMovement;
    public float rightLeftMovement;
    public float horizontalRotation;
    public float verticalRotation;
    public Vector2 mousePos;

    KeyCode kc_Jump;
    KeyCode kc_DropOffLedge;
    //KeyCode kc_Up;
    //KeyCode kc_Left;
    //KeyCode kc_Right;
    //KeyCode kc_Down;
    
        

    // Use this for initialization
    void Start ()
    {
        //Debug.Log(inputMap.Jump);
        kc_Jump = (KeyCode)System.Enum.Parse(typeof(KeyCode), inputMap.Jump);
        kc_DropOffLedge = (KeyCode)System.Enum.Parse(typeof(KeyCode), inputMap.DropOffLedge);
        //kc_Up = (KeyCode)System.Enum.Parse(typeof(KeyCode),inputMap.Up);
        //kc_Left = (KeyCode)System.Enum.Parse(typeof(KeyCode), inputMap.Left);
        //kc_Right = (KeyCode)System.Enum.Parse(typeof(KeyCode), inputMap.Right);
        //kc_Down = (KeyCode)System.Enum.Parse(typeof(KeyCode), inputMap.Down);


    }

    // Update is called once per frame
    void Update ()
    {
		jump_Pressed = Input.GetKeyDown(kc_Jump);
        dropOffLedge_Pressed = Input.GetKeyDown(kc_DropOffLedge);
        //upPressed = Input.GetKey(kc_Up);//upValue = Input.GetAxisRaw(kc_Up.ToString());
        //downPressed = Input.GetKey(kc_Down); //downValue = downPressed ? -1.0f : 0.0f;
        //leftPressed = Input.GetKey(kc_Left); //leftValue = leftPressed ? 1.0f : 0.0f;
        //rightPressed = Input.GetKey(kc_Right); //rightValue = rightPressed ? -1.0f : 0.0f;
        rightLeftMovement = Input.GetAxisRaw(inputMap.rightLeftMove);
        frontBackMovement = Input.GetAxisRaw(inputMap.frontBackMove);
        horizontalRotation = Input.GetAxisRaw(inputMap.horizontalRotate);
        verticalRotation = Input.GetAxisRaw(inputMap.verticalRotate);

        mousePos = Input.mousePosition;
        mousePos.x -= Screen.width / 2;
        mousePos.y -= Screen.height / 2;

        //Debug.Log(mousePos);

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (Time.timeScale == 0.0f)
            {
                Time.timeScale = 1.0f;
            }
            else
            {
                Time.timeScale = 0.0f;
            }
        }
    }
}
