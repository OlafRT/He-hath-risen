using UnityEngine;

public class CorpseFollower : MonoBehaviour
{
    Transform  _spike;

    Vector3    _posOffset;
    Quaternion _rotOffset;

    public void Init(Transform spike, Vector3 worldPos, Quaternion worldRot)
    {
        _spike     = spike;
        _posOffset = Quaternion.Inverse(spike.rotation) * (worldPos - spike.position);
        _rotOffset = Quaternion.Inverse(spike.rotation) * worldRot;
    }

    void LateUpdate()
    {
        if (_spike == null) return;

        transform.position = _spike.position + _spike.rotation * _posOffset;
        transform.rotation = _spike.rotation * _rotOffset;
    }
}
