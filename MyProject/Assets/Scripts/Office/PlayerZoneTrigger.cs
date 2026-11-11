using System;
using UnityEngine;

// 플레이어가 들어오면 알리는 트리거 영역
[RequireComponent(typeof(Collider))]
public class PlayerZoneTrigger : MonoBehaviour
{
    public event Action OnPlayerEntered;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out FirstPersonController _)) return;

        OnPlayerEntered?.Invoke();
    }
}
