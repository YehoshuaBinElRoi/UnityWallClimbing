using UnityEngine;
using System.Collections;

public class WallClimbing : MonoBehaviour
{
    public CameraController camCtrl;
    public CharControl3rdPerson charCtrl;

    private PlayerStates prevPlayerState;
    public PlayerStates playerState;

    bool ledgeMode;

    [System.Serializable]
    public class InputSettings

    {
        public float inputDelay = 0.1f; //Deadzone for key press
        public string FORWARD_AXIS = "Vertical";
        public string TURN_AXIS = "Horizontal";
        public string JUMP_AXIS = "Jump";
        public string CROUCH_DROP = "Crouch";
    }

    public InputSettings inputSetting = new InputSettings();



    public Transform ledgeScanPivot;
    public Transform orientationTarget;
    public LayerMask ledgeLayers;
    public GameObject handTarget;
    public GameObject bodyTarget;
    public GameObject r_shoulder;
    public GameObject r_hand;
    public GameObject r_orientation;
    public GameObject l_shoulder;
    public GameObject l_hand;
    public GameObject l_orientation;

    //If the identified is outside under or over slopy it cannot be climbed
    public float minGrabbableAngle_X = 60.0f;
    public float minGrabbableAngle_Z = 60.0f;
    public float maxGrabbableAngle_X = 120.0f;
    public float maxGrabbableAngle_Z = 120.0f;
    public float climbSpeed;


    float fwdInput;
    float turnInput;
    bool jumpInputReceived;
    bool dropInputReceived;
    float deadZone = 50.0f;//0 for joystick
    Vector3 faceDirection;
    float ledgeClimbDuration;
    Vector3 ledgeScanDirection;


    string latestMovementInput;

    float screenCentreX;
    float screenCentreY;
    LedgeDetails ledgeDetails;
    LedgeDetails prevLedgeDetails;
    Transform bodyReference;
    Transform handReference;
    CapsuleCollider playerCollider;

    //>>>TEMP-REMOVE AFTER TESTING
    bool drawray;
    //>>>TEMP-REMOVE AFTER TESTING

    Vector3 capsuleGizmoLocation1;
    Vector3 capsuleGizmoLocation2;

    // Use this for initialization
    void Start()
    {
        //Get the centre point of the display
        //Used with various platforming elements
        screenCentreX = Screen.width / 2;
        screenCentreY = Screen.height / 2;

        playerState = PlayerStates.Normal;

        bodyReference = new GameObject().transform;
        bodyReference.name = "Body";
        handReference = new GameObject().transform;
        handReference.name = "Hand";

        playerCollider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();


        if (Input.GetKeyDown(KeyCode.P))
        {
            drawray = !drawray;
            if (Time.timeScale == 1.0f)
            {
                Time.timeScale = 0.1f;
            }
            else
            {
                Time.timeScale = 1.0f;
            }
        }
    }

