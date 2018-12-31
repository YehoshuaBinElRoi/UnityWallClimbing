using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;

public class ElRoi_Animator : MonoBehaviour
{
    Animator animator;
    AnimatorController controller;
    [SerializeField]
    AvatarMask excludePartsFromIK;


    public bool idle;
    public bool jump;
    public bool walk;
    public bool plateauClimb;


    public int direction; //1-Right,-1 is Left
    public float currentDistanceToLedge;

    //When climbing to right, left hand will still remain in older position, right hand will point towards the identified ledge position
    //And vice versa
    public LedgeDetails initialLedge;
    public LedgeDetails targetLedge;

    bool climbMode;
    bool ledgeMode;


    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>();
        controller = (AnimatorController)animator.runtimeAnimatorController;
        animator.SetTrigger("Idle");
        animator.SetLayerWeight(animator.GetLayerIndex("RegularAnimations"), 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (animator.GetBool("Jump") != jump)
        {
            animator.SetBool("Jump", jump);
        }
        if (animator.GetBool("Walk") != walk)
        {
            animator.SetBool("Walk", walk);
        }


    }




    private void OnAnimatorIK(int layerIndex)
    {
        if (climbMode)
        {
            ClimbingIK();
        }
        else if (ledgeMode)
        {
            ClimbingIK(true);
        }
    }

    public void InitiateLedgeMode(LedgeDetails p_prevLedge, LedgeDetails p_nextLedge)
    {
        if (!ledgeMode)
        {
            //Debug.Log("Initiate Ledge");
            direction = 0;
            initialLedge = p_prevLedge;
            targetLedge = p_nextLedge;
            ledgeMode = true;
            //blockIK = false;
            plateauClimb = false;
            animator.SetLayerWeight(animator.GetLayerIndex("RightHandIK"), 1);
            animator.SetLayerWeight(animator.GetLayerIndex("LeftHandIK"), 1);
            animator.SetLayerWeight(animator.GetLayerIndex("RegularAnimations"), 0);
            animator.SetLayerWeight(animator.GetLayerIndex("DoNothing"), 1);
            //ClimbingIK(true);

        }


    }

    public void InitiateClimbMode(int p_direction, LedgeDetails p_prevLedge, LedgeDetails p_nextLedge)
    {
        
        if (!climbMode)
        {
            //Time.timeScale = 0.1f;

            //Debug.Log("Initiate Climb");

            climbMode = true;
            direction = p_direction;
            initialLedge = p_prevLedge;
            targetLedge = p_nextLedge;

            if (plateauClimb)
            {
                //Debug.Log("Plateau Climb");
                animator.SetLayerWeight(animator.GetLayerIndex("RightHandIK"), 0);
                animator.SetLayerWeight(animator.GetLayerIndex("LeftHandIK"), 0);
                animator.SetLayerWeight(animator.GetLayerIndex("ClimbUp"), 1);
            }
            else
            {
                //Debug.Log("Ledge Climb");
                animator.SetLayerWeight(animator.GetLayerIndex("RightHandIK"), 1);
                animator.SetLayerWeight(animator.GetLayerIndex("LeftHandIK"), 1);
            }
        }

    }

    public void TerminateClimbMode()
    {

        if (climbMode)
        {
            //Time.timeScale = 1.0f;
            //Debug.Log("Terminate Climb");

            climbMode = false;
            ClimbingIK(true);

            if (plateauClimb)
            {
                Debug.Log("Plateau Climb End");
                animator.SetLayerWeight(animator.GetLayerIndex("RightHandIK"), 1);
                animator.SetLayerWeight(animator.GetLayerIndex("LeftHandIK"), 1);
                animator.SetLayerWeight(animator.GetLayerIndex("ClimbUp"), 0);
            }
            else
            {
                Debug.Log("Ledge Climb End");
                animator.SetLayerWeight(animator.GetLayerIndex("RightHandIK"), 0);
                animator.SetLayerWeight(animator.GetLayerIndex("LeftHandIK"), 0);

            }
        }
    }




