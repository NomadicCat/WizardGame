using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;


public enum CrouchInput
{
    None, Toggle
}

public enum Stance
{
    Stand, Crouch
}

public struct CharacterInput
{
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;


}


public class PlayerCharacter : MonoBehaviour, ICharacterController
{
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform root;
    [SerializeField] private Transform cameraTarget;
    [Space]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float crouchSpeed = 7f;
    [SerializeField] private float walkResponse = 25f;
    [SerializeField] private float crouchResponse = 20f;
    [Space]
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private float airAcceleration = 70f;

    [SerializeField] private float jumpSpeed = 20f;
    [Range(0f, 1f)]
    [SerializeField] private float jumpSustainGravity = 0.4f;
    [SerializeField] private float gravity = -90f;
    [Space]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 15f;
    [Range(0f, 1f)]
    [SerializeField] private float standCameraTargetHeight = 0.9f;
    [Range(0f, 1f)]
    [SerializeField] private float crouchCameraTargetHeight = 0.7f;





    private Stance _stance; 
    private Quaternion _reqestedRotation;
    private Vector3 _reqestedMovement;
    private bool _requestedJump;
    private bool _requestedSustainedJump;
    private bool _requestedCrouch;
    private Collider[] _uncrouchOverLapResults;


    public void Initialize()
    {
        _uncrouchOverLapResults = new Collider[8];
        _stance = Stance.Stand;
        motor.CharacterController = this;
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

        _requestedJump = _requestedJump || input.Jump;
        _requestedSustainedJump = input.JumpSustain;

        _requestedCrouch = input.Crouch switch
        {
            CrouchInput.Toggle => !_requestedCrouch,
            CrouchInput.None => _requestedCrouch,
            //CrouchInput.Crouch => true, for holding
            //CrouchInput.UnCrouch => false for releasing
           _ => _requestedCrouch

        };




    }


    public void UpdateBody(float deltaTime)
    {
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        var cameraTargetHeight = currentHeight *
            (
            _stance is Stance.Stand
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

        if (motor.GroundingStatus.IsStableOnGround)
        {
            var groundedMovement = motor.GetDirectionTangentToSurface
                (
                    direction: _reqestedMovement,
                    surfaceNormal: motor.GroundingStatus.GroundNormal
                ) * _reqestedMovement.magnitude;




            var speed = _stance is Stance.Stand
                ? walkSpeed
                : crouchSpeed;

            var response = _stance is Stance.Stand
                ? walkResponse
                : crouchResponse;



            var targetVelocity = groundedMovement * speed;
            currentVelocity = Vector3.Lerp
            (
                a: currentVelocity,
                b: targetVelocity,
                t: 1f - Mathf.Exp(-response * deltaTime)
            );
        }
        else //in the air
        {

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

                //add it to the current planar velopcity for a target celocity
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                //limit target velocity to air speed
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

                //steer towards current velocity
                currentVelocity += targetPlanarVelocity - currentPlanarVelocity;

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

        if (_requestedJump)
        {
            _requestedJump = false;

            //unstick from ground
            motor.ForceUnground(time: 0f);

            //add jump force
            //currentVelocity += motor.CharacterUp * jumpSpeed;
            //min vert speed to jump speed
            //currentVelocity.y = Mathf.Max(currentVelocity.y, jumpSpeed);
            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
            currentVelocity += motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
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
        //crouching
        if(_requestedCrouch && _stance is Stance.Stand)
        {
            //Debug.Log($"Crouching - changing height from {motor.Capsule.height} to {crouchHeight}");

            _stance = Stance.Crouch;
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
        if (!_requestedCrouch && _stance is not Stance.Stand)
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
                _stance = Stance.Stand;
            }

        }
    }


    public bool IsColliderValidForCollisions(Collider coll) => true;

    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }

    public void PostGroundingUpdate(float deltaTime) { }

    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport){ }



    public Transform GetCameraTarget() => cameraTarget;

 
}
