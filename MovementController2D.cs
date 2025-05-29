using System.Collections;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class MovementController2D : MonoBehaviour
{
    [SerializeField] float normalSpeed;
    [SerializeField] float gravity;

    [Header("Jumping")]
    [SerializeField] float airSpeed;
    [SerializeField] float jumpForce;
    [SerializeField] LayerMask groundMask;
    [SerializeField] Collider2D feetCollider;
    [Tooltip("Time window in which a new jump input will try to be processed")]
    [SerializeField] float jumpPressedTolerance;
    [Tooltip("Time window in which the player will jump even if it is not grounded")]
    [SerializeField] float groundedTolerance;
    [Tooltip("Time limit for the player to separate from the floor. If the limit is reached, the jump will be canceled")]
    [SerializeField] float startJumpTimeout;

    [Header("Climbing")]
    [SerializeField] float climbSpeed;

    [Header("Slope Movement")]
    [SerializeField] float slopeSpeed;
    [SerializeField] float slopeCheckHorizontalDistance;
    [SerializeField] float slopeCheckVecticalDistance;
    [Tooltip("Minimum terrain inclination to be considered a slope")]
    [SerializeField] float minSlopeAngle;
    [Tooltip("Maximum slope inclination to be walkable")]
    [SerializeField][Range(0, 80)] float maxSlopeAngle;
    [SerializeField] Transform slopeCheckPoint;

    [Header("Movement Responsiveness")]


    [Header("Physics Materials")]
    [SerializeField] PhysicsMaterial2D noFrictionMaterial;
    [SerializeField] PhysicsMaterial2D fullFrictionMaterial;

    //Components
    Rigidbody2D rigidBody;
    ClimbCheck climbCheck;
    Animator animator;

    //Flags
    bool jumping;
    bool startJumping;
    bool isClimbing;

    //State variables
    Vector2 input;
    float lookingDirection = 1;
    Vector2 velocity;

    //Timers
    float timeSinceLastJumpInput = Mathf.Infinity;
    float timeSinceLastGrounded = Mathf.Infinity;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        feetCollider = transform.GetChild(1).GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        climbCheck = GetComponentInChildren<ClimbCheck>();
    }
    private void FixedUpdate()
    {
        rigidBody.velocity = velocity;
        Debug.DrawRay(transform.position, rigidBody.velocity.normalized, Color.blue);
    }

    private void Update()
    {
        input = GetMovementInput();

        FaceDirection(input.x);

        //update timers
        timeSinceLastJumpInput += Time.deltaTime;
        timeSinceLastGrounded += Time.deltaTime;
        if (JumpInput())
        {
            timeSinceLastJumpInput = 0;
        }

        //if climbing
        isClimbing = CheckClimbState();
        if (isClimbing)
        {
            //allow movement on every direction
            velocity = input * climbSpeed;
        }
        //else if player on the ground
        else if (IsGrounded())
        {
            timeSinceLastGrounded = 0;
            jumping = false;

            //if there's a slope
            if (CheckSlope(out Vector2 slopeDirection))
            {
                //if the slope is walkable
                if (GetSlopeAngle(slopeDirection) < maxSlopeAngle)
                {
                    //if player is not moving
                    if (input.x == 0)
                    {
                        //apply friction so the player does not slip on the slope
                        ApplyFriction(true);
                    }
                    else
                    {
                        //remove friction so the player can move
                        ApplyFriction(false);
                    }
                    //move in the direction of the slope
                    velocity = slopeDirection * -input.x * slopeSpeed;
                }
                //if the slope is not walkable
                else
                {
                    //remove friction so the player slips down the slope
                    ApplyFriction(false);
                    velocity = rigidBody.velocity;
                }
            }
            else //if there's no slope
            {
                //remove friction so the player can move
                ApplyFriction(false);
                //move horizontally
                velocity = Vector2.right * input.x * normalSpeed;
            }
        }
        else //if player not grounded
        {
            velocity = new Vector2(input.x * airSpeed, rigidBody.velocity.y);
        }

        //if player was recently grounded
        if (timeSinceLastGrounded < groundedTolerance)
        {
            //if player recently pressed jump
            if (timeSinceLastJumpInput < jumpPressedTolerance && !jumping)
            {
                Jump();
            }
        }
    }


    private void LateUpdate()
    {
        animator.SetBool("Climbing", isClimbing);
        animator.SetBool("Moving", Mathf.Abs(rigidBody.velocity.x) > 0.1f);
        animator.SetBool("Jumping", jumping);
        animator.SetFloat("jumpYVelocity", rigidBody.velocity.y);
        animator.SetBool("Grounded", IsGrounded());
    }

    private Vector2 GetMovementInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void FaceDirection(float xInput)
    {
        if (xInput != 0)
        {
            lookingDirection = xInput;
        }
        transform.localScale = new Vector2(lookingDirection, transform.localScale.y);
    }

    private bool CheckClimbState()
    {
        //if there's an object the player can climb
        if (climbCheck.currentObjectToClimb != null)
        {
            //if player is climbing but touches the ground and moves horizontally
            if (isClimbing && IsGrounded() && input.x != 0)
            {
                //stop climbing
                ExitClimbState();
                return false;
            }
            //else if player is not climbing and moves vertically
            else if (!isClimbing && input.y != 0)
            {
                //start climbing
                EnterClimbState();
                return true;
            }
            else
            {
                return isClimbing;
            }
        }
        //else if there's no object to climb
        else
        {
            //stop climbing if climbing
            if (isClimbing) ExitClimbState();
            return false;
        }
    }

    private void EnterClimbState()
    {
        rigidBody.gravityScale = 0;
        climbCheck.EnterClimbObject();
    }

    private void ExitClimbState()
    {
        ResetVelocity();
        rigidBody.gravityScale = gravity;
        climbCheck.ExitClimbObject();
    }

    private void ResetVelocity()
    {
        rigidBody.velocity = Vector2.zero;
    }

    private bool IsGrounded()
    {
        //if the player has started a jump
        if (startJumping)
        {
            //the player is not grounded
            return false;
        }
        else
        {
            //return whether feet are touching the floor 
            return feetCollider.IsTouchingLayers(groundMask);
        }
    }

    private bool CheckSlope(out Vector2 slopeDirection)
    {
        slopeDirection = Vector2.left;

        if (CheckSlopeOnSides(out Vector2 sideSlopeDirection))
        {
            slopeDirection = sideSlopeDirection;
        }
        else if (CheckSlopeBelow(out Vector2 belowSlopeDirection))
        {
            slopeDirection = belowSlopeDirection;
        }
        else
        {
            return false;
        }

        float slopeAngle = GetSlopeAngle(slopeDirection);
        if (minSlopeAngle <= slopeAngle && slopeAngle < 90 - minSlopeAngle)
        {
            return true;
        }

        return false;
    }

    private bool CheckSlopeOnSides(out Vector2 slopeDirection)
    {
        slopeDirection = Vector2.left;
        //check if there's a slope in the front or the back
        RaycastHit2D frontHit = Physics2D.Raycast(slopeCheckPoint.position,
            Vector2.right * lookingDirection,
            slopeCheckHorizontalDistance,
            groundMask);

        RaycastHit2D backHit = Physics2D.Raycast(slopeCheckPoint.position,
            -Vector2.right * lookingDirection,
            slopeCheckHorizontalDistance,
            groundMask);

        if (frontHit)
        {
            slopeDirection = Vector2.Perpendicular(frontHit.normal);
            Debug.DrawRay(slopeCheckPoint.position, Vector2.right * lookingDirection * slopeCheckHorizontalDistance, Color.green);
            return true;
        }
        else if (backHit)
        {
            slopeDirection = Vector2.Perpendicular(backHit.normal);
            Debug.DrawRay(slopeCheckPoint.position, -Vector2.right * lookingDirection * slopeCheckHorizontalDistance, Color.green);
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool CheckSlopeBelow(out Vector2 slopeDirection)
    {
        slopeDirection = Vector2.left;

        //check if there's a slope below the player
        Vector2 origin = (Vector2)slopeCheckPoint.position + new Vector2(slopeCheckHorizontalDistance * 0.75f * lookingDirection, 0);
        RaycastHit2D downHit = Physics2D.Raycast(
            origin,
            Vector2.down,
            slopeCheckVecticalDistance,
            groundMask);

        if (downHit)
        {
            slopeDirection = Vector2.Perpendicular(downHit.normal);
            float downwardSlopeAngle = Mathf.Abs(Vector2.Angle(Vector2.up, downHit.normal));
            if (downwardSlopeAngle < 5f)
            {
                return false;
            }
            else
            {
                Debug.DrawRay(origin, Vector2.down * slopeCheckVecticalDistance, Color.red);
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    private float GetSlopeAngle(Vector2 slopeDirection)
    {
        Vector2 correctedDirection = new Vector2(slopeDirection.x, Mathf.Abs(slopeDirection.y));
        return 90 - Vector2.Angle(Vector2.up, correctedDirection);
    }


    private void ApplyFriction(bool enable)
    {
        if (enable)
        {
            rigidBody.sharedMaterial = fullFrictionMaterial;
        }
        else
        {
            rigidBody.sharedMaterial = noFrictionMaterial;
        }
    }

    private static bool JumpInput()
    {
        return Input.GetKeyDown(KeyCode.Space);

    }

    private void Jump()
    {
        jumping = true;
        startJumping = true;

        ResetVelocity();
        ApplyFriction(false);
        rigidBody.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);

        velocity = new Vector2(input.x * airSpeed, rigidBody.velocity.y);
        StartCoroutine(StartJumping());
    }

    private IEnumerator StartJumping()
    {
        float startJumpTimer = 0;
        yield return new WaitUntil(() =>
        {
            startJumpTimer += Time.deltaTime;
            return startJumpTimeout < startJumpTimer || !feetCollider.IsTouchingLayers(groundMask);
        });
        startJumping = false;
    }

    private void OnEnable()
    {
        ApplyFriction(false);
    }

    private void OnDisable()
    {
        ResetVelocity();
        ApplyFriction(true);
    }
}
