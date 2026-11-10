using System;
using UnityEngine;

// 릴리즈 문 통과 시 릴리즈 제출 알림
public class ReleaseDoorTrigger : MonoBehaviour
{
    public event Action OnEntered;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            OnEntered?.Invoke();
    }
}
