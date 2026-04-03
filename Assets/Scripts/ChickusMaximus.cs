using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ChickusMaximus : MonoBehaviour
{

    public Transform body;
    public Transform rightLeg;
    public Transform leftLeg;
    public Transform rightWing;
    public Transform leftWing;
    public Transform head;
    public Transform spikeHandle;
    public Transform spikeTip;

    public float moveSpeed     = 3.8f;
    public float rotationSpeed = 8f;
    public float gravity       = -20f;

    public float detectionRadius = 9f;
    public float loseRadius = 30f;

    public float aggressionRampTime = 25f;
    public float maxSpeedMult = 1.65f;
    public float minCooldownMult = 0.45f;
    public float comboUnlockThreshold = 0.3f;
    public float chargeUnlockThreshold = 0.65f;

    public float attackRange    = 2.2f;
    public float attackCooldown = 1.4f;
    public float lungeDistance  = 1.1f;
    public float lungeDuration  = 0.18f;
    public int   missesBeforeCharge = 3;

    public float circleTimeMin = 0.6f;
    public float circleTimeMax = 1.8f;
    public float circleRadius  = 2.6f;
    public float circleSpeed   = 2.4f;

    public float chargeWindupTime = 0.60f;
    public float chargeSpeed      = 9.5f;
    public float chargeDuration   = 0.50f;
    public float chargeHitRadius  = 0.80f;

    public float feintHesitateTime = 0.38f;

    public float legSwingAngle = 32f;
    public float legSwingSpeed = 8f;

    public float bodyBobHeight = 0.06f;
    public float bodyBobSpeed  = 8f;
    public float bodyRunLean   = 10f;

    public float headNodAngle = 10f;
    public float headIdleLook = 12f;

    public Vector3 spikeCarryRotation  = new Vector3(0f,   0f, -70f);
    public Vector3 spikeWindupRotation = new Vector3(0f, -40f, -50f);
    public float   rightWingIdleDrift  = 4f;

    public float jumpForce          = 10f;
    public float jumpHeightThreshold = 1.4f;
    public float jumpCooldown        = 1.6f;
    public float jumpAttackRange     = 3.5f;

    public AudioSource audioSource;
    public AudioClip stabWindupSFX;
    public AudioClip stabLungeSFX;
    public AudioClip chargeRoarSFX;
    public AudioClip jumpSFX;
    public AudioClip landSFX;
    public AudioClip footstepSFX;
    public float footstepInterval = 0.38f;

    enum State      { Idle, Chase, Circle, Attack }
    enum AttackType { SingleStab, StabCombo, Feint, Charge, JumpAttack }

    State _state = State.Idle;

    CharacterController _cc;
    Transform           _player;

    float _verticalVelocity;
    float _legTimer;
    float _bodyBobTimer;
    float _attackCooldownTimer;
    float _idleLookTimer;
    float _chaseTimer;
    float _circleTimer;
    float _circleDir = 1f;
    int   _consecutiveMisses;
    bool  _isAttacking;
    bool  _isGrounded;
    bool  _wasGrounded;

    float _jumpCooldownTimer;
    float _footstepTimer;

    Vector3 _lungeOrigin;

    Vector3    _bodyRestPos;
    Vector3    _bodyRestScale;
    Quaternion _rightLegRest, _leftLegRest;
    Quaternion _rightWingRest, _leftWingRest;
    Quaternion _headRest;
    Quaternion _spikeCarryRot;
    Quaternion _spikeWindupRot;

    float Aggression      => Mathf.Clamp01(_chaseTimer / aggressionRampTime);
    float SpeedMult       => Mathf.Lerp(1f, maxSpeedMult,    Aggression);
    float CooldownMult    => Mathf.Lerp(1f, minCooldownMult, Aggression);
    float CurrentSpeed    => moveSpeed     * SpeedMult;
    float CurrentCooldown => attackCooldown * CooldownMult;
    float CurrentLunge    => lungeDistance  * Mathf.Lerp(1f, 1.35f, Aggression);
    float CurrentWindup   => Mathf.Lerp(0.22f, 0.13f, Aggression);

    void Start()
    {
        _cc = GetComponent<CharacterController>();

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;

        if (body)      { _bodyRestPos = body.localPosition; _bodyRestScale = body.localScale; }
        if (rightLeg)  _rightLegRest  = rightLeg.localRotation;
        if (leftLeg)   _leftLegRest   = leftLeg.localRotation;
        if (rightWing) _rightWingRest = rightWing.localRotation;
        if (leftWing)  _leftWingRest  = leftWing.localRotation;
        if (head)      _headRest      = head.localRotation;

        _spikeCarryRot  = _leftWingRest * Quaternion.Euler(spikeCarryRotation);
        _spikeWindupRot = _leftWingRest * Quaternion.Euler(spikeWindupRotation);
    }

    void Update()
    {
        if (_isAttacking) return;

        UpdateState();
        HandleMovement();
        HandleAnimation();

        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer -= Time.deltaTime;

        if (_jumpCooldownTimer > 0f)
            _jumpCooldownTimer -= Time.deltaTime;
    }

    void UpdateState()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        switch (_state)
        {
            case State.Idle:
                if (dist <= detectionRadius)
                {
                    _state      = State.Chase;
                    _chaseTimer = 0f;
                }
                break;

            case State.Chase:
                _chaseTimer += Time.deltaTime;

                if (dist > loseRadius)
                {
                    _state      = State.Idle;
                    _chaseTimer = 0f;
                    break;
                }

                if (_attackCooldownTimer <= 0f && dist <= attackRange)
                {
                    AttackType attack = PickAttack(dist);

                    float circleChance = Mathf.Lerp(0.55f, 0.1f, Aggression);
                    if (attack != AttackType.Charge && Random.value < circleChance)
                        StartCoroutine(CircleThenAttack(attack));
                    else
                        LaunchAttack(attack);
                }
                else if (_attackCooldownTimer <= 0f && dist <= attackRange * 2f
                         && Aggression >= 0.2f && Random.value < 0.008f)
                {
                    EnterCircle();
                }
                break;

            case State.Circle:
                _chaseTimer  += Time.deltaTime;
                _circleTimer -= Time.deltaTime;

                if (dist > loseRadius)
                {
                    _state = State.Chase;
                    break;
                }

                if (Random.value < 0.004f) _circleDir *= -1f;

                if (_circleTimer <= 0f || (_attackCooldownTimer <= 0f && dist <= attackRange))
                    LaunchAttack(PickAttack(dist));
                break;

            case State.Attack:
                break;
        }
    }

    AttackType PickAttack(float dist)
    {
        if (_consecutiveMisses >= missesBeforeCharge && Aggression >= chargeUnlockThreshold)
            return AttackType.Charge;

        bool playerIsAirborne = _player != null &&
                                (_player.position.y - transform.position.y) > jumpHeightThreshold * 0.5f;
        float hDist = _player != null
            ? Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                               new Vector2(_player.position.x,   _player.position.z))
            : 999f;

        if (playerIsAirborne && hDist <= jumpAttackRange && _jumpCooldownTimer <= 0f)
            return AttackType.JumpAttack;

        float r = Random.value;

        if (Aggression >= chargeUnlockThreshold && dist > attackRange * 1.1f && r < 0.28f)
            return AttackType.Charge;

        if (Aggression >= comboUnlockThreshold)
        {
            if (r < 0.35f) return AttackType.StabCombo;
            if (r < 0.56f) return AttackType.Feint;
        }

        return AttackType.SingleStab;
    }

    void LaunchAttack(AttackType type)
    {
        _state = State.Attack;
        switch (type)
        {
            case AttackType.SingleStab: StartCoroutine(SingleStabRoutine()); break;
            case AttackType.StabCombo:  StartCoroutine(StabComboRoutine());  break;
            case AttackType.Feint:      StartCoroutine(FeintRoutine());       break;
            case AttackType.Charge:     StartCoroutine(ChargeRoutine());      break;
            case AttackType.JumpAttack: StartCoroutine(JumpAttackRoutine());  break;
        }
    }

    void EnterCircle()
    {
        _state       = State.Circle;
        _circleTimer = Random.Range(circleTimeMin, circleTimeMax);
        _circleDir   = Random.value < 0.5f ? 1f : -1f;
    }

    void HandleMovement()
    {
        _wasGrounded = _isGrounded;
        _isGrounded  = _cc.isGrounded;

        if (_isGrounded && !_wasGrounded)
            PlaySound(landSFX);

        if (_player != null)
        {
            float   dist     = Vector3.Distance(transform.position, _player.position);
            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;

            if (_state == State.Chase && toPlayer.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                    Time.deltaTime * rotationSpeed);

                if (dist > attackRange * 0.8f)
                    _cc.Move(toPlayer.normalized * CurrentSpeed * Time.deltaTime);

                float heightDiff = _player.position.y - transform.position.y;
                if (_isGrounded && heightDiff > jumpHeightThreshold && _jumpCooldownTimer <= 0f)
                {
                    _verticalVelocity  = jumpForce;
                    _jumpCooldownTimer = jumpCooldown;
                    PlaySound(jumpSFX);
                }
            }
            else if (_state == State.Circle && toPlayer.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(toPlayer.normalized),
                    Time.deltaTime * rotationSpeed * 1.5f);

                Vector3 right      = Vector3.Cross(Vector3.up, toPlayer.normalized);
                Vector3 strafe     = right * _circleDir;
                float   distDelta  = dist - circleRadius;
                Vector3 correction = toPlayer.normalized * Mathf.Clamp(distDelta, -1f, 1f) * 0.5f;

                _cc.Move((strafe + correction).normalized * circleSpeed * SpeedMult * Time.deltaTime);
            }
        }

        if (_isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
        _verticalVelocity += gravity * Time.deltaTime;
        _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
    }

    IEnumerator SingleStabRoutine()
    {
        _isAttacking         = true;
        _attackCooldownTimer = CurrentCooldown;

        yield return StartCoroutine(LockOnRoutine(0.15f));
        yield return StartCoroutine(WindupRoutine(CurrentWindup));
        yield return new WaitForSeconds(0.08f);
        yield return StartCoroutine(DoLunge(CurrentLunge, lungeDuration));

        _consecutiveMisses++;

        yield return StartCoroutine(DoRecover(0.3f));

        _isAttacking = false;
        _state       = State.Chase;
    }

    IEnumerator StabComboRoutine()
    {
        _isAttacking         = true;
        _attackCooldownTimer = CurrentCooldown;

        int stabs = Aggression >= 0.8f ? 3 : 2;

        for (int i = 0; i < stabs; i++)
        {
            yield return StartCoroutine(LockOnRoutine(0.08f));

            float elapsed = 0f;
            while (elapsed < 0.13f)
            {
                elapsed += Time.deltaTime;
                if (leftWing) leftWing.localRotation =
                    Quaternion.Slerp(_spikeCarryRot, _spikeWindupRot, elapsed / 0.13f);
                yield return null;
            }

            Vector3 comboOrigin = transform.position;
            yield return StartCoroutine(DoLunge(CurrentLunge * 0.85f, lungeDuration * 0.85f));

            _consecutiveMisses++;

            Vector3 retractFrom  = transform.position;
            float   retractTimer = 0f;
            while (retractTimer < 0.15f)
            {
                retractTimer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, retractTimer / 0.15f);
                transform.position = Vector3.Lerp(retractFrom, comboOrigin, t);
                if (leftWing) leftWing.localRotation =
                    Quaternion.Slerp(leftWing.localRotation, _spikeCarryRot, t);
                yield return null;
            }

            if (i < stabs - 1)
                yield return new WaitForSeconds(0.08f);
        }

        _isAttacking = false;
        _state       = State.Chase;
    }

    IEnumerator FeintRoutine()
    {
        _isAttacking         = true;
        _attackCooldownTimer = CurrentCooldown;

        yield return StartCoroutine(LockOnRoutine(0.15f));

        yield return StartCoroutine(WindupRoutine(0.22f));

        yield return new WaitForSeconds(feintHesitateTime);

        float   sideDir   = Random.value < 0.5f ? 1f : -1f;
        Vector3 sideStart = transform.position;
        Vector3 sideEnd   = sideStart + transform.right * sideDir * 0.65f;
        float   elapsed   = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(sideStart, sideEnd,
                Mathf.SmoothStep(0f, 1f, elapsed / 0.12f));
            FacePlayer();
            yield return null;
        }

        yield return StartCoroutine(DoLunge(CurrentLunge * 1.2f, lungeDuration));

        _consecutiveMisses++;

        yield return StartCoroutine(DoRecover(0.3f));

        _isAttacking = false;
        _state       = State.Chase;
    }

    IEnumerator ChargeRoutine()
    {
        _isAttacking         = true;
        _attackCooldownTimer = CurrentCooldown * 1.4f;
        _consecutiveMisses   = 0;

        PlaySound(chargeRoarSFX);
        float elapsed = 0f;
        while (elapsed < chargeWindupTime)
        {
            elapsed += Time.deltaTime;
            FacePlayer();
            if (body) body.localRotation = Quaternion.Slerp(body.localRotation,
                Quaternion.Euler(-bodyRunLean * 2.5f, 0f, 0f), Time.deltaTime * 5f);
            if (leftWing) leftWing.localRotation = Quaternion.Slerp(leftWing.localRotation,
                _spikeWindupRot, Time.deltaTime * 8f);
            yield return null;
        }

        Vector3 chargeDir = transform.forward;
        elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            _cc.Move(chargeDir * chargeSpeed * Time.deltaTime);

            if (_isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;
            _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(0.45f);

        _isAttacking = false;
        _state       = State.Chase;
    }

    IEnumerator CircleThenAttack(AttackType attack)
    {
        EnterCircle();
        float circleFor = Random.Range(circleTimeMin * 0.4f, circleTimeMax * 0.5f);
        yield return new WaitForSeconds(circleFor);
        if (!_isAttacking)
            LaunchAttack(attack);
    }

    IEnumerator LockOnRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            FacePlayer();
            yield return null;
        }
    }

    IEnumerator WindupRoutine(float duration)
    {
        PlaySound(stabWindupSFX);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (leftWing) leftWing.localRotation =
                Quaternion.Slerp(_spikeCarryRot, _spikeWindupRot, t);
            if (head) head.localRotation = Quaternion.Slerp(head.localRotation,
                _headRest * Quaternion.Euler(-headIdleLook, 0f, 0f), Time.deltaTime * 12f);
            yield return null;
        }
    }

    IEnumerator DoLunge(float distance, float duration)
    {
        PlaySound(stabLungeSFX);
        _lungeOrigin  = transform.position;
        Vector3 target = _lungeOrigin + transform.forward * distance;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(_lungeOrigin, target, t);

            if (leftWing != null && _player != null)
                AimSpikeAtPlayer(_player.position);

            yield return null;
        }
    }

    IEnumerator DoRecover(float duration)
    {
        Vector3 recoverStart = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(recoverStart, _lungeOrigin, t);
            if (leftWing) leftWing.localRotation =
                Quaternion.Slerp(leftWing.localRotation, _spikeCarryRot, t);
            yield return null;
        }
    }

    void FacePlayer()
    {
        if (_player == null) return;
        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(toPlayer.normalized),
                Time.deltaTime * 20f);
    }

    void AimSpikeAtPlayer(Vector3 targetPos)
    {
        if (leftWing == null) return;

        if (spikeHandle == null || spikeTip == null)
        {
            Vector3 fallback = (targetPos - leftWing.position).normalized;
            if (fallback.sqrMagnitude > 0.01f)
                leftWing.rotation = Quaternion.LookRotation(fallback);
            return;
        }

        Vector3 currentDir = (spikeTip.position - spikeHandle.position).normalized;
        Vector3 desiredDir = (targetPos - spikeHandle.position).normalized;

        if (currentDir.sqrMagnitude < 0.01f || desiredDir.sqrMagnitude < 0.01f) return;

        Quaternion aimDelta = Quaternion.FromToRotation(currentDir, desiredDir);
        leftWing.rotation = Quaternion.Slerp(leftWing.rotation,
            aimDelta * leftWing.rotation, Time.deltaTime * 35f);
    }

    IEnumerator JumpAttackRoutine()
    {
        _isAttacking         = true;
        _attackCooldownTimer = CurrentCooldown;
        _jumpCooldownTimer   = jumpCooldown * 1.5f;

        yield return StartCoroutine(LockOnRoutine(0.12f));
        PlaySound(stabWindupSFX);

        float windElapsed = 0f;
        while (windElapsed < 0.15f)
        {
            windElapsed += Time.deltaTime;
            if (leftWing) leftWing.localRotation =
                Quaternion.Slerp(_spikeCarryRot, _spikeWindupRot, windElapsed / 0.15f);
            if (head) head.localRotation = Quaternion.Slerp(head.localRotation,
                _headRest * Quaternion.Euler(-headIdleLook, 0f, 0f), Time.deltaTime * 14f);
            yield return null;
        }

        if (_player != null) FacePlayer();
        Vector3 toPlayerWorld = _player != null
            ? (_player.position - transform.position).normalized
            : transform.forward;

        float hSpeed = CurrentSpeed * 1.4f;
        _verticalVelocity = jumpForce * 1.1f;
        PlaySound(jumpSFX);

        bool thrustPlayed = false;

        float airTime = 0f;
        float maxAirTime = 0.7f;

        while (!_isGrounded && airTime < maxAirTime)
        {
            airTime += Time.deltaTime;

            _cc.Move(toPlayerWorld * hSpeed * Time.deltaTime);

            if (leftWing != null && _player != null)
                AimSpikeAtPlayer(_player.position);

            if (!thrustPlayed && _verticalVelocity <= 0f)
            {
                thrustPlayed = true;
                PlaySound(stabLungeSFX);
            }

            _verticalVelocity += gravity * Time.deltaTime;
            _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(0.25f);

        if (leftWing) leftWing.localRotation =
            Quaternion.Slerp(leftWing.localRotation, _spikeCarryRot, 1f);

        _consecutiveMisses++;
        _isAttacking = false;
        _state       = State.Chase;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void HandleAnimation()
    {
        bool isMoving = _state == State.Chase || _state == State.Circle;

        if (isMoving)
        {
            _legTimer     += Time.deltaTime * legSwingSpeed * SpeedMult;
            _bodyBobTimer += Time.deltaTime * bodyBobSpeed  * SpeedMult;

            if (_isGrounded)
            {
                _footstepTimer -= Time.deltaTime * SpeedMult;
                if (_footstepTimer <= 0f)
                {
                    PlaySound(footstepSFX);
                    _footstepTimer = footstepInterval;
                }
            }
        }

        _idleLookTimer += Time.deltaTime;

        AnimateLegs(isMoving);
        AnimateBody(isMoving);
        AnimateWings();
        AnimateHead(isMoving);
    }

    void AnimateLegs(bool isMoving)
    {
        if (!rightLeg || !leftLeg) return;

        if (isMoving)
        {
            float rAngle = Mathf.Sin(_legTimer)            * legSwingAngle;
            float lAngle = Mathf.Sin(_legTimer + Mathf.PI) * legSwingAngle;
            rightLeg.localRotation = _rightLegRest * Quaternion.Euler(rAngle, 0f, 0f);
            leftLeg.localRotation  = _leftLegRest  * Quaternion.Euler(lAngle, 0f, 0f);
        }
        else
        {
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, _rightLegRest, Time.deltaTime * 8f);
            leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,  _leftLegRest,  Time.deltaTime * 8f);
        }
    }

    void AnimateBody(bool isMoving)
    {
        if (!body) return;

        float bobY = isMoving
            ? Mathf.Abs(Mathf.Sin(_bodyBobTimer)) * bodyBobHeight
            : 0f;

        body.localPosition = Vector3.Lerp(body.localPosition,
            _bodyRestPos + new Vector3(0f, bobY, 0f), Time.deltaTime * 15f);

        float lean = isMoving ? bodyRunLean : 0f;
        body.localRotation = Quaternion.Slerp(body.localRotation,
            Quaternion.Euler(lean, 0f, 0f), Time.deltaTime * 7f);

        body.localScale = Vector3.Lerp(body.localScale, _bodyRestScale, Time.deltaTime * 10f);
    }

    void AnimateWings()
    {
        if (!_isAttacking && leftWing)
            leftWing.localRotation = Quaternion.Slerp(leftWing.localRotation,
                _spikeCarryRot, Time.deltaTime * 8f);

        if (rightWing)
        {
            float driftSpeed = Mathf.Lerp(1.6f, 3.5f, Aggression);
            float drift = Mathf.Sin(Time.time * driftSpeed) * rightWingIdleDrift;
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation,
                _rightWingRest * Quaternion.Euler(0f, 0f, drift), Time.deltaTime * 6f);
        }
    }

    void AnimateHead(bool isMoving)
    {
        if (!head || _isAttacking) return;

        Quaternion targetRot;
        if (isMoving)
        {
            float intensity = Mathf.Lerp(1f, 1.6f, Aggression);
            float nod = Mathf.Sin(_bodyBobTimer) * headNodAngle * intensity;
            targetRot = _headRest * Quaternion.Euler(nod + 8f, 0f, 0f);
        }
        else
        {
            float scan = Mathf.Sin(_idleLookTimer * 0.7f) * headIdleLook;
            targetRot  = _headRest * Quaternion.Euler(0f, scan, 0f);
        }

        head.localRotation = Quaternion.Slerp(head.localRotation, targetRot, Time.deltaTime * 6f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, loseRadius);

        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0.6f, 0f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, chargeHitRadius);
    }
#endif
}
