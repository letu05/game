using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float suddenTurnStopDuration = 0.12f;
    [SerializeField] private float inputThreshold = 0.1f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private int maxAirJumps = 1;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string movingParam = "IsMoving";
    [SerializeField] private string stopTriggerParam = "Stop";

    private Rigidbody2D rb;
    private float horizontalInput;
    private float lastMoveInput;
    private float stopTimer;
    private float jumpBufferCounter;
    private float coyoteCounter;
    private int airJumpsUsed;

    private PlayerStateMachine currentState = PlayerStateMachine.idie;

    private int speedHash;
    private int movingHash;
    private int stopTriggerHash;

    private bool canSetSpeed;
    private bool canSetMoving;
    private bool canSetStopTrigger;

    public PlayerStateMachine CurrentState => currentState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        speedHash = string.IsNullOrWhiteSpace(speedParam) ? 0 : Animator.StringToHash(speedParam);
        movingHash = string.IsNullOrWhiteSpace(movingParam) ? 0 : Animator.StringToHash(movingParam);
        stopTriggerHash = string.IsNullOrWhiteSpace(stopTriggerParam) ? 0 : Animator.StringToHash(stopTriggerParam);

        canSetSpeed = HasAnimatorParameter(animator, speedParam, AnimatorControllerParameterType.Float);
        canSetMoving = HasAnimatorParameter(animator, movingParam, AnimatorControllerParameterType.Bool);
        canSetStopTrigger = HasAnimatorParameter(animator, stopTriggerParam, AnimatorControllerParameterType.Trigger);
    }

    private void Update()
    {
        ReadInput();
        UpdateState();
        UpdateAnimation();
        UpdateFacingDirection();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyJump();
    }

    private void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Bat su kien nhan phim Space trong Update de khong bi mat input.
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Coyote time cho phep nhay tre mot chut sau khi roi khoi mat dat.
        if (IsGrounded())
        {
            coyoteCounter = coyoteTime;
            airJumpsUsed = 0;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }
    }

    private void UpdateState() // Quan ly chuyen trang thai di chuyen/nhay cua nhan vat.
    {
        if (stopTimer > 0f)
        {
            stopTimer -= Time.deltaTime;
            SetState(PlayerStateMachine.stop);
            return;
        }

        bool isGrounded = IsGrounded();

        bool hasInput = Mathf.Abs(horizontalInput) > inputThreshold;
        bool hadInput = Mathf.Abs(lastMoveInput) > inputThreshold;
        bool abruptDirectionChange = hasInput && hadInput && Mathf.Sign(horizontalInput) != Mathf.Sign(lastMoveInput);

        // Dang o tren khong thi uu tien state jump.
        if (!isGrounded)
        {
            SetState(PlayerStateMachine.jump);
            return;
        }

        // Vua tiep dat: thoat jump va quay ve walk/idle theo input hien tai.
        if (currentState == PlayerStateMachine.jump)
        {
            SetState(hasInput ? PlayerStateMachine.walk : PlayerStateMachine.idie);
        }

        if (abruptDirectionChange)
        {
            stopTimer = suddenTurnStopDuration;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetState(PlayerStateMachine.stop);
            lastMoveInput = horizontalInput;
            return;
        }

        if (hasInput)
        {
            SetState(PlayerStateMachine.walk); // Co input thi di bo.
            lastMoveInput = horizontalInput;
        }
        else if (currentState != PlayerStateMachine.stop)
        {
            SetState(PlayerStateMachine.idie);
        }
    }

    private void ApplyMovement()
    {
        // Cho phep dieu huong trai/phai ca khi dang jump de cam giac dieu khien mem hon.
        bool canMoveHorizontally = currentState == PlayerStateMachine.walk || currentState == PlayerStateMachine.jump;
        float targetHorizontalSpeed = canMoveHorizontally ? horizontalInput * moveSpeed : 0f;
        rb.linearVelocity = new Vector2(targetHorizontalSpeed, rb.linearVelocity.y);
    }
    private void ApplyJump()
    {
        if (jumpBufferCounter <= 0f)
        {
            return;
        }

        bool canGroundJump = coyoteCounter > 0f;
        bool canAirJump = !IsGrounded() && airJumpsUsed < maxAirJumps;

        if (!canGroundJump && !canAirJump)
        {
            return;
        }

        if (!canGroundJump)
        {
            airJumpsUsed++;
        }

        // Reset luc roi de do cao nhay on dinh, sau do tao xung luc nhay.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        SetState(PlayerStateMachine.jump);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // Da xu ly lan nhay nay thi xoa buffer de tranh nhay lap.
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
    }
    private bool IsGrounded()//check xem player co dang o tren mat dat hay khong de quyet dinh co the nhay duoc hay khong.
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
    }
    private void UpdateFacingDirection()
    {
        if (Mathf.Abs(horizontalInput) <= inputThreshold)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontalInput);
        transform.localScale = scale;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (canSetSpeed && speedHash != 0)
        {
            animator.SetFloat(speedHash, Mathf.Abs(rb.linearVelocity.x));
        }

        if (canSetMoving && movingHash != 0)
        {
            animator.SetBool(movingHash, currentState == PlayerStateMachine.walk);
        }
    }

    private void SetState(PlayerStateMachine newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        if (animator != null && currentState == PlayerStateMachine.stop && canSetStopTrigger && stopTriggerHash != 0)
        {
            animator.SetTrigger(stopTriggerHash);
        }
    }

    private bool HasAnimatorParameter(Animator targetAnimator, string paramName, AnimatorControllerParameterType expectedType)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(paramName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.name == paramName && parameter.type == expectedType)
            {
                return true;
            }
        }

        
        return false;
    }
    void OnDrawGizmos()
    {
        if (groundCheck != null)        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
