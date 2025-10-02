using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.Windows;

public class Player : MonoBehaviour
{

    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [Space]
    [SerializeField] private CameraSpring cameraSpring;
    [SerializeField] private CameraLean cameraLean;
    [SerializeField] private bool useCrouchToggle = true;


    private PlayerInputActions _inputActions;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget());

        cameraSpring.Initialize();
        cameraLean.Initialize();
    }


    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
        var input = _inputActions.Player;
        var deltaTime = Time.deltaTime;
        
        // gets camera input, update rotation
        var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
        playerCamera.UpdateRotation(cameraInput);




        //get chracterinput and update
        var characterInput = new CharacterInput
        {
            Rotation = playerCamera.transform.rotation,
            Move = input.Move.ReadValue<Vector2>(),
            Jump = input.Jump.WasPressedThisFrame(),
            JumpSustain = input.Jump.IsPressed(),
            Crouch = useCrouchToggle
                ? (input.Crouch.WasPressedThisFrame() ? CrouchInput.Toggle : CrouchInput.None)
                : (input.Crouch.IsPressed() ? CrouchInput.Crouch : CrouchInput.UnCrouch),
            
            //Attack = input.Attack.WasPressedThisFrame()
            Attack = input.Attack.IsPressed()
        };

        playerCharacter.UpdateInput(characterInput);
        playerCharacter.UpdateBody(deltaTime);




#if UNITY_EDITOR
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("Teleport");
            var ray = new Ray(playerCamera.transform.position,playerCamera.transform.forward);
            if(Physics.Raycast(ray, out var hit))
            {
                Teleport(hit.point);
            }
        }
        #endif



    }

    private void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        var cameraTarget = playerCharacter.GetCameraTarget();
        var state = playerCharacter.GetState();

        playerCamera.UpdatePosition(cameraTarget);
        cameraSpring.UpdateSpring(deltaTime, cameraTarget.up);
        cameraLean.UpdateLean(deltaTime ,state.Stance is Stance.Slide ,state.Acceleration , cameraTarget.up);
    }

    public void Teleport(Vector3 position)
    {
        playerCharacter.setPosition(position);
    }





}
