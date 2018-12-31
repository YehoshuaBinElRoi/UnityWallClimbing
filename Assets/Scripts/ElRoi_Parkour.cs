using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElRoi_Parkour : MonoBehaviour
{

    //Assumptions & Pre-requisites:
    //capsule collider exactly encloses the player
    //Player transform position is the base of the player
    //Dummy object to mark the position of hand, when hanging from ledge


    [System.Serializable]
    public class LineBasedLedgeScanSettings
    {
        public float scan_Height = 0.5f;
        public float scan_Depth = 0.8f;
        public float forwardScan_startDistance = 0.2f;//LB
        public float forwardScan_endDistance = 0.5f;//LB
        public float forwardScan_distanceStep = 0.1f;//LB
        public float forwardScan_PrecisionSteps = 0.01f;

        public float sideScan_startDistance = 0.75f;
        public float sideScan_endDistance = 0;
        public float sideScan_step = 0.1f;
        public float sideScan_sweepStartAngle = 90;
        public float sideScan_sweepEndAngle = -90;
        public float sideScan_sweepStep = 10;
        //public float sideScan_scanDistance = 0.2f;
        public float sideScan_scanStartDistance = 0;//LB
        public float sideScan_scanEndDistance = 0.2f;//LB
        public float sideScan_scanStep = 0.1f;
        public float sideScan_precisionStep = 0.01f;

        //Have different start distance for non horizontal scans
        //Any scan not horizontal is a vertical scan, allowing player to leap too far above/below feels unnatural
        public float leapScan_thresholdAngle = 25.0f;
        public float leapScan_thresholdBreachStartDistance = 1.2f; //Should be a little over the vertical ledge placement distance
        public float leapScan_startDistance = 2.0f;
        public float leapScan_endDistance = 0.5f;
        public float leapScan_step = 0.1f;
        public float leapScan_sweepStartAngle = 90;
        public float leapScan_sweepEndAngle = -25;
        public float leapScan_sweepStep = 10;
        public float leapScan_scanStartDistance = 0;//LB
        //This should be > maximum Z distance between two ledges + maximum Z distance between player and the current ledge
        public float leapScan_scanEndDistance = 1.0f;//LB
        public float leapScan_scanStep = 0.1f;
        public float leapScan_precisionStep = 0.01f;


    }

    [System.Serializable]
    public class MovementSettings
    {
        public LayerMask groundDetectLayerMask;
        public ELRoi_CamController camController;
        public float movementSpeed = 2;
        public float jumpForce = 15;
        public float downwardForce = 0.75f;

    }

    [SerializeField]
    ElRoi_Animator animator;

    public LineBasedLedgeScanSettings lineBasedLedgeScanSettings = new LineBasedLedgeScanSettings();
    public MovementSettings movementSettings = new MovementSettings();

    [SerializeField]
    SkinnedMeshRenderer playerCharRenderer;//To determine the extents
    [SerializeField]
    InputTranslator inputTranslator;
    [SerializeField]
    LayerMask ledgeLayers;
    [SerializeField]
    float ledgeScanStartPercentage = 95;
    [SerializeField]
    LayerMask movementBlockerLayers;//We will check for collision with this layer(s) to see if the player will end up inside a geometry during this move
    [SerializeField]
    GameObject pivotReferenceObject;

    [SerializeField]
    float minimumLedgeHeight = 0.1f;
    [SerializeField]
    Vector3 handFitSize = new Vector3(0.075f, 0.025f, 0.075f);

    [SerializeField]
    float sideLeapAngleCheck = 80; //Only when the angle is between camera and angle is within this, ledge leap scans will be done

    Transform handReference;
    [SerializeField]
    GameObject handTarget; //The expected position of hand. Can also use a vector, which contains a local position of the hand instead.
    [SerializeField]
    GameObject bodyTarget; //The expected position of player
    [SerializeField]
    float climbSpeed = 5;
    [SerializeField]
    GameObject chest;
    [SerializeField]
    GameObject shoulder_r;
    [SerializeField]
    GameObject shoulder_l;

    [SerializeField]
    PlayerStates playerState;

    [SerializeField]
    GameObject handGizmo;
    [SerializeField]
    GameObject bodyGizmo;

    Vector3 bodyFitSize;
    Transform bodyReference;


    //ELRoi_CharController charController;
    LedgeDetails ledgeDetail;
    LedgeDetails prevLedgeDetail;
    Vector3 leapScanDirection;
    int leapScanRL;
    float leapScanOverrideStartDistance;

    //Only used to minimize number of calls to MoveToLEdge
    //Note that this is not the same as LedgeFound, this just says that a ledge scan was done this frame
    bool ledgeScanDone;
    float distanceToLedge;

    //********************************************************
    //Movement related fields
    //The velocity the player was at when initiating a jump
    //Used when jumping from a ledge, etc.,
    float jumpStartVelocity;
    public bool grounded;
    public bool ledgeMode;
    public bool jumping;
    public bool walking;
    Vector3 velocity;
    CharacterController controller;
    //********************************************************

    //********************************************************
    int ikDirection; //Activate IK for left or right hand
    //********************************************************

    //********************************************************
    //Test and debug purposes
    Vector3 capsuleCastPoint1;
    Vector3 capsuleCastPoint2;
    //********************************************************

    // Use this for initialization
    void Start()
    {
        //Can be modified to use any character controller
        //Used to get the radius
        controller = GetComponent<CharacterController>();

        bodyReference = new GameObject().transform;
        bodyReference.name = "Body";
        handReference = new GameObject().transform;
        handReference.name = "Hand";

        bodyFitSize = new Vector3(controller.radius * 2, controller.height, controller.radius * 2);
        playerState = PlayerStates.Normal;
        animator = GetComponent<ElRoi_Animator>();
        bodyGizmo.transform.localScale = bodyFitSize;

    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (ledgeDetail.ledgeFound)
        {
        }

        if (jumping)
        {
            if (IsLedgeScanWindow())
            {
                prevLedgeDetail = ledgeDetail;
                ledgeDetail = DetectLedge_LB(chest.transform,
                                                transform.forward,
                                                    lineBasedLedgeScanSettings.forwardScan_startDistance,
                                                        lineBasedLedgeScanSettings.forwardScan_endDistance,
                                                            lineBasedLedgeScanSettings.forwardScan_distanceStep,
                                                                lineBasedLedgeScanSettings.scan_Height, lineBasedLedgeScanSettings.scan_Depth/*(lineBasedLedgeScanSettings.scan_Height + 0.1f)*/, true, lineBasedLedgeScanSettings.forwardScan_PrecisionSteps);
                ledgeScanDone = true;

            }
        }

        if (ledgeMode)
        {
            if (playerState == PlayerStates.LedgeHanging || playerState == PlayerStates.PlateauHanging)
            {
                if (inputTranslator.jump_Pressed
                    && (
                        movementSettings.camController.angleBetweenPlayerAndCamera >= -sideLeapAngleCheck &&
                        movementSettings.camController.angleBetweenPlayerAndCamera <= sideLeapAngleCheck
                       )
                   )
                {

                    prevLedgeDetail = ledgeDetail;
                    //If jump is pressed when hanging on a ledge, jump in the direction of the mouse
                    if (inputTranslator.mousePos.x != 0 &&
                        inputTranslator.mousePos.y != 0)
                    {
                        leapScanDirection = new Vector3(inputTranslator.mousePos.x, inputTranslator.mousePos.y, 0.0f);
                        leapScanRL = (int)Mathf.Sign(inputTranslator.mousePos.x);
                        leapScanDirection = transform.rotation * leapScanDirection;
                        float angle;
                        angle = Vector3.SignedAngle(leapScanRL * transform.right, leapScanRL * transform.right + leapScanDirection, transform.forward);
                        angle = leapScanRL * angle;

                        if (angle <= lineBasedLedgeScanSettings.leapScan_thresholdAngle &&
                            angle >= -lineBasedLedgeScanSettings.leapScan_thresholdAngle)
                        {
                            //The jump distance for horizontal jump will be higher than the one for non horizontal jumps
                            leapScanOverrideStartDistance = lineBasedLedgeScanSettings.leapScan_startDistance;
                        }
                        else
                        {
                            leapScanOverrideStartDistance = lineBasedLedgeScanSettings.leapScan_thresholdBreachStartDistance;
                        }

                        if (leapScanRL > 0)
                        {
                            ledgeDetail = DetectLedgeToSide_LB(shoulder_r,
                                                                leapScanDirection,
                                                                leapScanOverrideStartDistance,
                                                                lineBasedLedgeScanSettings.leapScan_endDistance,
                                                                lineBasedLedgeScanSettings.leapScan_step,
                                                                    lineBasedLedgeScanSettings.leapScan_sweepStartAngle,
                                                                    lineBasedLedgeScanSettings.leapScan_sweepEndAngle,
                                                                    lineBasedLedgeScanSettings.leapScan_sweepStep,
                                                                        lineBasedLedgeScanSettings.leapScan_scanStartDistance,
                                                                        lineBasedLedgeScanSettings.leapScan_scanEndDistance,
                                                                        lineBasedLedgeScanSettings.leapScan_scanStep,
                                                                        lineBasedLedgeScanSettings.leapScan_precisionStep,
                                                                        lineBasedLedgeScanSettings.scan_Height,
                                                                        lineBasedLedgeScanSettings.scan_Depth,
                                                                        -1,
                                                                        angle);
                            ledgeScanDone = true;

                            //If a ledge has been detected, IK needs to update towards the right side
                            ikDirection = 1;

                        }
                        else
                        {
                            //</TBD>Temporary comment
                            //Debug.DrawRay(shoulder_l.transform.position, leapScanDirection.normalized, Color.blue, 3.0f);
                            //<TBD/>Temporary comment
                            ledgeDetail = DetectLedgeToSide_LB(shoulder_l,
                                                                -transform.right + leapScanDirection,
                                                                leapScanOverrideStartDistance,
                                                                lineBasedLedgeScanSettings.leapScan_endDistance,
                                                                lineBasedLedgeScanSettings.leapScan_step,
                                                                    lineBasedLedgeScanSettings.leapScan_sweepStartAngle,
                                                                    lineBasedLedgeScanSettings.leapScan_sweepEndAngle,
                                                                    lineBasedLedgeScanSettings.leapScan_sweepStep,
                                                                        lineBasedLedgeScanSettings.leapScan_scanStartDistance,
                                                                        lineBasedLedgeScanSettings.leapScan_scanEndDistance,
                                                                        lineBasedLedgeScanSettings.leapScan_scanStep,
                                                                        lineBasedLedgeScanSettings.leapScan_precisionStep,
                                                                        lineBasedLedgeScanSettings.scan_Height,
                                                                        lineBasedLedgeScanSettings.scan_Depth,
                                                                        1,
                                                                        angle);
                            ledgeScanDone = true;

                            //If a ledge has been detected, IK needs to update towards the left side
                            ikDirection = -1;
                        }
                    }
                }
                //While on ledge, player can move to the right/left or jump off the ledge
                else if (inputTranslator.rightLeftMovement > 0.0f)
                {
                    prevLedgeDetail = ledgeDetail;
                    //Scan to right, to move along the ledge

                    ledgeDetail = DetectLedgeToSide_LB(shoulder_r,
                                                        transform.right,
                                                            lineBasedLedgeScanSettings.sideScan_startDistance,
                                                            lineBasedLedgeScanSettings.sideScan_endDistance,
                                                            lineBasedLedgeScanSettings.sideScan_step,
                                                                lineBasedLedgeScanSettings.sideScan_sweepStartAngle,
                                                                lineBasedLedgeScanSettings.sideScan_sweepEndAngle,
                                                                lineBasedLedgeScanSettings.sideScan_sweepStep,
                                                                    lineBasedLedgeScanSettings.sideScan_scanStartDistance,
                                                                    lineBasedLedgeScanSettings.sideScan_scanEndDistance,
                                                                    lineBasedLedgeScanSettings.sideScan_scanStep,
                                                                    lineBasedLedgeScanSettings.sideScan_precisionStep,
                                                                    lineBasedLedgeScanSettings.scan_Height,
                                                                    lineBasedLedgeScanSettings.scan_Depth,
                                                                    -1);
                    ledgeScanDone = true;
                    //If a ledge has been detected, IK needs to update towards the right side
                    ikDirection = 1;
                }
                else if (inputTranslator.rightLeftMovement < 0.0f)
                {
                    prevLedgeDetail = ledgeDetail;
                    //Scan to left, to move along the ledge
                    //Scan to right, to move along the ledge
                    ledgeDetail = DetectLedgeToSide_LB(shoulder_l,
                                    -transform.right,
                                        lineBasedLedgeScanSettings.sideScan_startDistance,
                                        lineBasedLedgeScanSettings.sideScan_endDistance,
                                        lineBasedLedgeScanSettings.sideScan_step,
                                            lineBasedLedgeScanSettings.sideScan_sweepStartAngle,
                                            lineBasedLedgeScanSettings.sideScan_sweepEndAngle,
                                            lineBasedLedgeScanSettings.sideScan_sweepStep,
                                                lineBasedLedgeScanSettings.sideScan_scanStartDistance,
                                                lineBasedLedgeScanSettings.sideScan_scanEndDistance,
                                                lineBasedLedgeScanSettings.sideScan_scanStep,
                                                lineBasedLedgeScanSettings.sideScan_precisionStep,
                                                lineBasedLedgeScanSettings.scan_Height,
                                                lineBasedLedgeScanSettings.scan_Depth);
                    ledgeScanDone = true;
                    //If a ledge has been detected, IK needs to update towards the left side
                    ikDirection = -1;
                }
                else if (inputTranslator.frontBackMovement > 0)
                {
                    //When hanging from ledge and Forward button is pressed
                    //Initiate ledge climb
                    if (playerState == PlayerStates.PlateauHanging)
                    {
                        playerState = PlayerStates.PlateauClimbing;
                    }
                }
                if (inputTranslator.dropOffLedge_Pressed)
                {
                    //Drop off the ledge
                    if (playerState == PlayerStates.LedgeHanging ||
                        playerState == PlayerStates.PlateauHanging)
                    {
                        //Reset the current ledge Detail
                        ledgeDetail = new LedgeDetails();
                        playerState = PlayerStates.Normal;
                        TriggerNormalMode();
                    }
                }
            }
        }

        if (ledgeScanDone)
        {
            //********************************************************************************//
            //If a ledge is found, trigger movement to the identified point
            if (ledgeDetail.ledgeFound)
            {
                TriggerLedgeMode();
                if (ledgeDetail.ledgeType == LedgeType.Ledge)
                {
                    playerState = PlayerStates.LedgeClimbing;
                }
                else if (ledgeDetail.ledgeType == LedgeType.Plateau)
                {
                    playerState = PlayerStates.LedgeClimbing;
                }
                //}
            }
            else
            {
                ledgeDetail = prevLedgeDetail;
            }
            ledgeScanDone = false;
            //********************************************************************************//
        }

        if (playerState == PlayerStates.LedgeClimbing)
        {
            //Lerp the player toward the ledge
            if (ledgeDetail.ledgeType == LedgeType.Ledge)
            {
                MoveToLedge(ledgeDetail.ledgeLocation,
                                ledgeDetail.targetRotation,
                                    handTarget,
                                        PlayerStates.LedgeHanging);
            }
            else if (ledgeDetail.ledgeType == LedgeType.Plateau)
            {
                MoveToLedge(ledgeDetail.ledgeLocation,
                                ledgeDetail.targetRotation,
                                    handTarget,
                                        PlayerStates.PlateauHanging);
            }
        }
        else if (playerState == PlayerStates.PlateauClimbing)
        {
            //If forward is pressed while player is hanging off a plateau
            //Climb up the plateau
            {
                MoveToLedge(ledgeDetail.ledgeLocation,
                                ledgeDetail.targetRotation,
                                    bodyTarget,
                                        PlayerStates.Normal);
            }
        }

        HandleNormalMovement();
        Animate();
    }



    void MoveToLedge(
                        Vector3 p_ledgeTargetPosition, //The target position on the ledge
                            Quaternion p_ledgeTargetRotation,   //The target rotation on the ledge. To make sure player faces the wall, if any
                                GameObject p_offsetTarget,      //The player will be positioned on the ledge relative to this position
                                    PlayerStates p_finalState   //Once the player reaches the ledge, the player is set to be in this state
                        )
    {

        Vector3 playerTargetPosition;
        Quaternion playerTargetRotation;

        //Slowly lerp towards the ledge/plateau
        playerTargetPosition = p_ledgeTargetPosition - p_ledgeTargetRotation * p_offsetTarget.transform.localPosition;

        transform.position = Vector3.Lerp(transform.position,
                                                //localPosition to make sure player base location is at the offset distance
                                                //i.e., 
                                                //if the location identified is for Hand to rest (the hand needs to rest on the ledge)                                         
                                                //The movement is still being done in the body, w.r.t. hand
                                                //So we apply the Body -> Hand distance
                                                playerTargetPosition,
                                                    Time.deltaTime * climbSpeed);



        playerTargetRotation = p_ledgeTargetRotation;




        transform.rotation = Quaternion.Slerp(transform.rotation,
                                                    playerTargetRotation,
                                                        Time.deltaTime * climbSpeed); ;

        //How far from the target is the player
        distanceToLedge = Vector3.Distance(transform.position, playerTargetPosition);

        //If player is closer to the ledge/Plateau
        //SNAP to the target position
        if (distanceToLedge <= 0.1f)
        {
            //Debug.Log("Snapping to ledge");

            transform.position = playerTargetPosition;
            transform.rotation = playerTargetRotation;
            playerState = p_finalState;
            //If the player is on a plateau now
            //Restore normal movement
            if (playerState == PlayerStates.Normal)
            {
                ledgeMode = false;
                TriggerNormalMode();
                //Reset the ledge details, so that it is not processed in the next frame
                ledgeDetail = new LedgeDetails();
            }

            ikDirection = 0;
        }


    }


    LedgeDetails DetectLedgeToSide_LB(GameObject p_shoulder,
                                        Vector3 p_moveDirection,
                                            float p_startDistance,
                                            float p_endDistance,
                                            float p_distanceStep,
                                                        float p_sweepStartAngle,
                                                        float p_sweepEndAngle,
                                                        float p_sweepStep,
                                                            float p_sideScan_startDistance,
                                                            float p_sideScan_endDistance,
                                                            float p_sideScan_step,
                                                            float p_sideScan_precisionStep,
                                                                float p_scanHeight,
                                                                float p_scanDepth,
                                                                int p_dirMultiplier = 1,
                                                                float p_dirAngle = 0)
    {
        LedgeDetails ledgeDetail;


        for (float i = p_startDistance; i >= p_endDistance; i -= p_distanceStep)
        {
            for (float j = p_sweepStartAngle; j >= p_sweepEndAngle; j -= p_sweepStep)
            {
                pivotReferenceObject.transform.position = p_shoulder.transform.position;
                pivotReferenceObject.transform.rotation = p_shoulder.transform.rotation;

                pivotReferenceObject.transform.Rotate(0, -p_dirMultiplier * j, /*0*/-p_dirMultiplier * p_dirAngle);

                pivotReferenceObject.transform.position += (pivotReferenceObject.transform.right.normalized * i * -p_dirMultiplier);

                if (j == p_sweepStartAngle)
                {
                    Debug.DrawRay(p_shoulder.transform.position, (pivotReferenceObject.transform.position - p_shoulder.transform.position).normalized * i, Color.gray, 3.0f);

                }
                else if (j == p_sweepEndAngle)
                {
                    Debug.DrawRay(p_shoulder.transform.position, (pivotReferenceObject.transform.position - p_shoulder.transform.position).normalized * i, Color.gray, 3.0f);
                }
                else
                {
                    Debug.DrawRay(p_shoulder.transform.position, (pivotReferenceObject.transform.position - p_shoulder.transform.position).normalized * i, Color.gray, 3.0f);
                }
                ledgeDetail = new LedgeDetails();
                ledgeDetail = DetectLedge_LB(pivotReferenceObject.transform,
                                                pivotReferenceObject.transform.forward,
                                                    p_sideScan_startDistance,
                                                    p_sideScan_endDistance,
                                                    p_sideScan_step,
                                                        p_scanHeight,
                                                        p_scanDepth,
                                                        true,
                                                        p_sideScan_precisionStep
                                                                    );
                if (ledgeDetail.ledgeFound)
                {
                    return ledgeDetail;
                }

            }

        }

        return new LedgeDetails();
    }

    LedgeDetails DetectLedge_LB(Transform p_pivot,
                                    Vector3 p_scanDirection,
                                            float p_startDistance,
                                                float p_endDistance,
                                                    float p_distanceSteps,
                                                        float p_scanStartHeight,
                                                            float p_scanDepth,
                                                                bool p_forcePrecisionMode = false,
                                                                    float p_precisionSteps = 0.01f
                                                                    )
    {

        LedgeDetails ledgeDetail = new LedgeDetails();
        Vector3 rayStartPoint;
        RaycastHit hit;
        Vector3 overlapCheckPosition = p_pivot.position;


        for (float i = p_startDistance; i <= p_endDistance; i += p_distanceSteps)
        {
            rayStartPoint = p_pivot.transform.position + (p_scanDirection.normalized * i) + (Vector3.up * p_scanStartHeight);

            if (p_forcePrecisionMode)
            {
                Debug.DrawRay(rayStartPoint, Vector3.down * p_scanDepth, Color.blue, 3.0f);
            }
            else
            {
            }

            if (!p_forcePrecisionMode)
            {
                //</TBD>Temporary comment
                //Debug.DrawRay(rayStartPoint, Vector3.down * p_scanDepth, Color.red, 3.0f);
                //<TBD/>Temporary comment
            }


            if (Physics.Raycast(rayStartPoint, Vector3.down, out hit, p_scanDepth, ledgeLayers))
            {

                overlapCheckPosition.y = hit.point.y - (minimumLedgeHeight / 2.0f);
                ledgeDetail = new LedgeDetails();

                if (p_forcePrecisionMode)
                {
                    //if (p_forcePrecisionMode)
                    //{
                    //</TBD>Temporary comment
                    //Debug.DrawRay(rayStartPoint, Vector3.down * p_scanDepth, Color.black, 3.0f);
                    //<TBD/>Temporary comment
                    //}

                    //Force further scans to detect the correct edge
                    ledgeDetail = DetectLedge_LB(p_pivot, p_scanDirection, (i - p_distanceSteps), i, p_precisionSteps, p_scanStartHeight, p_scanDepth, false);
                    if (ledgeDetail.ledgeFound)
                    {
                        return ledgeDetail;
                    }
                }
                else
                {
                    //If the start point (pivot) is inside any object, do not scan
                    Debug.DrawRay(overlapCheckPosition, (overlapCheckPosition - p_pivot.transform.position), Color.red, 3.0f);
                    if (Physics.CheckSphere(overlapCheckPosition, 0.01f, movementBlockerLayers))
                    {
                        return ledgeDetail;
                    }

                    ledgeDetail = CheckLedge(hit,
                                                p_pivot.transform.position,
                                                    p_distanceSteps
                                                    ); //This is needed when doing capsule cast to see if player will fit the hanging space from the ledge, so that the cast doesnt fail because it happens too close to the ledge
                    if (ledgeDetail.ledgeFound)
                    {
                        return ledgeDetail;
                    }
                }


            }
        }
        return ledgeDetail;

    }


    LedgeDetails CheckLedge(RaycastHit p_hit,
                                Vector3 p_initialStartPoint,
                                    float p_precisionCorrection
                            )
    {
        LedgeDetails ledgeDetail = new LedgeDetails();
        Vector3 facingDirection = new Vector3();

        //The surface must be top facing (In this case it will mostly be top facing)
        if (p_hit.normal.y > 0.0f)
        {
            //</TBD>Additional checks to surface inclination can be done here<TBD/>

            //Find the direction to face
            facingDirection = FindFacingDirection(p_hit, p_initialStartPoint, transform.forward);


            //Check if player's entire body will fit that spot (Plateau)
            ledgeDetail = CheckFit(bodyReference, bodyFitSize, p_hit, facingDirection, LedgeType.Plateau, p_precisionCorrection);
            //Check if player's hand will fit that spot
            if (!ledgeDetail.ledgeFound)
            {
                ledgeDetail = CheckFit(handReference, handFitSize, p_hit, facingDirection, LedgeType.Ledge,
                                        p_precisionCorrection//Adding a slight distance to the capsule cast, so that it doesnt fail because its done very close to the ledge
                                        );
            }
            return ledgeDetail;
        }
        else
        {
            ledgeDetail.ledgeFound = false;
            return ledgeDetail;
        }
    }

    LedgeDetails CheckFit(Transform p_referenceObject,
                            Vector3 p_referenceObjectSize,
                                RaycastHit p_hit,
                                    Vector3 p_faceDirection,
                                        LedgeType p_ledgeType,
                                            float p_precisionCorrection = 0.0f
                                    )
    {
        LedgeDetails ledgeDetail = new LedgeDetails();
        RaycastHit[] intersectingColliders;
        Collider[] targetPositionCollisions = new Collider[0];
        Vector3 castCapsuleP1;
        Vector3 castCapsuleP2;

        Quaternion yRotation;
        Quaternion xzRotation;



        ledgeDetail.ledgeFound = false;

        //We are going to place the Hand/Body reference at the hit point
        //in the desired direction, and see if the hand/body will fit in the identified ledge
        p_referenceObject.rotation = Quaternion.identity;
        p_referenceObject.position = p_hit.point;

        //Forward will be rotated around Y, to face the expected direction
        //i.e., Rotate the reference object around Y to face the direction
        yRotation = Quaternion.FromToRotation(p_referenceObject.forward, p_faceDirection);
        p_referenceObject.rotation *= yRotation;

        //</TBD>Temporary comment
        //Debug.DrawRay(p_hit.point,
        //                p_faceDirection,
        //                    Color.yellow,
        //                        p_hit.normal.magnitude);
        //<TBD/>Temporary comment

        //Rotate the reference object around XZ to be aligned with the ledge/normal
        xzRotation = Quaternion.FromToRotation(p_referenceObject.up, p_hit.normal);
        p_referenceObject.rotation = xzRotation * p_referenceObject.rotation;

        //Move the aligned reference object slightly above the ledge
        //Adding 0.055f to make sure the box cast starts slightly above the identified ledge surface
        //This is to avoid collision of boxcast with the ledge itself
        p_referenceObject.transform.Translate(0, 0.055f + p_referenceObjectSize.y / 2, p_referenceObjectSize.z / 2);

        //Check if there is enough room for hand/player
        //If the below box cast (size of the hand/body) does not collide with anything, there is enough room for hand/player        
        intersectingColliders = Physics.BoxCastAll(
                                                    p_referenceObject.transform.position,
                                                    new Vector3(p_referenceObjectSize.x / 2, p_referenceObjectSize.y / 2, p_referenceObjectSize.z / 2),
                                                    p_referenceObject.transform.up,
                                                    p_referenceObject.rotation,
                                                    0.0f,
                                                    movementBlockerLayers
                                                   );
        handGizmo.transform.position = p_referenceObject.position;
        handGizmo.transform.rotation = p_referenceObject.rotation;
        if (p_ledgeType == LedgeType.Ledge)
        {

        }
        else
        {
            bodyGizmo.transform.position = p_referenceObject.position;
            bodyGizmo.transform.rotation = p_referenceObject.rotation;
        }

        if (intersectingColliders.Length == 0)
        {
            ledgeDetail.ledgeLocation = p_hit.point; //A ledge is found at this point
            ledgeDetail.targetRotation = Quaternion.identity;
            ledgeDetail.targetRotation = Quaternion.AngleAxis(yRotation.eulerAngles.y, Vector3.up);//Apply just the Y rotation to face player to the wall

            //Identify the capsule end points
            //i.e., if the player is going to be hanging from the ledge
            //will the player be colliding with anything
            castCapsuleP1 = (ledgeDetail.ledgeLocation - ledgeDetail.targetRotation * (handTarget.transform.localPosition + new Vector3(0, 0, p_precisionCorrection))) +
                                controller.center +
                                    controller.transform.up *
                                        -((controller.height * 0.5f) - controller.radius);
            castCapsuleP2 = castCapsuleP1 + controller.transform.up * (controller.height - (2 * controller.radius));

            capsuleCastPoint1 = castCapsuleP1;
            capsuleCastPoint2 = castCapsuleP2;

            if (!Physics.CheckCapsule(castCapsuleP1, castCapsuleP2, controller.radius, movementBlockerLayers))
            {
                ledgeDetail.ledgeFound = true; //A ledge has been found
                ledgeDetail.ledgeType = p_ledgeType;
                return ledgeDetail;
            }        
        }
        else
        {

        }

        return new LedgeDetails();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(capsuleCastPoint1, controller.radius);
        Gizmos.DrawWireSphere(capsuleCastPoint2, controller.radius);
    }

    Vector3 FindFacingDirection(RaycastHit p_hit, Vector3 p_initialStartPoint, Vector3 p_forward)
    {


        Vector3 startPoint = p_initialStartPoint;
        Vector3 endPoint = p_hit.point;
        RaycastHit hit;

        //We need to find a point slightly lower than the hit
        //So that we can raycast in that direction and find a face which is facing the player
        startPoint.y = p_hit.point.y - (minimumLedgeHeight / 2.0f);
        endPoint.y = startPoint.y;

        if (Physics.Raycast(startPoint,
                                (endPoint - startPoint),
                                    out hit,
                                        (endPoint - startPoint).magnitude,
                                            ledgeLayers
                                ))
        {
            Debug.DrawRay(hit.point,
                            hit.normal,
                                Color.cyan,
                                    hit.normal.magnitude);
            return -hit.normal;
        }
        return p_forward;

    }

    bool IsLedgeScanWindow()
    {
        //Ledge scan shouldnt start rightaway
        //When jumping, there should a delay before ledge scan starts
        //See if the character has lost certain amount of velocity beforestarting
        if (controller.velocity.y <= jumpStartVelocity * ledgeScanStartPercentage / 100
             && jumping)
        {
            return true;
        }

        return false;
    }
    //*****************************************************************************************
    void HandleNormalMovement()
    {
        IsGrounded();
        Vector3 charDefaultForward = transform.forward;
        Vector3 camForward = movementSettings.camController.transform.forward;
        Vector3 camRight = movementSettings.camController.transform.right;

        walking = false;

        if (!ledgeMode)
        {

            camForward.y = 0.0f;
            camForward.Normalize();

            camRight.y = 0.0f;
            camRight.Normalize();

            velocity = camForward * movementSettings.movementSpeed * inputTranslator.frontBackMovement +
                        camRight * movementSettings.movementSpeed * inputTranslator.rightLeftMovement;

            velocity = velocity.normalized;


            if (!grounded)
            {
                transform.forward = charDefaultForward; //To make sure character continue facing its current direction
            }
            else
            {
                if (inputTranslator.frontBackMovement == 0.0f &&
                    inputTranslator.rightLeftMovement == 0.0f)
                {
                    //If no button pressed for sideways movement
                    //To make sure character continue facing its current direction
                    transform.forward = charDefaultForward;
                }
                else
                {
                    //Else apply velocity based on the input
                    transform.forward = velocity;
                    walking = true;
                }

                if (inputTranslator.jump_Pressed)
                {
                    //Jump input works only when on ground
                    velocity.y = (movementSettings.jumpForce);
                    jumpStartVelocity = velocity.y;
                    jumping = true;
                }
            }
            velocity.y -= movementSettings.downwardForce;//Debug.Log("Downward Force");
            controller.Move(velocity * Time.deltaTime * movementSettings.movementSpeed);
        }


    }

    void IsGrounded()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 0.1f, movementSettings.groundDetectLayerMask))
        {
            grounded = true;
            jumping = false;//Debug.Log("Jump ended by Grounding");
        }
        else
        {
            grounded = false;
        }
    }

    void TriggerLedgeMode()
    {
        //Debug.Log("ledge mode triggered");
        ledgeMode = true;
        jumping = false; //Debug.Log("Jump ended by Ledge mounting");
        controller.enabled = false;
    }

    void TriggerNormalMode()
    {
        //Debug.Log("normal mode triggered");

        ledgeMode = false;
        jumping = false; //Debug.Log("Jump ended by Plateau mounting/Dropping off ledge");
        controller.enabled = true;

    }
    //*****************************************************************************************
    void Animate()
    {
        if (animator != null)
        {
            animator.jump = jumping;
        }

        if (playerState == PlayerStates.PlateauClimbing)
        {
            animator.plateauClimb = true;
        }

        animator.walk = walking;
        animator.currentDistanceToLedge = distanceToLedge;
        //When the player is in the process of climbing a ledge
        //Or jumping to a ledge
        //From another ledge,
        //IK hand placement needs to be updated
        if (
            (
                playerState == PlayerStates.LedgeClimbing
            )
                &&
            ledgeMode &&
            ikDirection != 0)
        {
            animator.InitiateClimbMode(ikDirection, prevLedgeDetail, ledgeDetail);

        }
        else if (
            (
                playerState == PlayerStates.PlateauClimbing
            )
                &&
            ledgeMode)
        {
            animator.InitiateClimbMode(ikDirection, prevLedgeDetail, ledgeDetail);

        }

        if (playerState != PlayerStates.LedgeClimbing &&
            playerState != PlayerStates.PlateauClimbing)
        {
            animator.TerminateClimbMode();
        }

        if (ledgeMode)
        {
            animator.InitiateLedgeMode(prevLedgeDetail, ledgeDetail);
        }
        else
        {
            animator.TerminateLedgeMode();
        }


    }

    private void OnValidate()
    {
        if (lineBasedLedgeScanSettings.forwardScan_PrecisionSteps < 0.0f ||
            lineBasedLedgeScanSettings.forwardScan_PrecisionSteps >= 0.1f)
        {
            Debug.Log("ForwardScan PrecisionSteps Recommended value between 0.0f (Exclusive) and 0.1f (Inclusive)");
        }
        if (lineBasedLedgeScanSettings.sideScan_precisionStep < 0.0f ||
            lineBasedLedgeScanSettings.sideScan_precisionStep >= 0.1f)
        {
            Debug.Log("SideScan PrecisionSteps Recommended value between 0.0f (Exclusive) and 0.1f (Inclusive)");
        }
        if (lineBasedLedgeScanSettings.leapScan_precisionStep < 0.0f ||
            lineBasedLedgeScanSettings.leapScan_precisionStep >= 0.1f)
        {
            Debug.Log("LeapScan PrecisionSteps Recommended value between 0.0f (Exclusive) and 0.1f (Inclusive)");
        }
    }

}
