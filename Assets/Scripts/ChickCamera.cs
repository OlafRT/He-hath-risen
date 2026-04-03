using UnityEngine;

public class ChickCamera : MonoBehaviour
{
    public Transform target;

    public Vector3 targetOffset = new Vector3(0f, 1f, 0f);

    public float distance    = 6f;
    public float heightOffset = 1.5f;
    public float mouseSensitivityX = 3f;
    public float mouseSensitivityY = 2f;

    public float minPitch = -20f;
    public float maxPitch =  60f;

    public float positionSmoothing = 12f;
    public float rotationSmoothing = 20f;

    public bool  enableCollision  = true;
    public float collisionRadius  = 0.3f;
    public LayerMask collisionMask = ~0;

    float _yaw;
    float _pitch;

    Vector3    _smoothPosition;
    Quaternion _smoothRotation;


    void Start()
    {
        _yaw   = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _smoothPosition = transform.position;
        _smoothRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (!target) return;

        HandleInput();
        PositionCamera();
    }

    void HandleInput()
    {
        _yaw   += Input.GetAxis("Mouse X") * mouseSensitivityX;
        _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
        _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    void PositionCamera()
    {
        Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 lookAtPoint  = target.position + targetOffset;
        Vector3 orbitOffset  = targetRotation * new Vector3(0f, heightOffset, -distance);
        Vector3 desiredPos   = lookAtPoint + orbitOffset;

        if (enableCollision)
        {
            Vector3 dir      = desiredPos - lookAtPoint;
            float   maxDist  = dir.magnitude;

            if (Physics.SphereCast(lookAtPoint, collisionRadius, dir.normalized,
                                   out RaycastHit hit, maxDist, collisionMask))
            {
                desiredPos = hit.point + hit.normal * collisionRadius;
            }
        }

        _smoothPosition = Vector3.Lerp(_smoothPosition, desiredPos,
                                       Time.deltaTime * positionSmoothing);
        _smoothRotation = Quaternion.Slerp(_smoothRotation, targetRotation,
                                           Time.deltaTime * rotationSmoothing);

        transform.position = _smoothPosition;
        transform.rotation = _smoothRotation;
        transform.LookAt(lookAtPoint);
    }


    public Vector3 GetCameraForward()
    {
        return Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
    }

    public Vector3 GetCameraRight()
    {
        return Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
    }

    public void Detach()
    {
        target = null;
    }
}