    void FixedUpdate()
    {
        if (ledgeMode)
        {
            //Debug.Log(ledgeDetails.ledgeLocation);
            //Once the player is on ledge
            //All movements are done by our code
            //Rigidbody is not involved
            //If player pressed the JUMP button this frame
            //Or the UP button
            if (jumpInputReceived || fwdInput > 0.0f)
            {

                //IF the mouse is pointing in a direction outside deadzone
                //Do a ledgescan in that direction
                ledgeScanDirection = GetLedgeJumpDirection();
                if (jumpInputReceived &&
                    ledgeScanDirection != Vector3.zero &&
                    (playerState == PlayerStates.PlateauHanging || playerState == PlayerStates.LedgeHanging)
                    )
                {
                    if (ledgeScanDirection.x > 0.0f)
                    {
                        prevPlayerState = playerState;
                        prevLedgeDetails = ledgeDetails;
                        playerState = PlayerStates.LedgeScanning;
                        ledgeDetails = JumpToLedge(
                                                    r_shoulder,
                                                    r_hand,
                                                    r_orientation,
                                                    1.0f, //Scan to right or left
                                                    -90, 0,//back to front sweep angle
                                                    90, -45,//top to bottom sweep angle
                                                    0.3f,//min right to left sweep distance
                                                    1.8f,//max right to left sweep distance
                                                    3,//Back to front sweep angle increment
                                                    0.2f,//Right to left sweep distance increment 
                                                    3,//Top to botton sweep angle increment
                                                    ledgeScanDirection //exact scan direction

                                                    );
                        if (!ledgeDetails.ledgeFound)
                        {
                            playerState = prevPlayerState;
                            ledgeDetails = prevLedgeDetails;
                        }
                        else
                        {
                            playerState = PlayerStates.LedgeClimbing;
                            ledgeClimbDuration = 0.0f; //Climbing should take so long. Should not be an instant teleport
                        }
                    }
                    else if (ledgeScanDirection.x < 0.0f)
                    {
                        prevPlayerState = playerState;
                        prevLedgeDetails = ledgeDetails;

                        playerState = PlayerStates.LedgeScanning;
                        ledgeDetails = JumpToLedge(
                                                    l_shoulder,
                                                    l_hand,
                                                    l_orientation,
                                                    -1.0f, //Scan to right or left
                                                    -90, 0, //back to front sweep angle
                                                    90, -45,//top to bottom sweep angle
                                                    0.3f,//min right to left sweep distance
                                                    1.8f,//max right to left sweep distance
                                                    3, //Back to front sweep angle increment
                                                    0.2f, //Right to left sweep distance increment 
                                                    3,//Top to botton sweep angle increment
                                                    ledgeScanDirection //exact scan direction
                                                    );
                        if (!ledgeDetails.ledgeFound)
                        {
                            playerState = prevPlayerState;
                            ledgeDetails = prevLedgeDetails;
                        }
                        else
                        {
                            playerState = PlayerStates.LedgeClimbing;
                            ledgeClimbDuration = 0.0f; //Climbing should take so long. Should not be an instant teleport
                        }
                    }
                    else if (ledgeScanDirection.z != 0f)
                    {
                        //The camera is to the side of the player
                        //And the mouse is pointing backward relative to the player

                        playerState = PlayerStates.Normal;
                        ledgeMode = false;

                        //Turn player around
                        transform.forward = -transform.forward;

                        //Initiate a backward jump
                        charCtrl.UnblockCharControl();
                        charCtrl.InitiateBackwardJump(7.5f, 7.0f);


                    }
                }
                else
                {
                    //If forward button is pressed or
                    //If jump button is pressed with pointers/joystick in deadzone
                    if (playerState == PlayerStates.PlateauHanging)
                    {
                        //If the player is hanging off a plateau
                        //Move the player to the plateau instead
                        playerState = PlayerStates.PlateauClimbing;
                        ledgeClimbDuration = 0.0f; //Climbing should take so long. Should not be an instant teleport
                    }
                    else if (playerState == PlayerStates.LedgeHanging)
                    {
                        //If the player is hanging off a ledge
                        //Initiate a jump and the ledge scan logic will take care of the rest
                        playerState = PlayerStates.Normal;
                        ledgeMode = false;
                        charCtrl.UnblockCharControl();
                        charCtrl.InitiateJump(/*If needed specify Z, Y velocity here*/);
                    }
                }
                jumpInputReceived = false;
            }
            else
            {

            }

            if (playerState == PlayerStates.PlateauClimbing)
            {
                //When player pressed JUMP/UP while hanging off a plateau
                //Move the player up the plateau
                MoveToLedge(ledgeDetails.ledgeLocation,
                            ledgeDetails.targetRotation,
                            bodyTarget.gameObject,
                            PlayerStates.Normal
                            );
            }
            else if (playerState == PlayerStates.LedgeClimbing)
            {
                //If a ledge/plateau is identified
                //Just make sure the player hangs off it
                //Should not climb until JUMP/UP Pressed
                if (ledgeDetails.ledgeType == LedgeType.Ledge)
                {
                    MoveToLedge(ledgeDetails.ledgeLocation,
                                ledgeDetails.targetRotation,
                                handTarget.gameObject,
                                PlayerStates.LedgeHanging
                                );
                }
                else if (ledgeDetails.ledgeType == LedgeType.Plateau)
                {
                    MoveToLedge(ledgeDetails.ledgeLocation,
                                ledgeDetails.targetRotation,
                                handTarget.gameObject,
                                PlayerStates.PlateauHanging
                                );
                }
            }

            //If the drop down button is pressed
            if (dropInputReceived)
            {
                //Debug.Log("Dropping");
                //While player is hanging off a ledge
                if (playerState == PlayerStates.LedgeHanging || playerState == PlayerStates.PlateauHanging)
                {
                    //Reactivate the third person controller
                    //So that the player falls down
                    playerState = PlayerStates.Normal;
                    ledgeMode = false;
                    charCtrl.UnblockCharControl();
                }
            }

            //If player RIGHT/LEFT Movement button is pressed
            if (turnInput > 0.0f)
            {
                if (playerState == PlayerStates.LedgeHanging || playerState == PlayerStates.PlateauHanging)
                {
                    prevPlayerState = playerState;
                    prevLedgeDetails = ledgeDetails;

                    playerState = PlayerStates.LedgeScanning;
                    ledgeDetails = MoveAlongLedge(
                                                    r_shoulder,
                                                    r_hand,
                                                    r_orientation,
                                                    1.0f,
                                                    -90/*0*/, 90,       //Back to Front sweep angle (Shoulder rotation)
                                                    90, -90,     //Top to Bottom sweep angle (Hand rotation)
                                                    0.0f, 0.3f,  //Sweep Distance Right from Left
                                                    3, 0.1f, 3);

                    if (!ledgeDetails.ledgeFound)
                    {
                        playerState = prevPlayerState;
                        ledgeDetails = prevLedgeDetails;
                    }
                    else if (ledgeDetails.ledgeFound)
                    {
                        playerState = PlayerStates.LedgeClimbing;
                        ledgeClimbDuration = 0.0f; //Climbing should take so long. Should not be an instant teleport
                    }

                }
            }
            else if (turnInput < 0.0f)
            {
                if (playerState == PlayerStates.LedgeHanging || playerState == PlayerStates.PlateauHanging)
                {
                    prevPlayerState = playerState;
                    prevLedgeDetails = ledgeDetails;

                    playerState = PlayerStates.LedgeScanning;
                    ledgeDetails = MoveAlongLedge(
                                                    l_shoulder,
                                                    l_hand,
                                                    l_orientation,
                                                    -1.0f,
                                                    -90/*0*/, 90,       //Back to Front sweep angle (Shoulder rotation)
                                                    90, -90,     //Top to Bottom sweep angle (Hand rotation)
                                                    0.0f, 0.3f,  //Sweep Distance Right to Left
                                                    3,
                                                    0.1f,
                                                    3);

                    if (!ledgeDetails.ledgeFound)
                    {
                        playerState = prevPlayerState;
                        ledgeDetails = prevLedgeDetails;
                    }
                    else if (ledgeDetails.ledgeFound)
                    {
                        playerState = PlayerStates.LedgeClimbing;
                        ledgeClimbDuration = 0.0f; //Climbing should take so long. Should not be an instant teleport
                    }

                }
            }

        }
        else
        {
            //If a ledge scan window has been initiated by Char. controller
            if (charCtrl.CheckLedgeScanWindow())
            {
                //Debug.Log("Scanning");
                prevPlayerState = playerState;
                playerState = PlayerStates.LedgeScanning;
                ledgeDetails = LedgeScan(
                                            ledgeScanPivot.transform,       //Ray is cast from here. The chest
                                            orientationTarget.transform,    //Ray cast is done from here to find the wall direction
                                            90.0f,                          //From Right above
                                            0.0f,                           //To the front
                                            0.6f
                                            );
                if (ledgeDetails.ledgeFound)
                {
                    //If a ledge has been found
                    //Prepare the player to move to the ledge
                    ledgeMode = true;
                    charCtrl.BlockCharControl();
                    //if (ledgeDetails.ledgeType == LedgeType.Ledge)
                    {
                        //If the identified point is a ledge
                        //Set player state accordingly,
                        //So that the program initiates a climb to ledge during the next frame
                        playerState = PlayerStates.LedgeClimbing;
                        ledgeClimbDuration = 0.0f; //Climbing should take so long. Should not be an instant teleport
                    }
                    //else if (ledgeDetails.ledgeType == LedgeType.Plateau)
                    //{
                    //    playerState = PlayerStates.PlateauClimbing;
                    //}
                }
                else
                {
                    playerState = PlayerStates.Normal;
                    //Else, resume normal 3rd Person movement
                    /*ledgeMode = false;
                    charCtrl.UnblockCharControl();*/
                }
            }
        }
    }

