using UnityEngine;
using System.Collections;

public class CharControl3rdPerson : MonoBehaviour
{
    
    [System.Serializable]
    public class MoveSettings
    {
        public float fwdVelocity = 6.0f;
        public float rotVelocity = 100.0f;
        public float jumpVel = 7.5f;
        public float distToGround = 0.1f;
        public Vector3 groundScanOffset = new Vector3(0, 0.1f, 0);
        public LayerMask ground;

    }

    [System.Serializable]
    public class LedgeMoveSettings
    {
        public float jumpBackFromLedgeVel = 1.0f;
        //Ledge Scan will start once the character has this % of upward velocity left
        //Example: If the jump speed is 10 & this variable value is 10
        //Ledgescan window will open once the speed reaches 1 (10% of speed is left)
        //Example: If the jump speed is 50 & this variable value is 90
        //Ledgescan window will open once the speed reaches 45 (i.e., 90% of speed is left)
        public float ledgeScanPer;
    }

    [System.Serializable]
    public class PhysSettings
    {
        public float downAccel = 0.75f;
    }

    [System.Serializable]
    public class InputSettings
    {
        public float inputDelay = 0.1f; //Deadzone for key press
        public string FORWARD_AXIS = "Vertical";
        public string TURN_AXIS = "Horizontal";
        public string JUMP_AXIS = "Jump";
        public string CROUCH_DROP = "Crouch";
    }

    public MoveSettings moveSetting = new MoveSettings();
    public PhysSettings physSetting = new PhysSettings();
    public InputSettings inputSetting = new InputSettings();
    public LedgeMoveSettings ledgeSetting = new LedgeMoveSettings();

    public bool charGrounded = false;
    public Rigidbody RB;

    public bool blockCharCtrl = false; //Block the character controller, as movement is handled by wall climbing etc.,

    RaycastHit groundHit;
    Quaternion targetRotation;
    float fwdInput;
    float turnInput;
    float jumpInput;
    bool jumping;
    bool ledgeScanWindow;
    Vector3 velocity = Vector3.zero;
    Vector3 initialVelocity;


    // Use this for initialization
    void Start ()
    {
        targetRotation = transform.rotation;

        if (GetComponent<Rigidbody>())
        {
            RB = GetComponent<Rigidbody>();
        }
        else
        {
            Debug.LogError("No rigid body attached");
        }

        fwdInput = turnInput = 0;


    }

    // Update is called once per frame
    void Update ()
    {
        if (blockCharCtrl)
        {
            return;
        }


        GetInput();
        Turn();
    }

    void FixedUpdate()
    {
        if (blockCharCtrl)
        {
            return;
        }

        Grounded();
        Run();
        Jump();

        RB.velocity = transform.TransformDirection(velocity);

        //Debug.Log(RB.velocity);    
    }

    void GetInput()
    {
        fwdInput = Input.GetAxis(inputSetting.FORWARD_AXIS);
        turnInput = Input.GetAxis(inputSetting.TURN_AXIS);
        jumpInput = Input.GetAxisRaw(inputSetting.JUMP_AXIS);

    }

    void Turn()
    {
        if (Mathf.Abs(turnInput) > inputSetting.inputDelay)
        {
            targetRotation *= Quaternion.AngleAxis(moveSetting.rotVelocity * turnInput * Time.deltaTime, Vector3.up);
            transform.rotation = targetRotation;
        }
    }

    void Run()
    {
        if (charGrounded)
        {
            if (Mathf.Abs(fwdInput) > inputSetting.inputDelay)
            {
                velocity.z = moveSetting.fwdVelocity * fwdInput;
            }
            else
            {
                velocity.z = 0;
            }
        }
        else
        {
            //While jumping reduce the forward velocity gradually
            if (velocity.z < 0.0f)
            {
                velocity.z += 0.1f;
                if (velocity.z > 0.0f)
                {
                    velocity.z = 0.0f;
                }
            }
            else if (velocity.z > 0.0f)
            {
                velocity.z -= 0.1f;
                if (velocity.z < 0.0f)
                {
                    velocity.z = 0.0f;
                }
            }
        }
    }

