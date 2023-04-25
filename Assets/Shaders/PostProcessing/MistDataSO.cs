using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Mist")]
public class MistDataSO : ScriptableObject
{
    public MistData MistData => mistData;

    [SerializeField] private MistData mistData = new MistData(0.01f, 0.001f);
}

[Serializable]
public struct MistData
{
    [SerializeField] private float mistAmount;
    [SerializeField] private float mistPow;

    public float MistAmount => mistAmount;
    public float MistPow => mistPow;

    public MistData(float mistAmount, float mistPow)
    {
        this.mistAmount = mistAmount;
        this.mistPow = mistPow;
    }
}