    LedgeDetails JumpToLedge(
                                GameObject p_shoulder,
                                GameObject p_hand,
                                GameObject p_orientationTarget,
                                float dir, //-1 for left, 1 for right
                                int sweepStartAngle_BF, //Back to forward sweep start angle of Shoulder
                                int sweepEndAngle_BF, //Back to forward sweep end angle of Shoulder
                                int sweepStartAngle_UD, //Top to bottom sweep start angle of Hand
                                int sweepEndAngle_UD, //Top to bottom sweep end angle of Hand
                                float minSweepDistance_RL, //Minimum sweep distance to the right or the left
                                float maxSweepDistance_RL, //Maximum sweep distance to the right or the left
                                int sweepAngleStep_BF = 1, //Back to forward sweeping will increment angles in these steps, not one degree always
                                float sweepDistStep_RL = 0.1f, //Scans are moved from right to left or viceversa in these increments
                                int sweepAngleStep_UD = 1, //Back to forward sweeping will increment angles in these steps, not one degree always
                                                           //Higher values for steps increases performances decreases accuracy
                                Vector3 scanDirection = default(Vector3) //Angle of scan, mouse direction
                            )
    {

        //In this case the scan will be done as described below
        //We just rotate the hand 1 degree forward and re-scan until a ledge is found
        //If no ledge is found,
        //From the farthest left/right move towards the the shoulder


        //We need the individual position and rotation as transform changes, as if it is passed by reference,
        //whenever a rotation or position is changed in the loop
        Transform initialShoulderTransform = p_shoulder.transform;
        Vector3 initialShoulderPosition = p_shoulder.transform.position;
        Quaternion intialShoulderRotation = p_shoulder.transform.rotation;

        Transform initialHandTransform = p_hand.transform;
        Vector3 initialHandPosition = p_hand.transform.position;
        Vector3 initialHandLocalPos = p_hand.transform.localPosition;
        Quaternion initialHandRotation = p_hand.transform.rotation;

        Transform initialOrientation = p_orientationTarget.transform;

        Transform shoulderTransform = p_shoulder.transform;
        Transform handTransform = p_hand.transform;
        Transform orientation = p_orientationTarget.transform;

        float handDistance = maxSweepDistance_RL;
        float yRotationArm = 0.0f;
        float zRotationArm = Vector3.Angle(dir * Vector3.right, scanDirection); zRotationArm = dir * zRotationArm;
        float zRotationSign = Mathf.Sign(scanDirection.y);

        Quaternion yRotationHand;
        Quaternion xzRotationHand;

        Vector3 displacementDirection;

        LedgeDetails ledgeDetails;

        zRotationArm = zRotationArm * zRotationSign;

        for (handDistance = maxSweepDistance_RL; handDistance >= minSweepDistance_RL; handDistance -= sweepDistStep_RL)
        {
            for (int shoulderAngle = sweepStartAngle_BF; shoulderAngle <= sweepEndAngle_BF; shoulderAngle += sweepAngleStep_BF)
            {
                //reset the position and rotation every loop
                //so that the angle and displacement can be applied to initial position
                shoulderTransform = p_shoulder.transform;
                handTransform = p_hand.transform;

                //Expected Angle of Arm around Y axis
                yRotationArm = dir * -shoulderAngle;

                //Apply the Y rotation to the shoulder
                shoulderTransform.localRotation = Quaternion.AngleAxis(yRotationArm, Vector3.up);

                //Z Rotation isrequired to perform a scan in the mouse pointing direction
                shoulderTransform.localRotation = Quaternion.AngleAxis(zRotationArm, Vector3.forward) * shoulderTransform.localRotation;

                yRotationHand = Quaternion.FromToRotation(handTransform.forward, shoulderTransform.forward); //Forward will be rotated around Y
                handTransform.rotation *= yRotationHand;

                xzRotationHand = Quaternion.FromToRotation(handTransform.up, Vector3.up);
                handTransform.rotation = xzRotationHand * handTransform.rotation;

                //move the scan pivot right/left towards the hand pivot/shoulder
                displacementDirection = dir * shoulderTransform.right;
                
                /*Debug.DrawRay(shoulderTransform.transform.position,
                                displacementDirection * maxSweepDistance_RL,
                                    new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f)), 1.0f);*/

                handTransform.localPosition = initialHandLocalPos + new Vector3(handDistance, 0, 0) * dir;

                if (handDistance == maxSweepDistance_RL)
                {
                    //Debug.DrawRay(handTransform.position, handTransform.up, Color.black, 1.0f);
                    //Debug.DrawRay(handTransform.position, handTransform.forward, Color.blue, 1.0f);
                }

                ledgeDetails = LedgeScan(
                                            handTransform,
                                            orientation,
                                            sweepStartAngle_UD,
                                            sweepEndAngle_UD,
                                            0.65f + 0.3f, //Grab distance, adding 0.3 to allow jumping from a shorter ledge to a larger ledge
                                            true,  //
                                            sweepAngleStep_UD
                                        );

                if (ledgeDetails.ledgeFound)
                {
                    //reset the position and rotation
                    p_shoulder.transform.position = initialShoulderPosition;
                    p_hand.transform.position = initialHandPosition;

                    p_shoulder.transform.rotation = intialShoulderRotation;
                    p_hand.transform.rotation = initialHandRotation;

                    return ledgeDetails;
                }

            }
        }

        //reset the position and rotation
        p_shoulder.transform.position = initialShoulderPosition;
        p_hand.transform.position = initialHandPosition;

        p_shoulder.transform.rotation = intialShoulderRotation;
        p_hand.transform.rotation = initialHandRotation;

        return new LedgeDetails();
    }