    void Jump()
    {
        if (jumpInput > 0 && charGrounded)
        {
            //If jump button is pressed
            //And player is on ground
            {
                velocity.y = moveSetting.jumpVel;
                initialVelocity = velocity;
            }
            {
                //Use a different jump velocity if backwardLedgeJump
            }
            jumping = true;
        }
        else if (jumpInput == 0 && charGrounded)
        {
            //If a jump input is not received 
            //And the player is on ground
            velocity.y = 0;

            if (jumping)
            {
                //Reset the jumping flag is the character has hit the ground
                jumping = false;
            }

        }
        else
        {
            //If the player is not on ground
            //Decelerate irrespective of jump input
            //if (/*velocity.y > 0.0f*/!charGrounded)
            {
                velocity.y -= physSetting.downAccel;

                //When player just fell off the ledge
                //When jump button is pressed
                //There will be no need to JUMP Again
                //But a ledge scan must start
                if (!jumping && jumpInput > 0.0f)
                {
                    //falling = true;
                    jumping = true;
                    //initialVelocity = velocity;
                }
            }
            //if ()
            {
                //Use a difference deceleration if backwardLedgeJump
            }
        }
        

    }

    public bool CheckLedgeScanWindow()
    {
        //There are a few conditions to be checked before starting a ledge scan

        //The player should not be on the ground
        if (charGrounded)
        {
            return false;
        }

        //The player should have lost a certain percentage of initial jump velocity
        if (RB.velocity.y <= initialVelocity.y * ledgeSetting.ledgeScanPer / 100.0f)
        {
            //Jump button must have been pressed
            if (jumping)
            {
                //Debug.Log(RB.velocity.y+":"+ initialVelocity.y);
                return true;
            }
        }

        return false;
    }

    public bool Grounded()
    {
        ///Cast the ray from slightly above the character BASE
        ///Because sometimes,the character will be right on the ground and the raycast will hit nothing, making the program assume it is not grounded

        //Also, when the ray distance is exactly same as the offset
        //The ground will not be detected if the player is on ground
        //So, we extend the ray slightly by 0.05 units



        if (Physics.Raycast(transform.position + moveSetting.groundScanOffset,
                                        Vector3.down,
                                        out groundHit,
                                        moveSetting.distToGround + 0.05f,
                                        moveSetting.ground))
        {
            if (transform.position.y - groundHit.point.y <= moveSetting.distToGround)
            {
                //But player is considered to be on ground
                //Only if the ground is at the correct distance 
                charGrounded = true;
            }

            else
            {
                charGrounded = false;
            }
        }
        else
        {
            charGrounded = false;
        }

        return charGrounded;
    }

    public void BlockCharControl ()
    {
        //When the wall climbing script has identified a ledge
        //The normal 3rd person movement logic does not apply
        blockCharCtrl = true;
        RB.isKinematic = true;
        RB.velocity = Vector3.zero;
        velocity = Vector3.zero;
        jumping = false;
        jumpInput = 0.0f;

        //Debug.Log("Blocking Target Rotation:"+ targetRotation.eulerAngles);
    }

    public void UnblockCharControl ()
    {
        blockCharCtrl = false;
        RB.isKinematic = false;
        velocity = Vector3.zero;
        jumping = false; //Will be set by other logic/methods down the line

        
        targetRotation = transform.rotation;

        //Debug.Log("Unblocking Target Rotation:" + targetRotation.eulerAngles);

    }

    public void InitiateJump ()
    {
        velocity.y = moveSetting.jumpVel;
        initialVelocity = velocity;
        //</TBD>
        jumping = true;
        //<TBD/>
    }

    public void InitiateBackwardJump (float p_YVelocity, float p_ZVelocity)
    {
        velocity.y = p_YVelocity;
        velocity.z = p_ZVelocity;
        initialVelocity = velocity;
        //</TBD>
        jumping = true;
        //<TBD/>
    }
}
