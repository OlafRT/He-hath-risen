using System.Collections;
using UnityEngine;

public class ChickDanceController : MonoBehaviour
{
    public Transform body;
    public Transform rightLeg;
    public Transform leftLeg;
    public Transform rightWing;
    public Transform leftWing;
    public Transform head;

    public float bpm                = 130f;
    public int   beatsPerMove       = 8;

    public float bopLegKickAngle    = 42f;
    public float bopHeadNodAngle    = 18f;
    public float bopBodyBounceHeight= 0.08f;
    public float bopBodySquish      = 0.18f;

    public float flapWingAngle      = 65f;
    public float flapLegShuffleAng  = 22f;
    public float flapBodyLean       = 10f;

    public float wiggleBodySway     = 14f;
    public float wiggleHeadRoll     = 20f;
    public float wiggleLegSplay     = 18f;
    public float wiggleWingDrift    = 25f;

    public float spinDegreesPerSec  = 360f;
    public float spinWingSpread     = 55f;
    public float spinHeadTilt       = 22f;
    public float spinBodyBob        = 0.06f;

    public float dropSquishIntensity= 0.32f;
    public float dropWingFling      = 70f;
    public float dropLegSplay       = 35f;

    public float transitionSpeed    = 14f;

    Vector3    _bodyRestPos;
    Vector3    _bodyRestScale;
    Quaternion _rightLegRest, _leftLegRest;
    Quaternion _rightWingRest, _leftWingRest;
    Quaternion _headRest;
    Quaternion _bodyRestRot;

    float _beatDuration;
    float _beatTimer;
    float _beatPhase;
    int   _beatCount;
    int   _currentMove;
    int   _totalMoves = 5;

    float _danceTimer;

    void Start()
    {
        _beatDuration = 60f / bpm;

        if (body)      { _bodyRestPos = body.localPosition; _bodyRestScale = body.localScale; _bodyRestRot = body.localRotation; }
        if (rightLeg)  _rightLegRest  = rightLeg.localRotation;
        if (leftLeg)   _leftLegRest   = leftLeg.localRotation;
        if (rightWing) _rightWingRest = rightWing.localRotation;
        if (leftWing)  _leftWingRest  = leftWing.localRotation;
        if (head)      _headRest      = head.localRotation;
    }

    void Update()
    {
        TickBeat();
        _danceTimer += Time.deltaTime;

        switch (_currentMove)
        {
            case 0: DanceBop();    break;
            case 1: DanceFlap();   break;
            case 2: DanceWiggle(); break;
            case 3: DanceSpin();   break;
            case 4: DanceDrop();   break;
        }
    }

    void TickBeat()
    {
        _beatTimer += Time.deltaTime;
        while (_beatTimer >= _beatDuration)
        {
            _beatTimer -= _beatDuration;
            _beatCount++;

            if (_beatCount % beatsPerMove == 0)
            {
                _currentMove = (_currentMove + 1) % _totalMoves;
                _danceTimer  = 0f;
            }
        }
        _beatPhase = _beatTimer / _beatDuration;
    }