    LedgeDetails MoveAlongLedge(
                                    GameObject p_shoulder,
                                    GameObject p_hand,
                                    GameObject p_orientationTarget,
                                    float dir, //-1 for left, 1 for right
                                    int sweepStartAngle_BF, //Back to forward sweep start angle of Shoulder
                                    int sweepEndAngle_BF, //Back to forward sweep end angle of Shoulder
                                    int sweepStartAngle_UD, //Top to bottom sweep start angle of Hand
                                    int sweepEndAngle_UD, //Top to bottom sweep end angle of Hand
                                    float minSweepDistance_RL, //Minimum sweep distance to the right or the left
                                    float maxSweepDistance_RL, //Maximum sweep distance to the right or the left
                                    int sweepAngleStep_BF = 1, //Back to forward sweeping will increment angles in these steps, not one degree always
                                    float sweepDistStep_RL = 0.1f, //Scans are moved from right to left or viceversa in these increments
                                    int sweepAngleStep_UD = 1//Back to forward sweeping will increment angles in these steps, not one degree always
                                                             //Higher values for steps increases performances decreases accuracy*/
                                )
    {
        //Arm rotation
        float yRotationArm = 0.0f;

        Transform shoulderTransform = p_shoulder.transform;
        Vector3 initialShoulderPosition = p_shoulder.transform.position;
        Quaternion initialShoulderRotation = p_shoulder.transform.rotation;

        Transform handTransform = p_hand.transform;
        Vector3 initialHandPosition = p_hand.transform.position;
        Quaternion initialHandRotation = p_hand.transform.rotation;
        Vector3 initialHandLocalPosition = p_hand.transform.localPosition;

        Transform orientation = p_orientationTarget.transform;

        Vector3 displacementDirection;
        float handDistance;

        RaycastHit hit;

        //If the hand is already inside a wall
        //Do not perform a scan
        if (p_hand.GetComponentInChildren<CheckCollisionStatus>().objectTangled)
        {
            return new LedgeDetails();
        }

        LedgeDetails ledgeDetails = new LedgeDetails();

        for (int shoulderAngle = sweepStartAngle_BF; shoulderAngle <= sweepEndAngle_BF; shoulderAngle += sweepAngleStep_BF)
        {
            if (shoulderAngle == sweepStartAngle_BF)
            {
                drawray = true;
            }
            else
            {
                drawray = false;
            }

            //Get the current TRANSFORM of the shoulder and the hand
            shoulderTransform = p_shoulder.transform;
            handTransform = p_hand.transform;

            //The angle of the shoulder around Y axis
            //Angle required to Rotate the shoulder around Y Axis
            yRotationArm = dir * -shoulderAngle;

            //Rotate shoulder around Y Axis
            shoulderTransform.localRotation = Quaternion.AngleAxis(yRotationArm, Vector3.up);

            //The direction in which we scan for the ledge
            displacementDirection = dir * shoulderTransform.right;

            //Keep scanning from the maximum distance to the point right at shoulder
            for (handDistance = maxSweepDistance_RL; handDistance >= minSweepDistance_RL; handDistance -= sweepDistStep_RL)
            {
                //Position the hand at the correct distance from the shoulder
                //So that the scan happens at that distance
                handTransform.localPosition = initialHandLocalPosition + new Vector3(handDistance, 0, 0) * dir;

                //If at this distance, the hand is already inside a wall
                //Do not perform a scan
                if (p_hand.GetComponentInChildren<CheckCollisionStatus>().objectTangled)
                {
                    continue;
                }

                //Sometime the above check might fail
                //So, we do a raycast to check if there is a wall between the shoulder
                //And the expected hand position
                if (Physics.Raycast(shoulderTransform.position,
                                    (handTransform.position - shoulderTransform.position),
                                    out hit,
                                    handDistance,
                                    ledgeLayers
                                    )
                                    )
                {
                    continue;
                }
                Debug.DrawRay(shoulderTransform.position, (handTransform.position - shoulderTransform.position),
                                //new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f)),
                                    Color.white, 
                                    1.0f);


                //Look for aledge
                ledgeDetails = LedgeScan(
                                            handTransform,
                                            orientation,
                                            sweepStartAngle_UD,
                                            sweepEndAngle_UD,
                                            0.6f,
                                            true,
                                            sweepAngleStep_UD
                                        );
                if (ledgeDetails.ledgeFound)
                {
                    //Restore the initial position and rotation of hand and shoulder

                    p_shoulder.transform.position = initialShoulderPosition;
                    p_shoulder.transform.rotation = initialShoulderRotation;

                    p_hand.transform.position = initialHandPosition;
                    p_hand.transform.rotation = initialHandRotation;

                    return ledgeDetails;
                }
            }

            //Due to floating point precision,  sometimes, the loop will never reach 0.0f.
            //Because it might be 0.09 instead of 0.1 before the last iteration
            //So we must do one more scan to see if there is a ledge right in front of the hand-shoulder joint
            if (handDistance <= 0.0f && !ledgeDetails.ledgeFound)
            {
                handTransform.localPosition = initialHandLocalPosition + new Vector3(0, 0, 0) * dir;

                ledgeDetails = LedgeScan(handTransform,
                                         orientation,
                                         sweepStartAngle_UD,
                                         sweepEndAngle_UD,
                                         0.6f,
                                         true,
                                         sweepAngleStep_UD);
            }

            if (ledgeDetails.ledgeFound)
            {
                //Restore the initial position and rotation of hand and shoulder

                p_shoulder.transform.position = initialShoulderPosition;
                p_shoulder.transform.rotation = initialShoulderRotation;

                p_hand.transform.position = initialHandPosition;
                p_hand.transform.rotation = initialHandRotation;

                return ledgeDetails;
            }

        }

        //Restore the initial position and rotation of hand and shoulder


        p_shoulder.transform.position = initialShoulderPosition;
        p_shoulder.transform.rotation = initialShoulderRotation;

        p_hand.transform.position = initialHandPosition;
        p_hand.transform.rotation = initialHandRotation;

        return new LedgeDetails();
    }

