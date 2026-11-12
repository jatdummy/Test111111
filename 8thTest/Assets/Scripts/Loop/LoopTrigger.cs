using UnityEngine;

// 루프 구간 통과 감지
[RequireComponent(typeof(BoxCollider))]
public class LoopTrigger : MonoBehaviour
{
    // 트리거 종류
    [SerializeField] private LoopTriggerType _type;

    // 통과를 알릴 루프 관리자
    [SerializeField] private LoopManager _loopManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out FirstPersonController player)) return;

        _loopManager.HandleTrigger(_type, player);
    }
}
