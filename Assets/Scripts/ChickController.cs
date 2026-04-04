using System.Collections;
using UnityEngine;
using UniSense;

[RequireComponent(typeof(CharacterController))]
public class ChickController : MonoBehaviour
{

    public ChickCamera chickCamera;

    // Assign the GamepadInput component in the Inspector for controller support.
    // If left empty the game still works fine with keyboard/mouse.
    public GamepadInput gamepadInput;

    public Transform body;
    public Transform rightLeg;
    public Transform leftLeg;
    public Transform rightWing;
    public Transform leftWing;
    public Transform head;

    public float walkSpeed     = 4.5f;
    public float sprintSpeed   = 8f;
    public float jumpForce     = 8f;
    public float gravity       = -22f;

    public float strafeAngleLimit = 40f;
    public float turnAngleLimit   = 100f;
    public float strafeRotSpeed   = 3f;
    public float turnRotSpeed     = 16f;

    public float legSwingAngle       = 38f;
    public float legSwingSpeed       = 9f;
    public float sprintLegAngle      = 55f;
    public float sprintLegSpeed      = 14f;

    public float wingIdleDrift       = 5f;
    public float wingIdleDriftSpeed  = 1.8f;
    public float wingJumpFlapAngle   = 50f;
    public float wingJumpDuration    = 0.45f;
    public float wingFallSpread      = 35f;
    public float wingFallWiggleSpeed = 7f;
    public float wingSprintFlap      = 18f;

    public float bodyBobHeight       = 0.07f;
    public float bodyBobSpeed        = 9f;
    public float bodyRunLean         = 12f;
    public float bodySprintLean      = 8f;
    public float landSquishIntensity = 0.35f;
    public float landSquishDuration  = 0.22f;

    public float headNodAngle        = 12f;
    public float headNodSpeed        = 9f;
    public float headRunForward      = 18f;
    public float headAirBack         = 14f;

    public AudioSource footstepSource;
    public AudioClip[] footstepSFX;
    [Range(0f, 1f)]
    public float footstepVolume       = 0.55f;
    [Range(0f, 1f)]
    public float sprintFootstepVolume = 0.8f;

    public GameObject footprintPrefab;
    public float footprintLifetime    = 14f;
    public float footprintYOffset     = 0.012f;
    public float footprintSideOffset  = 0.14f;
    public LayerMask groundLayerMask  = ~0;

    CharacterController _cc;
    Vector3 _velocity;

    bool _isGrounded;
    bool _wasGrounded;

    float _legTimer;
    float _bodyBobTimer;
    float _fallWiggleTimer;
    float _wingJumpTimer;
    float _squishTimer;

    bool _isDead;
    bool _isFallDying;
    bool _inputLocked;

    float _prevRightLegSin;
    float _prevLeftLegSin;

    bool _wasSprinting;

    Vector3    _bodyRestPos;
    Vector3    _bodyRestScale;
    Quaternion _rightLegRest, _leftLegRest;
    Quaternion _rightWingRest, _leftWingRest;
    Quaternion _headRest;