    LedgeDetails LedgeScan(
                            //The raycasts are done from here to detect the ledges also use to check the angle of the ledges
                            Transform p_pivotTransform,
                            //Raycasts are done from here to check which direction is facing the wall
                            Transform p_orientationTransform,
                            float startAngle,
                            float endAngle,
                            //Distance of the forward raycast. 
                            float p_grabDistance,
                            //Use player body to calculate final facedirection or the scan pivot
                            bool overrideOrientationTransform = false,
                            //Top to bottom sweeping will increment angles in these steps, not one degree always
                            //Bigger steps improves performance, but reduces accuracy

                            //Example: a 3 degree angle difference, at 2 unit distance will account for a gap of 0.1 units
                            //I.e., if two lines of length 2 units at 3 degrees to each other are starting at the same point. The end points will be 0.1 units away
                            //So a scan with 3 degree angle between each ray will surely find a ledge which is atleast 0.1 unit high (i.e., there is nothing above the ledge for up to 0.1 unit)
                            //If it is less than 0.1 unit it is findable depending on when the scan started
                            int angleStep = 1
                            )
    {
        Vector3 rayDirection;
        RaycastHit hit;
        float angleX;
        float angleZ;
        LedgeDetails ledgeDetails;

        //Debug.Log("Ledge Scanning");

        for (float i = startAngle; i >= endAngle; i -= angleStep)
        {
            //-i        :   The angle of the ray
            //right     :   Around which axis is  this rotated
            //forward   :   This is the vector which is rotated

            rayDirection = Quaternion.AngleAxis(-i, p_pivotTransform.right) * p_pivotTransform.forward;



            if (i == startAngle)
            {
                Debug.DrawRay(p_pivotTransform.position,
                                rayDirection * p_grabDistance,
                                /*new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f))*/
                                Color.black,
                               1.0f);
            }
            else if ( i == endAngle)
            {
                Debug.DrawRay(p_pivotTransform.position,
                                rayDirection * p_grabDistance,
                                /*new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f))*/
                                Color.red,
                               1.0f);
            }
            //Time.timeScale = 0.0f;

            if (Physics.Raycast(p_pivotTransform.position,
                                rayDirection,
                                out hit,
                                p_grabDistance,
                                ledgeLayers
                                ))
            {
                //If there is an obstruction before the maximum hand reachable point
                //And it is not pointing downward
                if (hit.normal.y > 0)
                {
                    ledgeDetails = LedgeFeasible(hit,
                             p_pivotTransform.forward,
                             p_pivotTransform.transform.right,
                             p_orientationTransform.gameObject,
                             overrideOrientationTransform);
                    if (ledgeDetails.ledgeFound)
                    {
                        Debug.DrawRay(p_pivotTransform.position,
                                        rayDirection * p_grabDistance,
                                        /*new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f))*/
                                        Color.green,
                                        1.0f);

                        Debug.DrawRay(hit.point,
                                        transform.forward * p_grabDistance,
                                        Color.green, 1.0f);

                        Debug.DrawRay(hit.point,
                                        transform.up * p_grabDistance,
                                            Color.green, 1.0f);

                        Debug.DrawRay(hit.point,
                                        transform.right * p_grabDistance,
                                            Color.green, 1.0f);

                        return ledgeDetails;
                    }
                }
                else if (hit.normal.y < 0)
                {
                    //If the ray cast hit a ceiling or downward pointing wall
                    //Look for a ledge directly below it

                    /*Debug.DrawRay(hit.point,
                                    transform.forward * p_grabDistance,
                                        new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f)), 1.0f);*/

                    if (Physics.Raycast( /*<COMMENT-30.07.2018>
                                         //p_pivotTransform.position + rayDirection * (p_grabDistance),
                                         <COMMENT-30.07.2018>*/
                                         //Must raycast down from the hit point, not from grab distance
                                         //</CHANGE 30.07.2018>
                                         hit.point,
                                        //<CHANGE 30.07.2018/>
                                        -transform.up,
                                        out hit,
                                        (p_pivotTransform.position + rayDirection * (p_grabDistance)).y - p_pivotTransform.position.y + 0.1f,
                                        ledgeLayers
                                        )
                       )
                    {
                        ledgeDetails = LedgeFeasible(hit,
                                                     p_pivotTransform.forward,
                                                     p_pivotTransform.transform.right,
                                                     p_orientationTransform.gameObject,
                                                     overrideOrientationTransform);
                        if (ledgeDetails.ledgeFound)
                        {
                            //Debug.DrawRay(p_pivotTransform.position,
                            //rayDirection * p_grabDistance,
                            ///*new Color(Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f), Random.Range(0.1f, 1.0f))*/
                            //Color.red,
                            //1.0f);

                            Debug.DrawRay(hit.point,
                                            transform.forward * p_grabDistance,
                                                Color.blue, 1.0f);

                            Debug.DrawRay(hit.point,
                                            transform.up * p_grabDistance,
                                                Color.blue, 1.0f);

                            Debug.DrawRay(hit.point,
                                            transform.right * p_grabDistance,
                                                Color.blue, 1.0f);

                            return ledgeDetails;
                        }
                    }
                }
            }
            else
            {
                //If the raycast did not hit a wall yet

                //Raycast down from every hand-reachable point
                //To detect a ledge
                if (Physics.Raycast(p_pivotTransform.position + rayDirection * (p_grabDistance),
                                    -transform.up,
                                    out hit,
                                    (p_pivotTransform.position + rayDirection * (p_grabDistance)).y - p_pivotTransform.position.y + 0.1f,
                                    ledgeLayers
                                    )
                   )
                {
                    ledgeDetails = LedgeFeasible(hit,
                             p_pivotTransform.forward,
                             p_pivotTransform.transform.right,
                             p_orientationTransform.gameObject,
                             overrideOrientationTransform);
                    if (ledgeDetails.ledgeFound)
                    {
                        //Debug.DrawRay(p_pivotTransform.position,
                        //rayDirection * p_grabDistance,
                        //Color.black,
                        //1.0f);

                        Debug.DrawRay(hit.point,
                                        transform.forward * p_grabDistance,
                                            Color.yellow,
                                            1.0f);

                        Debug.DrawRay(hit.point,
                                        transform.up * p_grabDistance,
                                            Color.yellow,
                                            1.0f);

                        Debug.DrawRay(hit.point,
                                        transform.right * p_grabDistance,
                                            Color.yellow,
                                            1.0f);

                        return ledgeDetails;
                    }
                }
            }

        }
        return new LedgeDetails();
    }

