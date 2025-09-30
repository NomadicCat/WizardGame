using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using Unity.VisualScripting;


public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch, Slide
}

public struct CharacterState
{
    public bool Grounded;

    public Stance Stance;

    public Vector3 Velocity;

    public Vector3 Acceleration;
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
    public bool Attack;


}


public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Camera playerCamera;
    [Space]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;

    [SerializeField] private float jumpSpeed = 20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float slideStartSpeed = 25f;//how fast slide start
    [SerializeField] private float slideEndSpeed = 15f; //lowest speed can have and still slide
    [SerializeField] private float slideFriction = 0.8f;//rate player lose slide speed
    [SerializeField] private float slideSteerAccelleration = 5f;//how strong steering force
    [SerializeField] private float slideGravity = -90f;


    [Space]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;
    [Space]
    [Header("FireBall Settings")]
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackKnockbackRadius = 20f;
    [SerializeField] private float attackKnockbackPower = 20f;



    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;


    //private Stance _stance; 
    private Quaternion _reqestedRotation;
    private Vector3 _reqestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private bool _requestedCrouchInAir;

    private float _timeSinceUngrounded;
    private float _timeSinceJumpRequest;
    private bool _ungroundedDueToJump;

    private bool _requestedAttack;
    private float _timeSinceLastAttack;




    private Collider[] _uncrouchOverLapResults;


    public void Initialize()
    {
        _state.Stance = Stance.Stand;
        _lastState = _state;
        _uncrouchOverLapResults = new Collider[8];
        //_stance = Stance.Stand;
        motor.CharacterController = this;
        _timeSinceLastAttack = 0;

    }


    public void UpdateInput(CharacterInput input)
    {
        _reqestedRotation = input.Rotation;

        //take 2d input vectir abd create 3d movement vector on xz plane
        _reqestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        //clamp
        _reqestedMovement = Vector3.ClampMagnitude(_reqestedMovement, 1f);
        //Orient the input so its relative to the dir player face
        _reqestedMovement = input.Rotation * _reqestedMovement;

        var wasRequestingJump = _requestedJump;
        _requestedJump = _requestedJump || input.Jump;
        if(_requestedJump && !wasRequestingJump)
        {
            _timeSinceJumpRequest = 0f;
        }

        _requestedSustainedJump = input.JumpSustain;

        var wasRequestingCroutch = _requestedCrouch;
        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            //CrouchInput.Crouch => true, for holding
            //CrouchInput.UnCrouch => false for releasing
           _ => _requestedCrouch

        };
        if(_requestedCrouch && !wasRequestingCroutch)
        {
            _requestedCrouchInAir = !_state.Grounded;
        }else if(!_requestedCrouch && wasRequestingCroutch)
        {
            _requestedCrouchInAir = false;
        }

        _requestedAttack = _requestedAttack || input.Attack;




    }


    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        var cameraTargetHeight = currentHeight *
            (
            _state.Stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight

            );
        var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);


        cameraTarget.localPosition = Vector3.Lerp
            (
                a:cameraTarget.localPosition,
                b: new Vector3(0f,cameraTargetHeight, 0f),
                t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime) //1f - Mathf.Exp(- to help fram independent
            );
        //root.localScale = rootTargetScale;
        root.localScale = Vector3.Lerp
            (
                a: root.localScale,
                b: rootTargetScale,
                t: 1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
            );

    }



    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        _state.Acceleration = Vector3.zero;
        //Debug.Log($"Character State - Grounded: {_state.Grounded}, Stance: {_state.Stance}");
        if (motor.GroundingStatus.IsStableOnGround)
        {
            _ungroundedDueToJump = false;
            _timeSinceUngrounded = 0f;
            var groundedMovement = motor.GetDirectionTangentToSurface
            (
                direction: _reqestedMovement,
                surfaceNormal: motor.GroundingStatus.GroundNormal
            ) * _reqestedMovement.magnitude;

            //start slideing
            {
                var moving = groundedMovement.sqrMagnitude > 0f;
                var crouching = _state.Stance is Stance.Crouch;
                var wasStanding = _lastState.Stance is Stance.Stand;
                var wasInAir = !_lastState.Grounded;
                if( moving && crouching && (wasStanding || wasInAir))
                {

                    //Debug.DrawRay(transform.position, currentVelocity, Color.red, 5f);
                    //Debug.DrawRay(transform.position, _lastState.Velocity, Color.green, 5f);
                    _state.Stance = Stance.Slide;
                    //when landing on stable ground the character motor projects the velocity onto a flat plane
                    //see: kinematicCharacterMotor.HandleVelocityProjection()
                    //in this case we wamt the player to slide
                    //reproject the last frame (falling) velocity onto the ground normal to slide
                    if (wasInAir)
                    {
                        currentVelocity = Vector3.ProjectOnPlane
                            (
                                vector: _lastState.Velocity,
                                planeNormal: motor.GroundingStatus.GroundNormal
                            );

                    }
                    var effectiveSlideStartSpeed = slideStartSpeed;
                    if(!_lastState.Grounded && !_requestedCrouchInAir)
                    {
                        effectiveSlideStartSpeed = 0f;
                        _requestedCrouchInAir = false;
                    }
                    var slideSpeed = Mathf.Max(effectiveSlideStartSpeed, currentVelocity.magnitude);
                    currentVelocity = motor.GetDirectionTangentToSurface
                        (
                        direction: currentVelocity,
                        surfaceNormal: motor.GroundingStatus.GroundNormal
                        ) * slideSpeed;

                    //Debug.DrawRay(transform.position, currentVelocity, Color.black, 5f);
                }

            }




            //move
            if(_state.Stance is Stance.Stand or Stance.Crouch) 
            {




                var speed = _state.Stance is Stance.Stand
                    ? walkSpeed
                    : crouchSpeed;

                var response = _state.Stance is Stance.Stand
                    ? walkResponse
                    : crouchResponse;


                //smooth moving
                var targetVelocity = groundedMovement * speed;

                var moveVelocity = Vector3.Lerp
                (
                    a: currentVelocity,
                    b: targetVelocity,
                    t: 1f - Mathf.Exp(-response * deltaTime)
                );

                _state.Acceleration = (moveVelocity - currentVelocity) / deltaTime;
                currentVelocity = moveVelocity;

            }

            else //continue sliding
            {





                //friction
                currentVelocity -= currentVelocity * (slideFriction * deltaTime);

                //steer
                {
                    

                    //slope
                    var force = Vector3.ProjectOnPlane
                        (
                            vector: -motor.CharacterUp,
                            planeNormal: motor.GroundingStatus.GroundNormal

                        ) * slideGravity;

                    currentVelocity -= force * deltaTime;


                    if (groundedMovement.sqrMagnitude > 0f)
                    {
                        //target velocity is the player's movement direction, at the current speed
                        var currentSpeed = currentVelocity.magnitude;
                        var targetVelocity = groundedMovement * currentSpeed;
                        var steerVelocity = currentVelocity;
                        var steerForce = (targetVelocity - steerVelocity) * slideSteerAccelleration * deltaTime;

                        //steer but dont speed up due to direct input
                        steerVelocity += steerForce;
                        steerVelocity = Vector3.ClampMagnitude(steerVelocity, currentSpeed);

                        _state.Acceleration = (steerVelocity - currentVelocity) / deltaTime;
                        currentVelocity = steerVelocity;

                        /*var currentSpeed = currentVelocity.magnitude;//get the current speed before any steering modifications
                        var currentDirection = currentVelocity.normalized;//get the normalized direction of current velocity
                        var targetDirection = groundedMovement.normalized;//target direction is the player's movement input
                        var steerDirection = Vector3.Slerp(currentDirection, targetDirection, slideSteerAccelleration * deltaTime);//calculate steering force as a cross product between current and target 
                        currentVelocity = steerDirection * currentSpeed;//apply the new direction while maintaining current speed*/
                    }



                    // Inside the "continue sliding" branch, after you know you're grounded
                    {
                        //// Frame-rate independent friction
                        //float frictionDecay = Mathf.Exp(-slideFriction * deltaTime);
                        //currentVelocity *= frictionDecay;

                        //// Acceleration along slope (use a positive accel value)
                        //Vector3 groundNormal = motor.GroundingStatus.GroundNormal;
                        //Vector3 downslopeDir = Vector3.ProjectOnPlane(-motor.CharacterUp, groundNormal);
                        //if (downslopeDir.sqrMagnitude > 1e-6f)
                        //{
                        //    downslopeDir.Normalize();
                        //    // slideGravity should be a positive acceleration magnitude
                        //    float slideAccel = Mathf.Abs(slideGravity);
                        //    currentVelocity += downslopeDir * slideAccel * deltaTime;
                        //}

                        //// Steering without changing speed
                        //if (groundedMovement.sqrMagnitude > 1e-6f && currentVelocity.sqrMagnitude > 1e-6f)
                        //{
                        //    float currentSpeed = currentVelocity.magnitude;
                        //    Vector3 currentDir = currentVelocity / currentSpeed;
                        //    Vector3 targetDir = groundedMovement.normalized;

                        //    float steerT = Mathf.Clamp01(slideSteerAccelleration * deltaTime);
                        //    Vector3 steeredDir = Vector3.Slerp(currentDir, targetDir, steerT);

                        //    // Keep motion tangent to ground after steering
                        //    steeredDir = Vector3.ProjectOnPlane(steeredDir, groundNormal).normalized;
                        //    currentVelocity = steeredDir * currentSpeed;
                        //}

                        //// Final safety: keep velocity tangent to ground
                        //if (currentVelocity.sqrMagnitude > 1e-6f)
                        //{
                        //    float speed = currentVelocity.magnitude;
                        //    Vector3 dir = Vector3.ProjectOnPlane(currentVelocity, groundNormal).normalized;
                        //    currentVelocity = dir * speed;
                        //}

                        //// Stop condition
                        //if (currentVelocity.magnitude < slideEndSpeed)
                        //    _state.Stance = Stance.Crouch;
                    }

                }

                //stop
                if(currentVelocity.magnitude < slideEndSpeed)
                {
                    _state.Stance = Stance.Crouch;
                }

            }




        }
        else //in the air
        {
            _timeSinceUngrounded += deltaTime;
            if(_reqestedMovement.sqrMagnitude > 0f)
            {

                var planarMovement = Vector3.ProjectOnPlane
                    (
                        vector:_reqestedMovement,
                        planeNormal: motor.CharacterUp
                    ) * _reqestedMovement.magnitude;

                //current velocity on moment plane
                var currentPlanarVelocity = Vector3.ProjectOnPlane
                    (
                        vector: currentVelocity,
                        planeNormal: motor.CharacterUp
                    
                    );

                //calculate movement force
                var movementForce = planarMovement * airAcceleration * deltaTime;

                //if moving slower than max  air speed, treat movement force as steer
                if(currentPlanarVelocity.magnitude < airSpeed)
                {
                    //add it to the current planar velopcity for a target celocity
                    var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                    //limit target velocity to air speed
                    targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);
                    //steer towards current velocity
                    currentVelocity += targetPlanarVelocity - currentPlanarVelocity;
                }
                else if(Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                {
                    var constrainedMovementForce = Vector3.ProjectOnPlane
                        (
                            vector: movementForce,
                            planeNormal: currentPlanarVelocity.normalized
                        );
                    movementForce = constrainedMovementForce;
                }


                //prevent air climb on steep slope
                if (motor.GroundingStatus.FoundAnyGround)
                {
                    //if moving in same dir as the result velocity
                    if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                    {
                        //calc obsturction normal
                        var obstructionNormal = Vector3.Cross
                            (
                                motor.CharacterUp,
                                Vector3.Cross
                                (
                                    motor.CharacterUp,
                                    motor.GroundingStatus.GroundNormal
                                )

                            ).normalized;

                        //project movement force onto obstruction plane
                        movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal );

                    }
                }



                currentVelocity += movementForce;

            }

            //gravity
            var effectiveGravity = gravity;
            var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            if (_requestedSustainedJump && verticalSpeed > 0f)
            {
                effectiveGravity *= jumpSustainGravity; 
            }
            currentVelocity += motor.CharacterUp * effectiveGravity * deltaTime;  //gravity changed to effective gravity for sustained jump
        }

        if (_requestedJump )
        {
            var grounded = motor.GroundingStatus.IsStableOnGround;
            var canCoyoteJump = _timeSinceUngrounded < coyoteTime && !_ungroundedDueToJump;
            if (grounded || canCoyoteJump)
            {
                print("jump");
                _requestedJump = false; //unset jump request
                _requestedCrouch = false;//request character uncroutch
                _requestedCrouchInAir = false;

                //unstick from ground
                motor.ForceUnground(time: 0f);
                _ungroundedDueToJump = true;

                //add jump force
                //currentVelocity += motor.CharacterUp * jumpSpeed;
                //min vert speed to jump speed
                //currentVelocity.y = Mathf.Max(currentVelocity.y, jumpSpeed);
                var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
                currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);

            }
            else
            {
                _timeSinceJumpRequest += deltaTime;
                
                //defer the jump request until coyote time has passed
                var canJumpLater = _timeSinceJumpRequest < coyoteTime;
                //deny jump request
                //_requestedJump = false;
                _requestedJump = canJumpLater;
            }
        }

        if (_requestedAttack && _timeSinceLastAttack < attackInterval)
        {
            _requestedAttack = false;
            float maxDistance = 100f;
            RaycastHit hitInfo;
            Debug.Log("at");
            //Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward, Color.red, 1f);
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            int playerLayer = LayerMask.NameToLayer("Player");
            int ignorePlayerMask = ~(1 << playerLayer);
            if (Physics.Raycast(ray, out hitInfo, maxDistance,ignorePlayerMask))
            {
                float distance = hitInfo.distance;
                Debug.Log($"Hit object {hitInfo.collider.name} at distance {distance}");
                if(distance <= attackKnockbackRadius)
                {
                    motor.ForceUnground(time: 0f);
                    //var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                    //var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, attackKnockbackPower);
                    //currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
                    var knockbackDirection = (playerCamera.transform.position - hitInfo.point).normalized;
                    currentVelocity += knockbackDirection * attackKnockbackPower;

                }
            }

        }

    }


    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) 
    {


        //dont want character to go up and down, so direction should be clamped
        var forward = Vector3.ProjectOnPlane(
            _reqestedRotation * Vector3.forward,
            motor.CharacterUp
            );
        if (forward != Vector3.zero) { 
        
            currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
        }
    
    }

    public void BeforeCharacterUpdate(float deltaTime)
    {
        _tempState = _state;
        //crouching
        if(_requestedCrouch && _state.Stance is Stance.Stand)
        {
            //Debug.Log($"Crouching - changing height from {motor.Capsule.height} to {crouchHeight}");

            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions
                (
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
        }
    }

    public void AfterCharacterUpdate(float deltaTime)
    {
        //uncrouching
        if (!_requestedCrouch && _state.Stance is not Stance.Stand)
        {
            //Debug.Log($"Standing - changing height from {motor.Capsule.height} to {standHeight}");

            //_stance = Stance.Stand;
            motor.SetCapsuleDimensions
                (
                    radius: motor.Capsule.radius,
                    height: standHeight,
                    yOffset: standHeight * 0.5f
                );

            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            var mask = motor.CollidableLayers;
            if(motor.CharacterOverlap(pos, rot, _uncrouchOverLapResults, mask, QueryTriggerInteraction.Ignore) > 0)
            {
                //recrouch
                _requestedCrouch = true;
                motor.SetCapsuleDimensions
                (
                    radius: motor.Capsule.radius,
                    height: crouchHeight,
                    yOffset: crouchHeight * 0.5f
                );
            }
            else
            {
                _state.Stance = Stance.Stand;
            }

        }


        //update state to reflect relevant morot properties
        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _state.Velocity = motor.Velocity;
        //update the laststate to store the character state snapshot taken at the beginning of this character update
        _lastState = _tempState;
    }


    public bool IsColliderValidForCollisions(Collider coll) => true;

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void PostGroundingUpdate(float deltaTime)
    {
        if(!motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide)
        {
            _state.Stance = Stance.Crouch;
        }
    }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport){ }



    public Transform GetCameraTarget() => cameraTarget;
    public CharacterState GetState() => _state;
    public CharacterState GetLastState() => _lastState;


    public void setPosition(Vector3 position, bool killvelocity = true)
    {
        motor.SetPosition(position);
        if (killvelocity)
        {
            motor.BaseVelocity = Vector3.zero;
        }


    }
    private void OnGUI()
    {
        // Crosshair settings
        float size = 5f; // diameter in pixels
        Color color = Color.white;

        // Calculate center position
        float x = (Screen.width - size) / 2f;
        float y = (Screen.height - size) / 2f;

        // Save previous GUI color and set new one
        Color prevColor = GUI.color;
        GUI.color = color;

        // Draw the dot (as a box, appears as a square dot)
        GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);

        // Restore previous GUI color
        GUI.color = prevColor;


        // --- Character State Info ---
        string stateInfo =
            $"Grounded: {_state.Grounded}\n" +
            $"Stance: {_state.Stance}\n" +
            $"Velocity: {_state.Velocity:F2}\n" +
            $"Acceleration: {_state.Acceleration:F2}";

        // Set style for state info
        GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };

        // Draw state info at top-left
        GUI.Label(new Rect(10, 10, 400, 80), stateInfo, infoStyle);


    }



}
