﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {
    [SerializeField] new Camera camera;
    [SerializeField] Plane plane;
    //[SerializeField] PlaneHUD planeHUD;

    Vector3 controlInput;
    PlaneCamera planeCamera;
    AIController aiController;

    void Start() {
        planeCamera = GetComponent<PlaneCamera>();
        SetPlane(plane);    //SetPlane if var is set in inspector
    }

    void SetPlane(Plane plane)
    {
        this.plane = plane;
        aiController = plane.GetComponent<AIController>();
    }

    void InputToggleHelp()
    {
        if (Input.GetKey(KeyCode.F1))
        {
            if (plane == null)
            {
                return;
            }

            //planeHUD.ToggleHelpDialogs();
            
        }
    }

    void InputSetThrottle()
    {
        if (plane == null || aiController.enabled)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            plane.SetThrottleInput(1);
        }
        if(Input.GetKeyDown(KeyCode.LeftControl))
        {
            plane.SetThrottleInput(-1);
        }
    }

    void InputSetPitchAndRoll()
    {
        if (plane == null || aiController.enabled)
        {
            return;
        }

        float roll = Input.GetAxis("Horizontal");
        float pitch = Input.GetAxis("Vertical");
        controlInput = new Vector3(pitch, controlInput.y, -roll);
    }

    void InputSetYaw()
    {
        if (plane == null || aiController.enabled)
        {
            return;
        }

        float yaw = Input.GetAxis("Yaw");
        controlInput = new Vector3(controlInput.x, yaw, controlInput.z);
    }

    //public void OnCameraInput(InputAction.CallbackContext context) {
    //    if (plane == null) return;

    //    var input = context.ReadValue<Vector2>();
    //    planeCamera.SetInput(input);
    //}

    void InputToggleFlaps()
    {
        if (plane == null || aiController.enabled)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            plane.ToggleFlaps();
        }
    }

    //public void OnFireMissile(InputAction.CallbackContext context) {
    //    if (plane == null) return;

    //    if (context.phase == InputActionPhase.Performed) {
    //        plane.TryFireMissile();
    //    }
    //}

    //public void OnFireCannon(InputAction.CallbackContext context) {
    //    if (plane == null) return;

    //    if (context.phase == InputActionPhase.Started) {
    //        plane.SetCannonInput(true);
    //    } else if (context.phase == InputActionPhase.Canceled) {
    //        plane.SetCannonInput(false);
    //    }
    //}

    void InputToggleAI()
    {
        if (plane == null)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.I))
        {
            if (aiController != null)
            {
                aiController.enabled = !aiController.enabled;
            }
        }
    }

    void FlightControls()
    {
        InputSetYaw();
        InputSetPitchAndRoll();
        InputSetThrottle();
        InputToggleFlaps();
    }

    void Update() {
        if (plane == null) return;
        if (aiController.enabled) return;

        plane.SetControlInput(controlInput);

        FlightControls();
    }
}