    LedgeDetails LedgeFeasible(RaycastHit hit,
                                 Vector3 p_forward,
                                 Vector3 p_right,
                                 GameObject p_orientationTransform,
                                 bool p_overrideOrientationTransform)
    {

        float angleX;
        float angleZ;
        {
            //Get the angle of the surface
            angleX = Vector3.Angle(-p_forward, hit.normal);
            angleZ = Vector3.Angle(p_right, hit.normal);

            //If the surface is not too slopy
            if (minGrabbableAngle_X <= angleX && angleX <= maxGrabbableAngle_X &&
                minGrabbableAngle_Z <= angleZ && angleZ <= maxGrabbableAngle_Z)
            {
                if (!p_overrideOrientationTransform)
                {
                    faceDirection = FindFaceDirection(hit, transform);
                }
                else
                {
                    faceDirection = FindFaceDirection(hit, p_orientationTransform.transform);
                }

                //Check if the player will fit in the identified point
                //And in the direction player is facing
                ledgeDetails = ClimbableCheck(hit, faceDirection);
                if (ledgeDetails.ledgeFound)
                {
                    return ledgeDetails;
                }
            }
        }

        return new LedgeDetails();
    }

    Vector3 FindFaceDirection(RaycastHit p_hit, Transform p_orientationTarget)
    {
        Vector3 startPoint = p_orientationTarget.position;
        Vector3 endPoint = p_hit.point;
        float raycastHeight = 0.0f;

        startPoint.y = endPoint.y;

        RaycastHit faceRay;

        //Start raycasting towards the identified ledge point, to find the front facing direction
        //Keep looking for a wall 0.1 unit down from the beginning position till the foot of the player
        while (startPoint.y - p_orientationTarget.position.y >= 0.0f)
        {

            //Look for a wall between the player and the identified point
            if (Physics.Raycast(startPoint,
                                    (endPoint - startPoint),
                                        out faceRay,
                                            (endPoint - startPoint).magnitude * 2.0f, /*1.0f, Do not always use a constant 1.0f distance.*/
                                                ledgeLayers))
            {
                if (faceRay.normal.y <= 0) //If the face is facing forward or downward
                {
                    Debug.DrawRay(startPoint,
                                    (endPoint - startPoint) * 2.0f,
                                    Color.cyan,
                                    1.0f);
                    return -faceRay.normal;
                }
            }

            //Start looking for a wall 0.1 unit below
            //To allow finding direction with even thinner edges, 0.1 might have to be reduced even further
            startPoint.y -= 0.1f;
            endPoint.y -= 0.1f;
            raycastHeight -= 0.1f;
        }


        return p_orientationTarget.forward;
    }

