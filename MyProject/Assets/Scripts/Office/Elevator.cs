using System;
using System.Collections;
using UnityEngine;

// 버튼 입력 시 문 닫힘·대기·도착 알림·문 열림 순서의 엘리베이터 동작
public class Elevator : MonoBehaviour
{
    [SerializeField] private ElevatorButton _button;

    [Header("문")]
    // 열린 상태로 배치된 문짝, 부모 로컬 x축 기준 왼쪽은 +x, 오른쪽은 -x로 닫힘
    [SerializeField] private Transform _leftDoor;
    [SerializeField] private Transform _rightDoor;

    // 문짝 한 짝이 닫힐 때 이동하는 거리(m)
    [SerializeField] private float _slideDistance = 0.7f;

    // 문짝 이동 속도(초당 거리)
    [SerializeField] private float _doorSpeed = 1f;

    // 문이 닫힌 채 기다리는 시간(초)
    [SerializeField] private float _travelTime = 2f;

    private Vector3 _leftOpenPosition;
    private Vector3 _rightOpenPosition;
    private Vector3 _leftClosedPosition;
    private Vector3 _rightClosedPosition;
    private bool _isOperating;

    public event Action OnArrived;

    private void Awake()
    {
        _leftOpenPosition = _leftDoor.localPosition;
        _rightOpenPosition = _rightDoor.localPosition;
        _leftClosedPosition = _leftOpenPosition + Vector3.right * _slideDistance;
        _rightClosedPosition = _rightOpenPosition + Vector3.left * _slideDistance;
    }

    private void OnEnable() => _button.OnPressed += HandlePressed;

    private void OnDisable() => _button.OnPressed -= HandlePressed;

    private void HandlePressed()
    {
        if (_isOperating) return;

        StartCoroutine(Operate());
    }

    // 문 닫기, 대기, 도착 알림, 문 열기 순서 진행
    private IEnumerator Operate()
    {
        _isOperating = true;
        yield return MoveDoors(_leftClosedPosition, _rightClosedPosition);
        yield return new WaitForSeconds(_travelTime);
        OnArrived?.Invoke();
        yield return MoveDoors(_leftOpenPosition, _rightOpenPosition);
        _isOperating = false;
    }

    // 두 문짝을 목표 위치까지 일정 속도로 이동
    private IEnumerator MoveDoors(Vector3 leftTarget, Vector3 rightTarget)
    {
        while (_leftDoor.localPosition != leftTarget || _rightDoor.localPosition != rightTarget)
        {
            float step = _doorSpeed * Time.deltaTime;
            _leftDoor.localPosition = Vector3.MoveTowards(_leftDoor.localPosition, leftTarget, step);
            _rightDoor.localPosition = Vector3.MoveTowards(_rightDoor.localPosition, rightTarget, step);
            yield return null;
        }
    }
}
