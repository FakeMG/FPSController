using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {

    public bool CanMove { get; private set; } = true;
    public bool IsSprinting => canSprint && Input.GetKey(sprintKey) && currentInput.x > 0.1f;
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

    [Header("Headbob Parameters")]
    [SerializeField] private bool canHeadbob = true;
    [SerializeField] private float headbobTriggerSpeed = 1f;
    [SerializeField] private float walkHeadbobSpeed = 10f;
    [SerializeField] private float walkHeadbobAmount = 0.015f;
    [SerializeField] private float sprintHeadbobSpeed = 18;
    [SerializeField] private float sprintHeadbobAmount = 0.1f;
    [SerializeField] private float crouchHeadbobSpeed = 8f;
    [SerializeField] private float crouchHeadbobAmount = 0.025f;
    private Vector3 defaultCameraLocalPos;
    private float headbobTimer = 0f;

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

        defaultCameraLocalPos = playerCamera.transform.localPosition;
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

    private void LateUpdate() {
        if (CanMove) {
            if (canHeadbob) {
                HandleHeadbob();
                ResetHeadbob();
            }
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

    private void HandleHeadbob() {
        if (!characterController.isGrounded)
            return;

        if (Mathf.Abs(moveDirection.x) > headbobTriggerSpeed || Mathf.Abs(moveDirection.z) > headbobTriggerSpeed) {
            playerCamera.transform.localPosition = new Vector3(
                defaultCameraLocalPos.x + HeadBobMotion().x,
                defaultCameraLocalPos.y + HeadBobMotion().y,
                playerCamera.transform.localPosition.z);
        }
    }

    private Vector3 HeadBobMotion() {
        Vector3 pos = Vector3.zero;
        //làm headbob mượt hơn
        headbobTimer += Time.deltaTime;

        pos.y = Mathf.Sin(headbobTimer * (isCrouching ? crouchHeadbobSpeed : IsSprinting ? sprintHeadbobSpeed : walkHeadbobSpeed))
            * (isCrouching ? crouchHeadbobAmount : IsSprinting ? sprintHeadbobAmount : walkHeadbobAmount);
        pos.x = Mathf.Sin(headbobTimer * (isCrouching ? crouchHeadbobSpeed : IsSprinting ? sprintHeadbobSpeed : walkHeadbobSpeed) / 2)
            * (isCrouching ? crouchHeadbobAmount : IsSprinting ? sprintHeadbobAmount : walkHeadbobAmount)
            * 2;
        return pos;
    }

    private void ResetHeadbob() {
        if (playerCamera.transform.localPosition == defaultCameraLocalPos)
            return;

        if (Mathf.Abs(moveDirection.x) < headbobTriggerSpeed && Mathf.Abs(moveDirection.z) < headbobTriggerSpeed) {
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, defaultCameraLocalPos, 2 * Time.deltaTime);
            headbobTimer = 0;
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