    LedgeDetails ClimbableCheck(RaycastHit p_hit, Vector3 p_direction)
    {
        LedgeDetails ledgeDetails;

        //Check if the body will fit on the ledge
        //So that the player can climb it up
        ledgeDetails = CheckFit(bodyReference, //This empty object decides the orientation of the cube cast that is used to check the fit
                                p_direction,   //The direction that the target position will be facing. Currently the same as player facing. To be changed to wall direction
                                p_hit,   //The point where a ledge is identified
                                0.5f, 1.8f, 0.5f //Dimensions of body
                                );

        if (ledgeDetails.ledgeFound)
        {
            ledgeDetails.ledgeType = LedgeType.Plateau;
            return ledgeDetails;
        }

        //Check if the hand will fit on the ledge
        //So that the player can hang on it
        ledgeDetails = CheckFit(handReference, //This empty object decides the orientation of the cube cast that is used to check the fit
                                p_direction,   //The direction that the target position will be facing. Currently the same as player facing. To be changed to wall direction
                                p_hit,   //The point where a ledge is identified
                                0.25f, 0.1f, 0.25f //Dimensions of body
                                );

        if (ledgeDetails.ledgeFound)
        {
            ledgeDetails.ledgeType = LedgeType.Ledge;
            return ledgeDetails;
        }

        return new LedgeDetails();
    }

    LedgeDetails CheckFit(
                            Transform p_fitTarget,
                            Vector3 p_direction,
                            RaycastHit p_hit,
                            float p_xDim, float p_yDim, float p_zDim
                            )
    {
        LedgeDetails ledgeDetails = new LedgeDetails();
        Quaternion yRotation;
        Quaternion xzRotation;
        RaycastHit[] intersectingColliders;

        //We are checking whether there is enough space to mount the player
        //Or the player's hand in the identified ledge location

        p_fitTarget.rotation = Quaternion.identity;
        p_fitTarget.position = p_hit.point;

        //Forward will be rotated around Y, to face the expected direction
        //i.e., Rotate the reference object around Y to face the direction
        yRotation = Quaternion.FromToRotation(p_fitTarget.forward, p_direction);
        p_fitTarget.rotation *= yRotation;

        //Rotate the reference object to be aligned with the ledge/normal
        xzRotation = Quaternion.FromToRotation(p_fitTarget.up, p_hit.normal);
        p_fitTarget.rotation = xzRotation * p_fitTarget.rotation;

        //Move the aligned reference object slightly above the ledge
        //Adding 0.055f to make sure the box cast starts slightly above the identified ledge surface
        //This is to avoid collision of boxcast with the ledge itself
        p_fitTarget.transform.Translate(0, 0.055f + p_yDim / 2, p_zDim / 2);

        //Check if there is enough room for hand/player
        //If the below box cast (size of the hand/body) does not collide with anything, there is enough room for hand/player

        intersectingColliders = Physics.BoxCastAll(
                                                    p_fitTarget.transform.position,
                                                    new Vector3(p_xDim / 2, p_yDim / 2, p_zDim / 2),
                                                    p_fitTarget.transform.up,
                                                    p_fitTarget.rotation,
                                                    0.0f,
                                                    ledgeLayers
                                                   );



        if (intersectingColliders.Length == 0)
        {
            ledgeDetails.ledgeFound = true; //A ledge has been found
            ledgeDetails.ledgeLocation = p_hit.point; //A ledge is found at this point
            ledgeDetails.targetRotation = Quaternion.identity;
            ledgeDetails.targetRotation = Quaternion.AngleAxis(yRotation.eulerAngles.y, Vector3.up);//Apply just the Y rotation to face player to the wall

            //</TBD>
            //Check if player will end up inside the geometry if they hang from this position
            //We have already checked if the player's entire body will fit in this position

            Collider[] targetPositionCollisions = new Collider[0];
            Vector3 castCapsuleP1;
            Vector3 castCapsuleP2;

            //Identify the capsule end points
            castCapsuleP1 = (ledgeDetails.ledgeLocation - ledgeDetails.targetRotation * handTarget.transform.localPosition) +
                                playerCollider.center +
                                    playerCollider.transform.up *
                                        -((playerCollider.height * 0.5f) - playerCollider.radius);
            capsuleGizmoLocation1 = castCapsuleP1;

            castCapsuleP2 = castCapsuleP1 + playerCollider.transform.up * (playerCollider.height - (2 * playerCollider.radius));
            capsuleGizmoLocation2 = castCapsuleP2;
            //Debug.Log(p_fitTarget.name+"--"+castCapsuleP1+","+castCapsuleP2);

            targetPositionCollisions = Physics.OverlapCapsule(castCapsuleP1,
                                                                castCapsuleP2,
                                                                    playerCollider.radius,
                                                                        ledgeLayers);
            if (targetPositionCollisions.Length == 0)
            {
                //<TBD/>
                return ledgeDetails;
                //</TBD>
            }
            //<TBD/>
        }
        return new LedgeDetails();
    }

    void GetInput()
    {
        fwdInput = Input.GetAxis(inputSetting.FORWARD_AXIS);
        turnInput = Input.GetAxis(inputSetting.TURN_AXIS);
        jumpInputReceived = Input.GetButtonDown(inputSetting.JUMP_AXIS);
        dropInputReceived = Input.GetButtonDown(inputSetting.CROUCH_DROP);
    }

    void Move()
    {

    }

    void Jump()
    {

    }

