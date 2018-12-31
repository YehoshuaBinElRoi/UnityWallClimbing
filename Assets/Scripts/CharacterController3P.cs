using UnityEngine;
using System.Collections;

public class CharacterController3P : MonoBehaviour
{


    /// <summary>
    public bool grounded;
    public bool ledgeScanWindow;
    public float ledgeMoveWindow_RL;//Allow movement to the left or the right while on ledge
    public float ledgeMoveWindow_UD;//Allow movement to and upper ledge while hanging from a plateau
    public Vector3 ledgeJumpWindow_RL;
    public bool freezeMovement = false; //When the character is climbing to a ledge, do not let the controller impact the movement
    public bool freezeRegJump = false;
    public bool freezeLedgeJump = false;
    public bool autoJumpMode = false; //If player is hanging from plateau no need to jump, just move to plateau
    public bool freezeLedgeMove = false;
    public bool ledgeJumping = false;
    public bool backwardLedgeJumping = false;
    public bool ledgeMode = false;
    public bool jumpInputReceived = false;
    public bool dropDown = false;
    public bool plateauMoveInitiated = false;
    public float initialVelocity;
    public CameraController CC;

    float screenCentreX;
    float screenCentreY;

    public string latestJumpInputKey;

    /// </summary>

    [System.Serializable]
    public class MoveSettings
    {
        public float fwdVelocity = 6.0f;
        public float rotVelocity = 100.0f;
        public float jumpVel = 7.5f;
        public float jumpBackFromLedgeVel = 1.0f;
        public float jumpSideFromLedgeVel = 1.0f;
        public float distToGround = 0.1f;
        public Vector3 groundScanOffset = new Vector3(0, 0.1f, 0);
        public LayerMask ground;
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


    public Vector3 velocity = Vector3.zero;
    Quaternion targetRotation;
    Rigidbody RB;

    float fwdInput;
    float turnInput;
    float jumpInput;
    float deadZone = 50.0f;

    RaycastHit groundHit;

    public Quaternion TargetRotation
    {
        get { return targetRotation; }
    }

    public bool Grounded()
    {
        ///Cast the ray from slightly above the character BASE
        ///Because sometimes,the character will be right on the ground and the raycast will hit nothing, making the program assume it is not grounded
        return Physics.Raycast(transform.position + moveSetting.groundScanOffset, Vector3.down, out groundHit, moveSetting.distToGround, moveSetting.ground);
    }

    // Use this for initialization
    void Start()
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

        screenCentreX = Screen.width / 2;
        screenCentreY = Screen.height / 2;
    }

    void GetInput()
    {
        fwdInput = Input.GetAxis(inputSetting.FORWARD_AXIS);
        turnInput = Input.GetAxis(inputSetting.TURN_AXIS);
        jumpInput = Input.GetAxisRaw(inputSetting.JUMP_AXIS);
        if (ledgeMode)
        {
            if (Input.GetButtonDown(inputSetting.JUMP_AXIS))
            {
                jumpInputReceived = true;
            }

        }
        if (Input.GetButtonDown(inputSetting.FORWARD_AXIS))
        {
            latestJumpInputKey = inputSetting.FORWARD_AXIS;
        }
        else if (Input.GetButtonDown(inputSetting.JUMP_AXIS))
        {
            latestJumpInputKey = inputSetting.JUMP_AXIS;
        }

        //If CROUCH button is pressed while on ledge, drop down
        if (Input.GetButtonDown(inputSetting.CROUCH_DROP) && ledgeMode)
        {
            dropDown = true;
            latestJumpInputKey = inputSetting.CROUCH_DROP;

        }

    }

    // Update is called once per frame
    void Update()
    {
        GetInput();
        Turn();
        //GetPointingDirection();

    }

    void FixedUpdate()
    {

        Run();
        Jump();

        RB.velocity = transform.TransformDirection(velocity);
        //RB.velocity = velocity;
        CheckLedgeScanWindow();


    }


    void CheckLedgeScanWindow()
    {
        ///Cast the ray from slightly above the character BASE
        ///Because sometimes,the character will be right on the ground and the raycast will hit nothing, making the program assume it is not grounded
        if (!ledgeMode)
        {
            //Initiate a ledge scan only when player pressed SPACE
            grounded = Physics.Raycast(transform.position + moveSetting.groundScanOffset,
                Vector3.down,
                out groundHit,
                moveSetting.distToGround,
                moveSetting.ground);
            if (!grounded)
            {
                //Once the player has lost a certain % of velocity while jumping
                //We can start scanning for ledges
                //Debug.Log(initialVelocity);
                if (RB.velocity.y <= /*moveSetting.jumpVel*/initialVelocity * moveSetting.ledgeScanPer / 100.0f)
                {
                    //Do not initiate a ledge scan, just because the player is in the air
                    //Player might have ended up off the ledge because the player pressed the Drop from ledge button
                    //Or When player walks off the ledge
                    if (latestJumpInputKey != inputSetting.FORWARD_AXIS &&
                        latestJumpInputKey != inputSetting.CROUCH_DROP)
                    {
                        //Debug.Log("Start Ledge Scan");
                        ledgeScanWindow = true;
                    }
                }
                else
                {
                    ledgeScanWindow = false;
                }
            }
            else
            {
                //No ledgescan while the character is on ground
                ledgeScanWindow = false;
            }
        }
        else
        {
            /*
            if (RB.velocity.y <= moveSetting.jumpVel * moveSetting.ledgeScanPer / 100.0f && ledgeJumping) 
            {
				ledgeScanWindow = true;

                //Debug.Log ("Ledge Scan window open" + RB.velocity.y);
                //Time.timeScale = 0.1f;
            }
            else */
            {
                //No ledge scan during in ledge mode,
                //All scans are initiated explicitly
                ledgeScanWindow = false;
                //Time.timeScale = 1.0f;
            }
        }
    }

