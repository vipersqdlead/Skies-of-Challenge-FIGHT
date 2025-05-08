using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class PlayerInputs : MonoBehaviour
{
    [Header("References")]
    public Transform planeTransform;       // Player's plane
    public Transform targetCursorTransform; // Assign a dummy GameObject (empty or small marker)
    public Camera playerCamera;

    [Header("Cursor Movement Settings")]
    public float cursorDistance = 1000f;        // How far in front of the plane the cursor starts
    public float moveSpeed = 200f;              // How fast the cursor moves
    public bool useTiltControls;
    public float tiltSensitivity = 5f;
    public AnimationCurve sensitivityCurve;
    public float mouseSensitivity = 40f;

    public float tiltY, tiltX;

    public float normalizedY;

    private Vector2 inputMovement; // Horizontal and vertical input accumulation
    public float calibratedOffset;
    public bool isCalibrated;

    private void Awake()
    {
        planeTransform = transform;
        playerCamera = Camera.main;

        //useTiltControls = Application.isMobilePlatform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {        
        if (useTiltControls && !isCalibrated)
        {
            CalibrateTilt();
        }
        ReadInput();
        UpdateCursorPosition();
    }

    private void ReadInput()
    {
        if (useTiltControls)
        {
            tiltY = Mathf.Clamp01(Mathf.Abs(Input.acceleration.y));
            tiltX = Input.acceleration.x;

            normalizedY = Mathf.Lerp(-1f, 1f, Mathf.Clamp01(tiltY - 0.1f));

            float sensitivity = sensitivityCurve.Evaluate(Mathf.Clamp01(Mathf.Abs(Input.acceleration.y) + Mathf.Abs(Input.acceleration.x))) * tiltSensitivity;

            inputMovement = new Vector2(tiltX, -normalizedY) * sensitivity;
        }
        else
        {
            float moveX = Input.GetAxis("Mouse X");
            float moveY = Input.GetAxis("Mouse Y");
            inputMovement = new Vector2(moveX, moveY) * mouseSensitivity;
        }
    }

    private void UpdateCursorPosition()
    {
        if (targetCursorTransform == null || planeTransform == null)
            return;

        // Get directional vectors based on camera view, NOT aircraft rotation
        Vector3 cameraRight = playerCamera.transform.right;
        Vector3 cameraUp = playerCamera.transform.up;

        // Apply movement based on camera's view orientation
        Vector3 moveDirection = (cameraRight * inputMovement.x + cameraUp * inputMovement.y) * moveSpeed * Time.deltaTime;

        // Move the target cursor
        targetCursorTransform.position += moveDirection;

        // Clamp the cursor's position relative to the plane
        Vector3 localOffset = planeTransform.InverseTransformPoint(targetCursorTransform.position);
        localOffset.z = cursorDistance; // Always maintain forward distance

        // Apply the clamped position
        targetCursorTransform.position = planeTransform.TransformPoint(localOffset);
    }
    void OnDrawGizmos()
    {
        if (planeTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(planeTransform.position, targetCursorTransform.position);
        }
    }

    public void CalibrateTilt()
    {
        calibratedOffset = Input.acceleration.y;
        isCalibrated = true;
        print(calibratedOffset);
    }
}