    void DanceBop()
    {
        if (!rightLeg || !leftLeg) return;

        bool rightDown = (_beatCount % 2 == 0);

        float kick = Mathf.Sin(_beatPhase * Mathf.PI) * bopLegKickAngle;

        Quaternion rLegTarget = _rightLegRest * Quaternion.Euler( rightDown ?  kick : -kick * 0.25f, 0f, 0f);
        Quaternion lLegTarget = _leftLegRest  * Quaternion.Euler(!rightDown ?  kick : -kick * 0.25f, 0f, 0f);

        rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, rLegTarget, Time.deltaTime * transitionSpeed);
        leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,  lLegTarget, Time.deltaTime * transitionSpeed);

        float nod    = Mathf.Sin(_beatPhase * Mathf.PI * 2f) * bopHeadNodAngle;
        if (head)
            head.localRotation = Quaternion.Slerp(head.localRotation,
                _headRest * Quaternion.Euler(nod, 0f, 0f),
                Time.deltaTime * transitionSpeed);

        if (rightWing && leftWing)
        {
            float drift = Mathf.Sin(_danceTimer * 3.5f) * 8f;
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation, _rightWingRest * Quaternion.Euler(0f, 0f, -drift), Time.deltaTime * 8f);
            leftWing.localRotation  = Quaternion.Slerp(leftWing.localRotation,  _leftWingRest  * Quaternion.Euler(0f, 0f,  drift), Time.deltaTime * 8f);
        }

        if (body)
        {
            float bounce = Mathf.Abs(Mathf.Sin(_beatPhase * Mathf.PI)) * bopBodyBounceHeight;
            float squish = Mathf.Sin(_beatPhase * Mathf.PI) * bopBodySquish;
            Vector3 targetScale = new Vector3(
                _bodyRestScale.x * (1f + squish * 0.4f),
                _bodyRestScale.y * (1f - squish * 0.5f),
                _bodyRestScale.z * (1f + squish * 0.4f)
            );
            body.localPosition = Vector3.Lerp(body.localPosition, _bodyRestPos + new Vector3(0f, bounce, 0f), Time.deltaTime * 20f);
            body.localScale    = Vector3.Lerp(body.localScale,    targetScale,                                 Time.deltaTime * 22f);
            body.localRotation = Quaternion.Slerp(body.localRotation, _bodyRestRot, Time.deltaTime * 8f);
        }
    }

    void DanceFlap()
    {
        float flapT   = Mathf.Sin(_beatPhase * Mathf.PI * 2f) * flapWingAngle;

        if (rightWing && leftWing)
        {
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation,
                _rightWingRest * Quaternion.Euler(0f, 0f, -(flapT + flapWingAngle * 0.4f)),
                Time.deltaTime * transitionSpeed);
            leftWing.localRotation = Quaternion.Slerp(leftWing.localRotation,
                _leftWingRest  * Quaternion.Euler(0f, 0f,  (flapT + flapWingAngle * 0.4f)),
                Time.deltaTime * transitionSpeed);
        }

        if (rightLeg && leftLeg)
        {
            float shuffle = Mathf.Sin(_danceTimer * (bpm / 30f)) * flapLegShuffleAng;
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation,
                _rightLegRest * Quaternion.Euler(0f, 0f,  shuffle),
                Time.deltaTime * transitionSpeed);
            leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,
                _leftLegRest  * Quaternion.Euler(0f, 0f, -shuffle),
                Time.deltaTime * transitionSpeed);
        }

        if (body)
        {
            float lean = Mathf.Sin(_beatPhase * Mathf.PI * 2f) * flapBodyLean;
            body.localRotation = Quaternion.Slerp(body.localRotation,
                _bodyRestRot * Quaternion.Euler(lean, 0f, 0f),
                Time.deltaTime * 10f);
            body.localPosition = Vector3.Lerp(body.localPosition, _bodyRestPos, Time.deltaTime * 12f);
            body.localScale    = Vector3.Lerp(body.localScale,    _bodyRestScale, Time.deltaTime * 12f);
        }

        if (head)
        {
            float nod = Mathf.Sin(_danceTimer * (bpm / 30f)) * 14f;
            head.localRotation = Quaternion.Slerp(head.localRotation,
                _headRest * Quaternion.Euler(nod, 0f, 0f),
                Time.deltaTime * transitionSpeed);
        }
    }

    void DanceWiggle()
    {
        float swayRate = (bpm / 60f) * 0.5f;
        float sway     = Mathf.Sin(_danceTimer * swayRate * Mathf.PI * 2f) * wiggleBodySway;

        if (body)
        {
            body.localRotation = Quaternion.Slerp(body.localRotation,
                _bodyRestRot * Quaternion.Euler(0f, 0f, sway),
                Time.deltaTime * 9f);
            body.localPosition = Vector3.Lerp(body.localPosition, _bodyRestPos, Time.deltaTime * 10f);
            body.localScale    = Vector3.Lerp(body.localScale,    _bodyRestScale, Time.deltaTime * 12f);
        }

        if (head)
        {
            float headRoll = Mathf.Sin(_danceTimer * swayRate * Mathf.PI * 2f + 0.4f) * wiggleHeadRoll;
            head.localRotation = Quaternion.Slerp(head.localRotation,
                _headRest * Quaternion.Euler(0f, 0f, -headRoll),
                Time.deltaTime * 10f);
        }

        if (rightLeg && leftLeg)
        {
            float splayAmt = Mathf.Abs(Mathf.Sin(_danceTimer * swayRate * Mathf.PI * 2f)) * wiggleLegSplay;
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation,
                _rightLegRest * Quaternion.Euler(0f, 0f,  splayAmt),
                Time.deltaTime * transitionSpeed);
            leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,
                _leftLegRest  * Quaternion.Euler(0f, 0f, -splayAmt),
                Time.deltaTime * transitionSpeed);
        }

        if (rightWing && leftWing)
        {
            float wingTrail = Mathf.Sin(_danceTimer * swayRate * Mathf.PI * 2f - 0.6f) * wiggleWingDrift;
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation,
                _rightWingRest * Quaternion.Euler(0f, 0f, -wingTrail),
                Time.deltaTime * 8f);
            leftWing.localRotation  = Quaternion.Slerp(leftWing.localRotation,
                _leftWingRest  * Quaternion.Euler(0f, 0f,  wingTrail),
                Time.deltaTime * 8f);
        }
    }

    void DanceSpin()
    {
        transform.Rotate(0f, spinDegreesPerSec * Time.deltaTime, 0f, Space.World);

        if (rightWing && leftWing)
        {
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation,
                _rightWingRest * Quaternion.Euler(0f, 0f, -spinWingSpread),
                Time.deltaTime * transitionSpeed);
            leftWing.localRotation  = Quaternion.Slerp(leftWing.localRotation,
                _leftWingRest  * Quaternion.Euler(0f, 0f,  spinWingSpread),
                Time.deltaTime * transitionSpeed);
        }

        if (head)
        {
            head.localRotation = Quaternion.Slerp(head.localRotation,
                _headRest * Quaternion.Euler(0f, 0f, spinHeadTilt),
                Time.deltaTime * transitionSpeed);
        }

        if (rightLeg && leftLeg)
        {
            float march = Mathf.Sin(_danceTimer * (bpm / 30f)) * 30f;
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation,
                _rightLegRest * Quaternion.Euler( march, 0f, 0f),
                Time.deltaTime * transitionSpeed);
            leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,
                _leftLegRest  * Quaternion.Euler(-march, 0f, 0f),
                Time.deltaTime * transitionSpeed);
        }

        if (body)
        {
            float bob = Mathf.Abs(Mathf.Sin(_danceTimer * (bpm / 30f))) * spinBodyBob;
            body.localPosition = Vector3.Lerp(body.localPosition, _bodyRestPos + new Vector3(0f, bob, 0f), Time.deltaTime * 16f);
            body.localScale    = Vector3.Lerp(body.localScale,    _bodyRestScale, Time.deltaTime * 12f);
            body.localRotation = Quaternion.Slerp(body.localRotation, _bodyRestRot, Time.deltaTime * 8f);
        }
    }


    void DanceDrop()
    {
        float dropCurve = Mathf.Pow(Mathf.Abs(Mathf.Sin(_beatPhase * Mathf.PI * 2f)), 2.5f);

        if (body)
        {
            Vector3 targetScale = new Vector3(
                _bodyRestScale.x * (1f + dropCurve * dropSquishIntensity * 0.7f),
                _bodyRestScale.y * (1f - dropCurve * dropSquishIntensity),
                _bodyRestScale.z * (1f + dropCurve * dropSquishIntensity * 0.7f)
            );
            float dropY = -dropCurve * 0.05f;
            body.localScale    = Vector3.Lerp(body.localScale,    targetScale,                                   Time.deltaTime * 28f);
            body.localPosition = Vector3.Lerp(body.localPosition, _bodyRestPos + new Vector3(0f, dropY, 0f),     Time.deltaTime * 28f);
            body.localRotation = Quaternion.Slerp(body.localRotation, _bodyRestRot, Time.deltaTime * 8f);
        }

        if (rightLeg && leftLeg)
        {
            float splay = dropCurve * dropLegSplay;
            rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation,
                _rightLegRest * Quaternion.Euler(splay * 0.6f, 0f,  splay),
                Time.deltaTime * transitionSpeed);
            leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,
                _leftLegRest  * Quaternion.Euler(splay * 0.6f, 0f, -splay),
                Time.deltaTime * transitionSpeed);
        }

        if (rightWing && leftWing)
        {
            float fling = dropCurve * dropWingFling;
            rightWing.localRotation = Quaternion.Slerp(rightWing.localRotation,
                _rightWingRest * Quaternion.Euler(0f, 0f, -fling),
                Time.deltaTime * transitionSpeed);
            leftWing.localRotation  = Quaternion.Slerp(leftWing.localRotation,
                _leftWingRest  * Quaternion.Euler(0f, 0f,  fling),
                Time.deltaTime * transitionSpeed);
        }

        if (head)
        {
            float snap = dropCurve * 24f;
            head.localRotation = Quaternion.Slerp(head.localRotation,
                _headRest * Quaternion.Euler(snap, 0f, 0f),
                Time.deltaTime * transitionSpeed);
        }
    }
}
