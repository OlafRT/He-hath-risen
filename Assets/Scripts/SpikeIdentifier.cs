using System;
using System.Collections.Generic;
using UnityEngine;

public class SpikeIdentifier : MonoBehaviour
{
    [SerializeField]
    string _id;

    public string Id => _id;

    static readonly Dictionary<string, SpikeIdentifier> s_registry
        = new Dictionary<string, SpikeIdentifier>();

    void OnEnable()
    {
        if (string.IsNullOrEmpty(_id))
        {
            Debug.LogWarning($"[SpikeIdentifier] '{name}' has no ID set. " +
                             "Right-click the component and choose 'Generate New ID'.", this);
            return;
        }
        s_registry[_id] = this;
    }

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(_id))
            s_registry.Remove(_id);
    }

    public static bool TryGet(string id, out Transform spikeTransform)
    {
        spikeTransform = null;
        if (string.IsNullOrEmpty(id)) return false;
        if (!s_registry.TryGetValue(id, out SpikeIdentifier found)) return false;
        spikeTransform = found.transform;
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate New ID")]
    void GenerateNewId()
    {
        _id = Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[SpikeIdentifier] Generated ID for '{name}': {_id}");
    }
#endif
}