    public Vector3 GetLedgeJumpDirection()
    {
        //Get the angle between the player and the camera
        //Jump direction needs to consider this
        float yAngle = Vector3.Angle(camCtrl.transform.forward, transform.forward);

        
        float mouseX = Input.mousePosition.x - screenCentreX;
        float mouseY = Input.mousePosition.y - screenCentreY;

        //</TBD>In case joystick is used, mouseX/Y = AnalogX/Y (No need to subtract screenCentreX/Y)
        //Get the joystick movement in case a joystick is used
        //<TBD/>

        //Get the weighted mouse movement (X+Y = 1)
        float horizontalPer = mouseX / (Mathf.Abs(mouseX) + Mathf.Abs(mouseY));
        float verticalPer = mouseY / (Mathf.Abs(mouseX) + Mathf.Abs(mouseY));

        float rightORleft;

        if (Mathf.Abs(mouseX) <= deadZone / 2 && Mathf.Abs(mouseY) <= deadZone / 2)
        {
            //If mouse has not moved beyond n pixels of screen centre
            //There will be no movement
            horizontalPer = 0.0f;
            verticalPer = 0.0f;
        }


        if (yAngle <= 45.0f)
        {
            //If the camera is right behind the player, consider the mouse movement for right/left jump direction
            return new Vector3(horizontalPer, verticalPer, 0.0f);
        }
        else if (yAngle <= 135.0f)
        {
            rightORleft = GetRightLeft(camCtrl.transform, transform);
            //If the camera is to the right/left, consider them mouse movement for backward jump direction
            //Do not consider any input to the front of the player 
            if (rightORleft == 1f)
            {
                //When looking at the player from right
                //The mouse should be to the left of the screen if it is poiting backward
                if (horizontalPer < 0)
                {
                    return new Vector3(0.0f, verticalPer, horizontalPer);
                }
            }
            else if (rightORleft == -1f)
            {
                //When looking at the player from left
                //The mouse should be to the right of the screen if it is poiting backward
                if (horizontalPer > 0)
                {
                    return new Vector3(0.0f, verticalPer, horizontalPer);
                }
            }
        }

        return Vector3.zero;
    }

    float GetRightLeft(Transform targetObjectTransform, Transform pivotObjectTransform)
    {
        //Checks whether the target object is to the right(1) or left(-1) of the pivot object
        Vector3 direction = targetObjectTransform.position - pivotObjectTransform.position;
        Vector3 normal = Vector3.Cross(pivotObjectTransform.forward, direction);
        float sign = Vector3.Dot(normal, targetObjectTransform.up);

        if (sign > 0f)
        {
            return 1f;
        }
        else if (sign < 0f)
        {
            return -1f;
        }
        else
        {
            return 0;
        }
    }

    void MoveToLedge(Vector3 p_ledgeTargetPosition, //The target position on the ledge
                     Quaternion p_ledgeTargetRotation,   //The target rotation on the ledge. To make sure player faces the wall, if any
                     GameObject p_offsetTarget,      //The player will be positioned on the ledge relative to this position
                     PlayerStates p_finalState   //Once the player reaches the ledge, the player is set to be in this state
                        )
    {
        float distanceToLedge;
        Vector3 playerTargetPosition;
        Quaternion playerTargetRotation;

        ledgeClimbDuration += Time.deltaTime;

        //Slowly lerp towards the ledge/plateau
        /*transform.position*/
        playerTargetPosition = Vector3.Lerp(transform.position,
                                          p_ledgeTargetPosition - p_ledgeTargetRotation * p_offsetTarget.transform.localPosition,
                                          //localPosition to make sure player base location is at the offset distance
                                          //i.e., 
                                          //if the location identified is for Hand to rest (the hand needs to rest on the ledge)                                         
                                          //The movement is still being done in the body, w.r.t. hand
                                          //So we apply the Body -> Hand distance

                                          Time.deltaTime * climbSpeed /* ledgeClimbDuration*/);

        /*transform.rotation*/
        playerTargetRotation = Quaternion.Slerp(transform.rotation,
                                              p_ledgeTargetRotation,
                                              Time.deltaTime * climbSpeed /* ledgeClimbDuration*/);


        transform.position = playerTargetPosition;
        transform.rotation = playerTargetRotation;

        //How far from the target is the player
        distanceToLedge = Vector3.Distance(transform.position, (p_ledgeTargetPosition - p_ledgeTargetRotation * p_offsetTarget.transform.localPosition));

        //If player is closer to the ledge/Plateau
        //SNAP to the target position
        if (distanceToLedge <= 0.1f)
        {
            transform.position = (p_ledgeTargetPosition - p_ledgeTargetRotation * p_offsetTarget.transform.localPosition);
            transform.rotation = p_ledgeTargetRotation;
            playerState = p_finalState;



            //If the player is on a plateau now
            //Restore normal movement
            if (playerState == PlayerStates.Normal)
            {
                ledgeMode = false;
                charCtrl.UnblockCharControl();
                //Reset the ledge details, so that it is not processed in the next frame
                ledgeDetails = new LedgeDetails();
            }
        }

    }

    void OnGUI()
    {
        //To mark the dead zone
        GUI.Box(new Rect(screenCentreX - (deadZone / 2), screenCentreY - (deadZone / 2), deadZone, deadZone), "");
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        //Gizmos.color = Color.yellow;
        try
        {
            //Gizmos.DrawSphere(capsuleGizmoLocation1, playerCollider.radius);

            //Gizmos.DrawSphere(capsuleGizmoLocation2, playerCollider.radius);
        }
        catch (System.Exception e)
        {

        }

    }
}
