using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CorpseData
{
    public SerializedVector3    position;
    public SerializedQuaternion rotation;

    public string               spikeId;
    public SerializedVector3    localPosition;
    public SerializedQuaternion localRotation;

    public SerializedQuaternion bodyRot;
    public SerializedVector3    bodyScale;
    public SerializedQuaternion rightLegRot;
    public SerializedQuaternion leftLegRot;
    public SerializedQuaternion rightWingRot;
    public SerializedQuaternion leftWingRot;
    public SerializedQuaternion headRot;
}

[Serializable] public struct SerializedVector3
{
    public float x, y, z;
    public SerializedVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable] public struct SerializedQuaternion
{
    public float x, y, z, w;
    public SerializedQuaternion(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
}

[Serializable]
class CorpseDataList { public List<CorpseData> corpses = new List<CorpseData>(); }

public class CorpseManager : MonoBehaviour
{
    public static CorpseManager Instance { get; private set; }

    public GameObject corpsePrefab;
    public int maxCorpses = 20;

    const string PREFS_KEY = "ChickCorpses";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SpawnSavedCorpses();
    }

    public void RegisterCorpse(CorpseData data, Transform spikeTransform = null)
    {
        if (spikeTransform != null)
        {
            SpikeIdentifier identifier = spikeTransform.GetComponent<SpikeIdentifier>();
            if (identifier != null)
            {
                data.spikeId = identifier.Id;

                Vector3    worldPos = data.position.ToVector3();
                Quaternion worldRot = data.rotation.ToQuaternion();

                data.localPosition = new SerializedVector3(
                    Quaternion.Inverse(spikeTransform.rotation) * (worldPos - spikeTransform.position));
                data.localRotation = new SerializedQuaternion(
                    Quaternion.Inverse(spikeTransform.rotation) * worldRot);
            }
        }

        CorpseDataList list = LoadList();
        list.corpses.Add(data);

        while (list.corpses.Count > maxCorpses)
            list.corpses.RemoveAt(0);

        SaveList(list);
        SpawnCorpse(data, spikeTransform);
    }

    void SpawnSavedCorpses()
    {
        CorpseDataList list = LoadList();
        foreach (CorpseData d in list.corpses)
            SpawnCorpse(d);
    }

    void SpawnCorpse(CorpseData d, Transform spikeTransform = null)
    {
        if (corpsePrefab == null) return;

        Vector3    worldPos;
        Quaternion worldRot;
        Transform  followerSpike = null;

        if (spikeTransform != null)
        {
            worldPos     = d.position.ToVector3();
            worldRot     = d.rotation.ToQuaternion();
            followerSpike = spikeTransform;
        }
        else if (!string.IsNullOrEmpty(d.spikeId)
                 && SpikeIdentifier.TryGet(d.spikeId, out Transform spike))
        {
            worldPos     = spike.position + spike.rotation * d.localPosition.ToVector3();
            worldRot     = spike.rotation * d.localRotation.ToQuaternion();
            followerSpike = spike;
        }
        else
        {
            worldPos = d.position.ToVector3();
            worldRot = d.rotation.ToQuaternion();
        }

        GameObject go = Instantiate(corpsePrefab, worldPos, worldRot);

        if (followerSpike != null)
            go.AddComponent<CorpseFollower>().Init(followerSpike, worldPos, worldRot);

        ApplyLimbRotations(go, d);
    }

    void ApplyLimbRotations(GameObject root, CorpseData d)
    {
        Transform body = root.transform.Find("Body");
        if (body == null) return;

        body.localRotation = d.bodyRot.ToQuaternion();
        body.localScale    = d.bodyScale.ToVector3();

        SetLocalRot(body, "RightLeg",  d.rightLegRot);
        SetLocalRot(body, "LeftLeg",   d.leftLegRot);
        SetLocalRot(body, "RightWing", d.rightWingRot);
        SetLocalRot(body, "LeftWing",  d.leftWingRot);
        SetLocalRot(body, "Head",      d.headRot);
    }

    void SetLocalRot(Transform parent, string childName, SerializedQuaternion rot)
    {
        Transform t = parent.Find(childName);
        if (t != null) t.localRotation = rot.ToQuaternion();
    }

    CorpseDataList LoadList()
    {
        if (!PlayerPrefs.HasKey(PREFS_KEY))
            return new CorpseDataList();
        try
        {
            return JsonUtility.FromJson<CorpseDataList>(PlayerPrefs.GetString(PREFS_KEY))
                   ?? new CorpseDataList();
        }
        catch { return new CorpseDataList(); }
    }

    void SaveList(CorpseDataList list)
    {
        PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    public void ClearAllCorpses()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY);
        PlayerPrefs.Save();
    }
}
