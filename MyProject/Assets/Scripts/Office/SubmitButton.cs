using System;
using System.Collections;
using UnityEngine;

// 뒤쪽 벽의 수정 요청 제출 버튼
public class SubmitButton : MonoBehaviour, IInteractable
{
    // 눌리는 버튼 머리
    [SerializeField] private Transform _buttonCap;

    // 눌렸을 때 들어가는 깊이
    [SerializeField] private float _pressDepth = 0.03f;

    // 눌린 채로 있는 시간
    [SerializeField] private float _pressDuration = 0.15f;

    private Vector3 _capRestPosition;
    private bool _isAvailable;

    public string PromptText => "E  수정 요청 제출";
    public bool CanInteract => _isAvailable;

    public event Action OnPressed;

    private void Awake() => _capRestPosition = _buttonCap.localPosition;

    // 제출 가능 여부 전환
    public void SetAvailable(bool isAvailable) => _isAvailable = isAvailable;

    public void Interact()
    {
        StartCoroutine(PlayPressRoutine());
        OnPressed?.Invoke();
    }

    // 버튼이 눌렸다가 돌아오는 연출
    private IEnumerator PlayPressRoutine()
    {
        _buttonCap.localPosition = _capRestPosition + Vector3.back * _pressDepth;
        yield return new WaitForSeconds(_pressDuration);
        _buttonCap.localPosition = _capRestPosition;
    }
}
