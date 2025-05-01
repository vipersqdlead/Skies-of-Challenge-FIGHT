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
    public float mouseSensitivity = 40f;

    private Vector2 inputMovement; // Horizontal and vertical input accumulation
    private Vector2 calibratedOffset = Vector2.zero;
    private bool tiltCalibrated = false;

    private Matrix4x4 calibrationMatrix;

    private void Awake()
    {
        planeTransform = transform;
        playerCamera = Camera.main;

        //useTiltControls = Application.isMobilePlatform;
        if (useTiltControls)
        {
            //CalibrateTilt();
        }



        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        ReadInput();
        UpdateCursorPosition();
    }

    private void ReadInput()
    {
        if (useTiltControls)
        {

            //Vector3 raw = Input.acceleration;

            //Vector2 tiltRaw = new Vector2(raw.x, raw.y);
            //Vector2 tiltAdjusted = tiltRaw - calibratedOffset;

            //inputMovement = tiltAdjusted * tiltSensitivity;

            //float tiltX = GetCalibratedAcceleration().x;
            //float tiltY = GetCalibratedAcceleration().y;

            float tiltY = NormalizeTilt(Input.acceleration.y, 0.4f, 0f, 0.8f);
            float tiltX = NormalizeTilt(Input.acceleration.x, 0f, -0.7f, 0.7f); // for left/right, assuming flat hold

            inputMovement = new Vector2(tiltX, tiltY) * tiltSensitivity;
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
        //Vector3 raw = Input.acceleration;

        //// We're assuming the user holds the device in landscape mode (horizontal),
        //// so we take X (left/right) and Y (up/down) as input axes
        //calibratedOffset = new Vector2(raw.x, raw.y);
        //tiltCalibrated = true;

        //Debug.Log($"Tilt calibrated: {calibratedOffset}");

        // Get the initial orientation (replace with your actual initial orientation)
        Vector3 initialOrientation = Input.acceleration;

        // Create a matrix to compensate for the initial orientation
        calibrationMatrix = Matrix4x4.Rotate(Quaternion.LookRotation(-initialOrientation));
    }

    Vector3 GetCalibratedAcceleration()
    {
        // Apply the calibration to the raw acceleration data
        return calibrationMatrix.MultiplyVector(Input.acceleration);
    }

    float NormalizeTilt(float raw, float neutral, float minInput, float maxInput)
    {
        if (raw < neutral)
            return Mathf.InverseLerp(minInput, neutral, raw) * -1f;
        else
            return Mathf.InverseLerp(neutral, maxInput, raw);
    }
}