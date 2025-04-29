using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class PlayerInputs : MonoBehaviour
{
    [Header("References")]
    public Transform planeTransform;
    public Camera playerCamera;
    public float cursorDistance = 50f; // Distance in world units from plane
    public Transform target;

    [Header("Cursor Settings")]
    public float sensitivity = 5f;  // Mouse sensitivity
    public float maxCursorMovement = 200f; // Max cursor movement in pixels (like a screen limit)
    Vector2 mouseDelta;

    [Header("Debug")]
    public Transform cursorVisual; // Optional: a dummy object to show the targetWorldPosition

    private Vector2 currentCursorOffset = Vector2.zero; // Cursor position offset from center (in screen space)
    public Vector3 targetWorldPosition { get; private set; }

    private void Awake()
    {
        planeTransform = transform;
        playerCamera = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if(HasMouseMoved())
        {
            UpdateCursorOffset();
            MoveTargetAround();
            //UpdateTargetWorldPosition();
        }
    }

    private bool HasMouseMoved()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        return Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f;
    }

    public void MoveTargetAround()
    {
        target.parent.rotation = Quaternion.Euler(target.parent.rotation.x + (mouseDelta.x * Time.deltaTime * 60f), target.parent.rotation.y + (mouseDelta.y * Time.deltaTime * 60f), 0);
        targetWorldPosition = target.position;
    }

    private void UpdateCursorOffset()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        mouseDelta = new Vector2(mouseX, mouseY) * sensitivity;

        // Only add delta if there is actual movement
        if (mouseDelta.sqrMagnitude > 0.0001f)
        {
            currentCursorOffset += mouseDelta;

            // Clamp cursor inside max movement area
            currentCursorOffset = Vector2.ClampMagnitude(currentCursorOffset, maxCursorMovement);
        }
    }

    private void UpdateTargetWorldPosition()
    {
        if (planeTransform == null || playerCamera == null)
            return;

        // Create a screen point from center + cursorOffset
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 cursorScreenPosition = screenCenter + new Vector3(currentCursorOffset.x, currentCursorOffset.y, 0f);

        // Create a ray from this position
        Ray ray = playerCamera.ScreenPointToRay(cursorScreenPosition);

        // Set the target world position along the ray
        targetWorldPosition = ray.origin + ray.direction * cursorDistance;

        // Move visual cursor if assigned
        if (cursorVisual != null)
        {
            cursorVisual.position = targetWorldPosition;
        }
    }
    void OnDrawGizmos()
    {
        if (planeTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(planeTransform.position, targetWorldPosition);
        }
    }
}