    public void TerminateLedgeMode()
    {
        if (ledgeMode)
        {
            //Debug.Log("Terminate Ledge");
            ledgeMode = false;
            animator.SetTrigger("Idle");

            animator.SetLayerWeight(animator.GetLayerIndex("RightHandIK"), 0);
            animator.SetLayerWeight(animator.GetLayerIndex("LeftHandIK"), 0);
            animator.SetLayerWeight(animator.GetLayerIndex("RegularAnimations"), 1);
            animator.SetLayerWeight(animator.GetLayerIndex("DoNothing"), 0);
        }
    }

    void ClimbingIK(bool p_end = false)
    {

        float sourceHandWeight = 1.0f;
        float destinationHandWeight = 1.0f;
        float fullDistanceToLedge;

        Vector3 position;
        Quaternion rotation;
        float lerp;
        float distanceProportion;

        if (plateauClimb)
        {
            
            

            //distanceProportion = 5.0f * currentDistanceToLedge / fullDistanceToLedge;
            //if (distanceProportion < 0.1f)
            //{
            //    distanceProportion = 1;
            //}

            //Debug.Log(currentDistanceToLedge + "," + fullDistanceToLedge + ","+distanceProportion);

            if (currentDistanceToLedge > 1.0f)
            {
                Debug.Log("Plateau Climbing");
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, targetLedge.ledgeLocation + (transform.right * 0.05f));

                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKRotation(AvatarIKGoal.RightHand, targetLedge.targetRotation);

                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, targetLedge.ledgeLocation + (-transform.right * 0.05f));

                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, targetLedge.targetRotation);
            }

            if (currentDistanceToLedge < 1.0f &&
                     currentDistanceToLedge > 0.25f
                    )
            {
                Debug.Log("Knee IK");
                animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 1.0f);
                animator.SetIKHintPosition(AvatarIKHint.RightKnee, targetLedge.ledgeLocation + (transform.right * 0.5f));

                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                animator.SetIKPosition(AvatarIKGoal.RightFoot, targetLedge.ledgeLocation + (transform.right * 0.5f) + (-transform.forward * 0.3f));

                animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 1.0f);
                animator.SetIKHintPosition(AvatarIKHint.LeftKnee, targetLedge.ledgeLocation + (transform.right * 0.5f));

                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftFoot, targetLedge.ledgeLocation + (-transform.right * 0.5f) + (-transform.forward * 0.3f));

            }
        }
        else
        {
            fullDistanceToLedge = Vector3.Distance(
                                                    initialLedge.ledgeLocation,
                                                    targetLedge.ledgeLocation
                                                    );

            if (p_end)
            {
                sourceHandWeight = 1;
                destinationHandWeight = 1;
            }
            else
            {
                sourceHandWeight = 1 - (currentDistanceToLedge / fullDistanceToLedge);

                if (sourceHandWeight <= 0.1f)
                {
                    sourceHandWeight = 1;
                }

                destinationHandWeight = currentDistanceToLedge / fullDistanceToLedge;

                if (destinationHandWeight <= 0.1f)
                {
                    destinationHandWeight = 1;
                }

            }

            if (direction == 1)
            {


                //Player moving towards right
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, destinationHandWeight);//Must be a proportion of current position to target position distance
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, targetLedge.ledgeLocation);
                animator.SetIKRotation(AvatarIKGoal.RightHand, targetLedge.targetRotation);

                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, sourceHandWeight);//Must be a proportion of current position to target position distance
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, initialLedge.ledgeLocation);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, initialLedge.targetRotation);

            }
            else if (direction == -1)
            {

                //Player moving towards left
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, destinationHandWeight);//Must be a proportion of current position to target position distance
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, targetLedge.ledgeLocation);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, targetLedge.targetRotation);

                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, sourceHandWeight);//Must be a proportion of current position to target position distance
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, initialLedge.ledgeLocation);
                animator.SetIKRotation(AvatarIKGoal.RightHand, initialLedge.targetRotation);

                //Debug.Log(sourceHandWeight + "," + destinationHandWeight);
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                animator.SetIKPosition(AvatarIKGoal.RightHand, targetLedge.ledgeLocation);
                animator.SetIKRotation(AvatarIKGoal.RightHand, targetLedge.targetRotation);

                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, targetLedge.ledgeLocation);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, targetLedge.targetRotation);
            }
        }
    }
}
