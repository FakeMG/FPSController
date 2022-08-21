using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {

    public bool CanMove { get; private set; } = true;
    public bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    public bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Sprint Parameters")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Jump Parameters")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Crouch Parameters")]
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private float crouchSpeed = 1.0f;
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 standCenter = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    private bool isCrouching = false;
    private Coroutine crouchCoroutine;

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 90)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 90)] private float lowerLookLimit = 80.0f;

    private CharacterController characterController;
    private Camera playerCamera;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float currentSpeed;
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
            ControlSpeed();
            HandleMovementInput();
            LimitDiagonalSpeed();

            if (canJump) {
                HandleJump();
            }

            if (canCrouch) {
                HandleCrouch();
            }

            HandleMouseLook();

            ApplyFinalMovement();
        }
    }

    private void ControlSpeed() {
        if (IsSprinting) {
            currentSpeed = sprintSpeed;
        } else {
            currentSpeed = walkSpeed;
        }

        if (isCrouching) {
            currentSpeed = crouchSpeed;
        }
    }

    private void HandleMovementInput() {
        currentInput = new Vector2(currentSpeed * Input.GetAxis("Vertical"), currentSpeed * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = transform.TransformDirection(Vector3.forward) * currentInput.x + transform.TransformDirection(Vector3.right) * currentInput.y;
        moveDirection.y = moveDirectionY;
    }

    private void LimitDiagonalSpeed() {
        if (currentInput.x != 0 && currentInput.y != 0) {
            float speed = Mathf.Sqrt((currentSpeed * currentSpeed) / 2);
            currentInput.x = Mathf.Clamp(currentInput.x, -speed, speed);
            currentInput.y = Mathf.Clamp(currentInput.y, -speed, speed);
        }
    }

    private void HandleJump() {
        if (ShouldJump) {
            moveDirection.y = jumpForce;
        }
    }

    private void HandleCrouch() {
        if (Input.GetKeyDown(crouchKey) || Input.GetKeyUp(crouchKey) || (!Input.GetKey(crouchKey) && isCrouching)) {
            if (!Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f)) {

                if (crouchCoroutine != null) {
                    StopCoroutine(crouchCoroutine);
                }

                crouchCoroutine = StartCoroutine(CrouchOrStand());
            }
        }
    }


    private IEnumerator CrouchOrStand() {
        isCrouching = !isCrouching;

        float timeElapsed = 0f;

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float currentHeight = characterController.height;

        Vector3 targetCenter = isCrouching ? crouchCenter : standCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch) {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
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