    void Run()
    {
        //Movements should happen only when the character is not climbing
        if (!ledgeMode)
        {
            if (Mathf.Abs(fwdInput) > inputSetting.inputDelay)
            {
                velocity.z = moveSetting.fwdVelocity * fwdInput;
            }
            else
            {
                if (!backwardLedgeJumping)
                {
                    //No need to 0 the velocity is player is jumping backward
                    velocity.z = 0; //Debug.Log("Here");
                }
                else
                {
                    //If player is jumping backwards from a ledge, gradually decrease the Z velocity
                    Debug.Log("Reducing velocity "+velocity.z);

                    velocity.z -= 0.1f;

                    if (velocity.z < 0.0f)
                    {
                        velocity.z = 0.0f;
                        ledgeJumping = false;
                        backwardLedgeJumping = false;
                    }
                }
            }
        }
        else
        {
            ledgeMoveWindow_UD = fwdInput;
        }
    }

    void Turn()
    {
        if (!ledgeMode)
        {
            //No turning while hanging on ledge
            if (Mathf.Abs(turnInput) > inputSetting.inputDelay)
            {
                targetRotation *= Quaternion.AngleAxis(moveSetting.rotVelocity * turnInput * Time.deltaTime, Vector3.up);
                transform.rotation = targetRotation;
            }
        }
        if (ledgeMode && !freezeLedgeMove)
        {
            if (Mathf.Abs(turnInput) > inputSetting.inputDelay)
            {
                ledgeMoveWindow_RL = turnInput;
            }
        }


    }
    void Jump()
    {
        if (!ledgeMode)
        {
            //When the character is climbing towards a ledge
            //Or hanging from a ledge a vertical jump must not happen
            if (jumpInput > 0 && Grounded())
            {
                velocity.y = moveSetting.jumpVel;
                initialVelocity = moveSetting.jumpVel;
            }
            else if (jumpInput == 0 && Grounded())
            {
                //velocity.x = 0;
                velocity.y = 0;
            }
            else
            {
                //If player is not grounded
                //apply gravity
                if (!backwardLedgeJumping)
                {
                    velocity.y -= physSetting.downAccel;//Debug.Log("Gravitime");
                }
                else
                {
                    if (!Grounded())
                    {
                        velocity.y -= (0.1f); //If jumping from ledge to ledge, slow down gradually
                    }
                    else
                    {
                        velocity.y = 0.0f;
                        //velocity.z = 0.0f;
                        backwardLedgeJumping = false;
                    }
                }
            }

            if (freezeRegJump)
            {
                //velocity.x = 0;
                velocity.y = 0;
            }
        }
        if (jumpInputReceived)
        {
            jumpInputReceived = false;
            if (ledgeMode && !freezeLedgeJump)
            {
                //When the character is climbing towards a ledge
                //Or hanging from a ledge a vertical jump must not happen
                if (jumpInput > 0 && !ledgeJumping)
                {
                    /*velocity.z = -moveSetting.jumpBackFromLedgeVel;
                    velocity.y = moveSetting.jumpVel;*/

                    //Auto move to ledge must happen only when mouse is in deadzone
                    //Else the wall climb will handle the movement programatically
                    if (autoJumpMode && GetJumpDirection(true) == Vector3.zero)
                    {
                        //Do not jump if player is hanging on plateau
                        //Wall Climb will take care of movement to Plateau
                        autoJumpMode = false;
                        //ledgeScanWindow = true;
                        plateauMoveInitiated = true;
                        ledgeJumping = true;
                        //jumpInputReceived = true;
                    }
                    else
                    {
                        autoJumpMode = false;
                        ledgeJumpWindow_RL = GetJumpDirection(true);

                        //If player is jumping to the right of left
                        //It is handled by Wall Climb, without any rigid body

                        //If the mouse is within the deadzone, player actually JUMPS, If not hanging from a plateau
                        if (ledgeJumpWindow_RL == Vector3.zero)
                        {
                            ledgeMode = false;
                            ledgeScanWindow = false;
                            freezeMovement = false;
                            freezeRegJump = false;
                            freezeLedgeJump = true;
                            freezeLedgeMove = true;
                            RB.isKinematic = false;

                            velocity.y = moveSetting.jumpVel;
                            initialVelocity = moveSetting.jumpVel;
                            GetComponent<WallClimbing>().playerState = PlayerStates.Normal;


                        }
                        else
                        {
                            if (ledgeJumpWindow_RL.z != 0.0f)
                            {
                                //This is a backwards jump
                                ledgeMode = false;
                                ledgeScanWindow = false;
                                freezeMovement = false;
                                freezeRegJump = false;
                                freezeLedgeJump = true;
                                freezeLedgeMove = true;
                                transform.forward = -transform.forward; //Face backward
                                ledgeJumping = true;
                                backwardLedgeJumping = true;
                                GetComponent<WallClimbing>().playerState = PlayerStates.Normal;
                                RB.isKinematic = false;

                                velocity.y = 1.0f;
                                initialVelocity = 1.0f;
                                velocity.z = moveSetting.jumpBackFromLedgeVel;
                                //RB.AddForce(new Vector3(0.0f, 1.0f, moveSetting.jumpBackFromLedgeVel), ForceMode.VelocityChange);
                            }
                            else //Else, the program will handle the movement
                            {
                                //This is a directional jump towards a ledge
                                //float jumpAngle = Vector3.Angle(Vector3.right, jumpDirection);
                                //Debug.Log(jumpAngle);

                                //Debug.DrawRay(transform.position, jumpDirection * 100.0f, Color.green, 1000);
                                //Debug.Log(transform.position+"Start Jump");
                                ledgeJumping = true;

                                //jumpInputReceived = true; //Player should go up the plateau when hanging off it's ledge and jump button is pressed
                                freezeLedgeJump = true;
                            }
                        }
                    }
                }
                else if (jumpInput == 0 && !ledgeJumping)
                {
                    //velocity.x = 0; //To make sure any sideway velocity is reset
                    velocity.y = 0;
                }
                else
                {
                    //If player is jumping between ledges
                    //apply gravity
                    velocity.y -= physSetting.downAccel;
                }
            }
        }
        if (jumpInput > 0)
        {

        }

    }

