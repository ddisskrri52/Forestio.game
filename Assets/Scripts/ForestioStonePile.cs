using UnityEngine;

[DisallowMultipleComponent]
public sealed class ForestioStonePile : MonoBehaviour
{
    public bool IsAvailable => isActiveAndEnabled;
}
