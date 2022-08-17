using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {

    public bool CanMove { get; private set; } = true;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 90)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 90)] private float lowerLookLimit = 80.0f;

    private CharacterController characterController;
    private Camera playerCamera;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float cameraRotationX = 0;

    void Awake() {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update() {
        if (CanMove) {
            HandleMovementInput();
            HandleMouseLook();

            ApplyFinalMovement();
        }
    }

    private void HandleMovementInput() {
        currentInput = new Vector2(walkSpeed * Input.GetAxis("Vertical"), walkSpeed * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = transform.TransformDirection(Vector3.forward) * currentInput.x + transform.TransformDirection(Vector3.right) * currentInput.y;
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook() {
        cameraRotationX += Input.GetAxis("Mouse Y") * lookSpeedY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -lowerLookLimit, upperLookLimit);
        playerCamera.transform.localRotation = Quaternion.Inverse(Quaternion.Euler(cameraRotationX, 0, 0));

        transform.localRotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void ApplyFinalMovement() {
        if (!characterController.isGrounded) {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }
}