    public Vector3 GetJumpDirection(bool simple = false) //If simple mode, jump direction will be just between -1 & 1. No velocity applied.
    {
        float yAngle = Vector3.Angle(CC.transform.forward, transform.forward);
        float mouseX = Input.mousePosition.x - screenCentreX;
        float mouseY = Input.mousePosition.y - screenCentreY;


        //float horizontalInput = mouseX / screenCentreX ;
        //float verticalInput = mouseY / screenCentreY;


        float horizontalPer = mouseX / (Mathf.Abs(mouseX) + Mathf.Abs(mouseY));
        float verticalPer = mouseY / (Mathf.Abs(mouseX) + Mathf.Abs(mouseY));

        //If mouse is within the deadzone, consider the input to be zero
        //For joystick the logic must be different, just getting mouseX/Y from joystick should be enough
        if (Mathf.Abs(mouseX) <= deadZone / 2 && Mathf.Abs(mouseY) <= deadZone / 2)
        {
            //If mouse has not moved beyond n % of screen centre
            horizontalPer = 0.0f;
            verticalPer = 0.0f;
        }

        //Debug.Log(Mathf.Abs(mouseX) + ":" + Mathf.Abs(mouseY));

        //Debug.Log(horizontalPer + ":" + verticalPer);


        //Return the mouse position between -1 and 1
        //Top left : -1,1
        //Top right: 1,1
        //Bot left : -1,-1
        //Bot right: 1,-1

        if (yAngle <= 45.0f)
        {
            if (simple)
            {
                //If the camera is right behind the player, consider the mouse movement for right/left jump direction
                return new Vector3(horizontalPer, verticalPer, 0.0f);
            }
            else
            {
                //If the camera is right behind the player, consider the mouse movement for right/left jump direction
                return new Vector3(horizontalPer * moveSetting.jumpSideFromLedgeVel, /*verticalPer */ moveSetting.jumpVel, 0.1f);
            }
        }
        else if (yAngle <= 135.0f)
        {
            //If the camera is to the right/left, consider them mouse movement for backward jump direction
            //Do not consider any input in front of the player
            if (horizontalPer < 0)
            {
                if (simple)
                {
                    return new Vector3(0.0f, verticalPer, horizontalPer);

                }
                else
                {
                    return new Vector3(0.0f, /*verticalPer */ moveSetting.jumpVel, horizontalPer * moveSetting.jumpBackFromLedgeVel);
                }
            }
        }

        return Vector3.zero;

    }

    void OnGUI()
    {
        //To mark the dead zone
        GUI.Box(new Rect(screenCentreX - (deadZone / 2), screenCentreY - (deadZone / 2), deadZone, deadZone), "");
    }

}