    // Cached input read once per frame and shared between HandleMovement / HandleAnimation
    float _inputH;
    float _inputV;
    bool  _inputSprint;

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        if (body)      { _bodyRestPos   = body.localPosition;  _bodyRestScale = body.localScale; }
        if (rightLeg)  _rightLegRest  = rightLeg.localRotation;
        if (leftLeg)   _leftLegRest   = leftLeg.localRotation;
        if (rightWing) _rightWingRest = rightWing.localRotation;
        if (leftWing)  _leftWingRest  = leftWing.localRotation;
        if (head)      _headRest      = head.localRotation;
    }

    void Update()
    {
        if (_isDead && !_isFallDying) return;
        if (_isFallDying) { ContinueFalling(); return; }
        GatherInput();
        GatherGroundState();
        HandleMovement();
        HandleAnimation();
        UpdateTriggerFeedback();
    }

    void OnDisable()
    {
        // Reset trigger resistance so it doesn't stay on when the script is disabled
        var dualSense = DualSenseGamepadHID.FindCurrent();
        dualSense?.SetGamepadState(new DualSenseGamepadState
        {
            RightTrigger = new DualSenseTriggerState { EffectType = DualSenseTriggerEffectType.NoResistance },
            LeftTrigger  = new DualSenseTriggerState { EffectType = DualSenseTriggerEffectType.NoResistance }
        });
    }

    // Read all input once per frame so HandleMovement and HandleAnimation share the same values.
    void GatherInput()
    {
        if (_inputLocked)
        {
            _inputH      = 0f;
            _inputV      = 0f;
            _inputSprint = false;
            return;
        }

        // Keyboard baseline
        _inputH      = Input.GetAxis("Horizontal");
        _inputV      = Input.GetAxis("Vertical");
        _inputSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Gamepad override — left stick takes over when it has meaningful input
        if (gamepadInput != null && gamepadInput.leftStick.magnitude > 0.15f)
        {
            _inputH = gamepadInput.leftStick.x;
            _inputV = gamepadInput.leftStick.y;
        }

        // Sprint: R2 / Right Trigger on gamepad
        if (gamepadInput != null && gamepadInput.rightTrigger)
            _inputSprint = true;
    }

    // Sends the right trigger state to the DualSense every frame.
    // Three states:
    //   - Not sprinting                  -> no resistance
    //   - Sprinting, step pulse active   -> strong brief resistance (the footstep thump)
    //   - Sprinting, no pulse            -> light base resistance so holding R2 always feels weighted
    void UpdateTriggerFeedback()
    {
        var dualSense = DualSenseGamepadHID.FindCurrent();
        if (dualSense == null) return;

        if (_inputSprint == _wasSprinting) return;
        _wasSprinting = _inputSprint;

        dualSense.SetGamepadState(new DualSenseGamepadState
        {
            Motor        = new DualSenseMotorSpeed(GamepadRumble.CurrentLow, GamepadRumble.CurrentHigh),
            RightTrigger = new DualSenseTriggerState
            {
                EffectType = _inputSprint
                    ? DualSenseTriggerEffectType.ContinuousResistance
                    : DualSenseTriggerEffectType.NoResistance,
                Continuous = new DualSenseContinuousResistanceProperties
                {
                    Force         = 60,
                    StartPosition = (byte)55
                }
            },
            LeftTrigger = new DualSenseTriggerState
            {
                EffectType = DualSenseTriggerEffectType.NoResistance
            }
        });
    }

    void GatherGroundState()
    {
        _wasGrounded = _isGrounded;
        _isGrounded  = _cc.isGrounded;

        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        if (_isGrounded && !_wasGrounded)
            OnLand();
    }

    void HandleMovement()
    {
        float h        = _inputH;
        float v        = _inputV;
        bool sprinting = _inputSprint;
        float speed    = sprinting ? sprintSpeed : walkSpeed;

        Vector3 camForward = chickCamera
            ? chickCamera.GetCameraForward()
            : Vector3.forward;

        Vector3 camRight = chickCamera
            ? chickCamera.GetCameraRight()
            : Vector3.right;

        Vector3 moveDir = camForward * v + camRight * h;
        moveDir.y = 0f;

        bool hasInput = moveDir.magnitude >= 0.1f;

        if (hasInput)
        {
            moveDir.Normalize();

            float absAngle = Mathf.Abs(
                Vector3.SignedAngle(transform.forward, moveDir, Vector3.up)
            );

            float rotBlend = Mathf.InverseLerp(strafeAngleLimit, turnAngleLimit, absAngle);
            float rotSpeed = Mathf.Lerp(strafeRotSpeed, turnRotSpeed, rotBlend);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(moveDir, Vector3.up),
                rotSpeed * Time.deltaTime * 60f
            );

            _cc.Move(moveDir * speed * Time.deltaTime);
        }

        // Jump — Space (keyboard) or Cross / A button (gamepad)
        bool jumpInput = Input.GetKeyDown(KeyCode.Space)
                      || (gamepadInput != null && gamepadInput.jumpPressed);

        if (!_inputLocked && jumpInput && _isGrounded)
        {
            _velocity.y    = Mathf.Sqrt(jumpForce * -2f * gravity);
            _wingJumpTimer = wingJumpDuration;
        }

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    void OnLand()
    {
        _squishTimer = landSquishDuration;
    }

    void HandleAnimation()
    {
        float h          = _inputH;
        float v          = _inputV;
        bool  sprinting  = _inputSprint;
        float inputMag   = new Vector2(h, v).magnitude;
        bool  isMoving   = inputMag > 0.1f;
        bool  isFalling  = !_isGrounded && _velocity.y < -1.5f;

        if (isMoving && _isGrounded)
        {
            float mult    = sprinting ? 1.55f : 1f;
            _legTimer     += Time.deltaTime * legSwingSpeed * mult;
            _bodyBobTimer += Time.deltaTime * bodyBobSpeed  * mult;
        }

        if (isFalling)
            _fallWiggleTimer += Time.deltaTime * wingFallWiggleSpeed;
        else
            _fallWiggleTimer = 0f;

        if (_wingJumpTimer > 0f) _wingJumpTimer -= Time.deltaTime;
        if (_squishTimer   > 0f) _squishTimer   -= Time.deltaTime;

        AnimateLegs(isMoving, isFalling, sprinting);
        AnimateWings(isMoving, isFalling, sprinting);
        AnimateBody(isMoving, isFalling, sprinting);
        AnimateHead(isMoving, isFalling, sprinting);
        CheckFootsteps(isMoving, sprinting);
    }

    void AnimateLegs(bool isMoving, bool isFalling, bool sprinting)
    {
        if (!rightLeg || !leftLeg) return;

        if (isFalling)
        {
            float wiggle = Mathf.Sin(_fallWiggleTimer * 3.2f) * 22f;
            rightLeg.localRotation = _rightLegRest * Quaternion.Euler(wiggle + 28f,  0f,  18f);
            leftLeg.localRotation  = _leftLegRest  * Quaternion.Euler(-wiggle + 28f, 0f, -18f);
        }
        else if (isMoving)
        {
            float swingAmt = sprinting ? sprintLegAngle : legSwingAngle;
            float rAngle   =  Mathf.Sin(_legTimer)            * swingAmt;
            float lAngle   =  Mathf.Sin(_legTimer + Mathf.PI) * swingAmt;
            float latR     =  Mathf.Cos(_legTimer)            * (swingAmt * 0.12f);
            float latL     =  Mathf.Cos(_legTimer + Mathf.PI) * (swingAmt * 0.12f);

            rightLeg.localRotation = _rightLegRest * Quaternion.Euler(rAngle, 0f, latR);
            leftLeg.localRotation  = _leftLegRest  * Quaternion.Euler(lAngle, 0f, latL);
        }
        else
        {
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, _rightLegRest, Time.deltaTime * 8f);
            leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,  _leftLegRest,  Time.deltaTime * 8f);
        }
    }

    void CheckFootsteps(bool isMoving, bool sprinting)
    {
        if (!isMoving || !_isGrounded) return;
        if (footstepSFX == null || footstepSFX.Length == 0) return;
        if (!footstepSource) return;

        float rightSin = Mathf.Sin(_legTimer);
        float leftSin  = Mathf.Sin(_legTimer + Mathf.PI);

        bool rightStep = _prevRightLegSin < 0f && rightSin >= 0f;
        bool leftStep  = _prevLeftLegSin  < 0f && leftSin  >= 0f;

        if (rightStep) StampFootprint( footprintSideOffset);
        if (leftStep)  StampFootprint(-footprintSideOffset);

        if (rightStep || leftStep)
        {
            float vol = sprinting ? sprintFootstepVolume : footstepVolume;
            footstepSource.PlayOneShot(footstepSFX[Random.Range(0, footstepSFX.Length)], vol);
        }

        _prevRightLegSin = rightSin;
        _prevLeftLegSin  = leftSin;
    }

    void StampFootprint(float side)
    {
        if (!footprintPrefab) return;

        Vector3 rayOrigin = transform.position
            + transform.right * side
            + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 2f, groundLayerMask))
        {
            Vector3 stampPos = hit.point + hit.normal * footprintYOffset;

            Quaternion stampRot = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized,
                hit.normal
            );

            GameObject fp = Instantiate(footprintPrefab, stampPos, stampRot);

            if (side < 0f)
            {
                Vector3 s = fp.transform.localScale;
                fp.transform.localScale = new Vector3(-s.x, s.y, s.z);
            }
            Destroy(fp, footprintLifetime);
        }
    }

    void AnimateWings(bool isMoving, bool isFalling, bool sprinting)
    {
        if (!rightWing || !leftWing) return;

        Quaternion rTarget, lTarget;

        if (_wingJumpTimer > 0f)
        {
            float t         = 1f - (_wingJumpTimer / wingJumpDuration);
            float flapAngle = Mathf.Sin(t * Mathf.PI * 2f) * wingJumpFlapAngle;
            rTarget = _rightWingRest * Quaternion.Euler(0f, 0f, -flapAngle);
            lTarget = _leftWingRest  * Quaternion.Euler(0f, 0f,  flapAngle);
        }
        else if (isFalling)
        {
            float wiggle = Mathf.Sin(_fallWiggleTimer * 2.8f) * wingFallSpread;
            float spread = wingFallSpread * 0.6f;
            rTarget = _rightWingRest * Quaternion.Euler(0f, 0f, -(spread + wiggle));
            lTarget = _leftWingRest  * Quaternion.Euler(0f, 0f,  (spread + wiggle));
        }
        else if (isMoving && sprinting)
        {
            float flap  = Mathf.Sin(_legTimer * 1.3f) * wingSprintFlap;
            rTarget = _rightWingRest * Quaternion.Euler(0f, 0f, -flap);
            lTarget = _leftWingRest  * Quaternion.Euler(0f, 0f,  flap);
        }
        else
        {
            float drift = Mathf.Sin(Time.time * wingIdleDriftSpeed) * wingIdleDrift;
            rTarget = _rightWingRest * Quaternion.Euler(0f, 0f, -drift);
            lTarget = _leftWingRest  * Quaternion.Euler(0f, 0f,  drift);
        }

        rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation, rTarget, Time.deltaTime * 12f);
        leftWing.localRotation  = Quaternion.Slerp(leftWing.localRotation,  lTarget, Time.deltaTime * 12f);
    }

    void AnimateBody(bool isMoving, bool isFalling, bool sprinting)
    {
        if (!body) return;

        Vector3 targetScale = _bodyRestScale;
        if (_squishTimer > 0f)
        {
            float t      = _squishTimer / landSquishDuration;
            float squish = Mathf.Sin(t * Mathf.PI) * landSquishIntensity;
            targetScale  = new Vector3(
                _bodyRestScale.x * (1f + squish * 0.55f),
                _bodyRestScale.y * (1f - squish),
                _bodyRestScale.z * (1f + squish * 0.55f)
            );
        }
        body.localScale = Vector3.Lerp(body.localScale, targetScale, Time.deltaTime * 20f);

        float bobY = (isMoving && _isGrounded)
            ? Mathf.Abs(Mathf.Sin(_bodyBobTimer)) * bodyBobHeight
            : 0f;

        body.localPosition = Vector3.Lerp(
            body.localPosition,
            _bodyRestPos + new Vector3(0f, bobY, 0f),
            Time.deltaTime * 18f
        );

        float targetLean = 0f;
        if      (isFalling) targetLean = -(bodyRunLean * 0.6f);
        else if (isMoving)  targetLean = bodyRunLean + (sprinting ? bodySprintLean : 0f);

        body.localRotation = Quaternion.Slerp(
            body.localRotation,
            Quaternion.Euler(targetLean, 0f, 0f),
            Time.deltaTime * 9f
        );
    }

    void AnimateHead(bool isMoving, bool isFalling, bool sprinting)
    {
        if (!head) return;

        float nod = (isMoving && _isGrounded)
            ? Mathf.Sin(_bodyBobTimer) * headNodAngle
            : 0f;

        float pitchOffset = 0f;
        if      (isFalling) pitchOffset = -headAirBack;
        else if (isMoving)  pitchOffset = headRunForward + (sprinting ? headRunForward * 0.4f : 0f);

        head.localRotation = Quaternion.Slerp(
            head.localRotation,
            _headRest * Quaternion.Euler(pitchOffset + nod, 0f, 0f),
            Time.deltaTime * 12f
        );
    }

    public void LockInput(bool locked)
    {
        _inputLocked = locked;
    }

    public void FallDie()
    {
        if (_isDead) return;
        _isDead      = true;
        _isFallDying = true;
        _velocity.x  = 0f;
        _velocity.z  = 0f;
        StartCoroutine(FallFlailRoutine());
    }

    void ContinueFalling()
    {
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    IEnumerator FallFlailRoutine()
    {
        float t = 0f;
        while (_isFallDying)
        {
            t += Time.deltaTime;

            if (rightLeg) rightLeg.localRotation = _rightLegRest *
                Quaternion.Euler(Mathf.Sin(t * 7.3f) * 50f, 0f, Mathf.Sin(t * 4.1f) * 25f);
            if (leftLeg)  leftLeg.localRotation  = _leftLegRest *
                Quaternion.Euler(Mathf.Sin(t * 6.8f + 1.2f) * 50f, 0f, Mathf.Sin(t * 5.3f) * 25f);

            if (rightWing) rightWing.localRotation = _rightWingRest *
                Quaternion.Euler(0f, 0f, Mathf.Sin(t * 11f) * 60f);
            if (leftWing)  leftWing.localRotation  = _leftWingRest *
                Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9.5f + 0.8f) * 60f);

            if (head) head.localRotation = _headRest *
                Quaternion.Euler(Mathf.Sin(t * 8f) * 20f, Mathf.Sin(t * 5.5f) * 30f, 0f);

            transform.Rotate(0f, 90f * Time.deltaTime, 0f);

            yield return null;
        }
    }

    public void Die()
    {
        if (_isDead) return;
        _isDead   = true;
        _velocity = Vector3.zero;
        StartCoroutine(LimpRoutine());
    }

    IEnumerator LimpRoutine()
    {
        Quaternion deadRightLeg  = _rightLegRest  * Quaternion.Euler( 40f,  10f,  20f);
        Quaternion deadLeftLeg   = _leftLegRest   * Quaternion.Euler( 40f, -10f, -20f);
        Quaternion deadRightWing = _rightWingRest * Quaternion.Euler( 20f,   0f,  55f);
        Quaternion deadLeftWing  = _leftWingRest  * Quaternion.Euler( 20f,   0f, -55f);
        Quaternion deadHead      = _headRest      * Quaternion.Euler(-30f,  15f,  10f);

        Quaternion startRL = rightLeg  ? rightLeg.localRotation  : Quaternion.identity;
        Quaternion startLL = leftLeg   ? leftLeg.localRotation   : Quaternion.identity;
        Quaternion startRW = rightWing ? rightWing.localRotation : Quaternion.identity;
        Quaternion startLW = leftWing  ? leftWing.localRotation  : Quaternion.identity;
        Quaternion startH  = head      ? head.localRotation      : Quaternion.identity;

        float elapsed      = 0f;
        float dropDuration = 0.35f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / dropDuration);

            if (rightLeg)  rightLeg.localRotation  = Quaternion.Slerp(startRL, deadRightLeg,  t);
            if (leftLeg)   leftLeg.localRotation   = Quaternion.Slerp(startLL, deadLeftLeg,   t);
            if (rightWing) rightWing.localRotation = Quaternion.Slerp(startRW, deadRightWing, t);
            if (leftWing)  leftWing.localRotation  = Quaternion.Slerp(startLW, deadLeftWing,  t);
            if (head)      head.localRotation      = Quaternion.Slerp(startH,  deadHead,      t);
            yield return null;
        }

        float wobbleTime     = 0f;
        float wobbleDuration = 1.2f;
        while (wobbleTime < wobbleDuration)
        {
            wobbleTime += Time.deltaTime;
            float decay = 1f - Mathf.SmoothStep(0f, 1f, wobbleTime / wobbleDuration);
            float swing = Mathf.Sin(wobbleTime * 8f) * 6f * decay;

            if (rightWing) rightWing.localRotation = deadRightWing * Quaternion.Euler(0f, 0f,  swing);
            if (leftWing)  leftWing.localRotation  = deadLeftWing  * Quaternion.Euler(0f, 0f, -swing);
            if (head)      head.localRotation      = deadHead      * Quaternion.Euler(swing * 0.5f, 0f, 0f);
            yield return null;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (_velocity.y >= 0f) return;
        BunPlatform bun = hit.collider.GetComponent<BunPlatform>();
        if (bun != null) bun.TriggerBounce(this);
    }

    public void ApplyBounce(float force)
    {
        _velocity.y = force;
    }
}
