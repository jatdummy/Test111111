using System;
using UnityEngine;

// 빌드 카운트·판정·다음 빌드 진행 관리
public class LoopManager : MonoBehaviour
{
    private const int ClearCount = 8;

    [Header("데이터")]
    [SerializeField] private BaseOfficeData _baseOfficeData;
    [SerializeField] private OfficeBuilder _officeBuilder;

    [Header("플레이어")]
    [SerializeField] private FirstPersonController _player;

    // 게임 시작 시 플레이어 배치 위치
    [SerializeField] private Transform _startPoint;

    [Header("표시")]
    [SerializeField] private BuildMonitorView _buildMonitorView;
    [SerializeField] private ProgressIndicatorView _progressIndicatorView;

    [Header("출구")]
    [SerializeField] private ElevatorButton _elevatorButton;
    [SerializeField] private Elevator _elevator;
    [SerializeField] private ReleaseDoorTrigger _releaseDoorTrigger;

    // 매 빌드 시작 시 잠금 해제 후 닫는 Merge 문
    [SerializeField] private ProximityDoor _mergeDoor;

    private int _count;
    private LoopSpec _currentSpec;
    private bool _isCleared;

    private void OnEnable()
    {
        _elevator.OnArrived += HandleArrived;
        _releaseDoorTrigger.OnEntered += HandleEntered;
    }

    private void Start()
    {
        _player.Teleport(_startPoint.position, _startPoint.rotation);
        StartLoop();
    }

    private void OnDisable()
    {
        _elevator.OnArrived -= HandleArrived;
        _releaseDoorTrigger.OnEntered -= HandleEntered;
    }

    private void HandleArrived() => Submit(ExitChoice.Elevator);

    private void HandleEntered()
    {
        // Merge 문을 지나 잠긴 루프에서만 릴리즈 제출 인정, 홀 안 반복 진입 차단
        if (!_mergeDoor.IsLocked) return;

        Submit(ExitChoice.Release);
    }

    // 현재 카운트 기준 빌드 생성과 Merge 문 초기화
    private void StartLoop()
    {
        _currentSpec = new LoopSpec(_count, Array.Empty<string>(), false);
        _officeBuilder.Build(_baseOfficeData);
        _buildMonitorView.Show(CodeTextGenerator.Generate(_currentSpec, _baseOfficeData.OfficeLength));
        _progressIndicatorView.Show(_count, ClearCount);
        // 0단계는 이상현상이 없으므로 엘리베이터 제출 차단
        _elevatorButton.SetInteractable(_count > 0);
        _mergeDoor.ResetDoor();
    }

    // 선택 판정 후 카운트 갱신과 다음 빌드 시작
    private void Submit(ExitChoice choice)
    {
        if (_isCleared) return;

        bool isCorrect = (choice == ExitChoice.Elevator) == _currentSpec.HasAnomaly;
        _count = isCorrect ? _count + 1 : 0;

        if (_count >= ClearCount)
        {
            Clear();
            return;
        }

        StartLoop();
    }

    // 클리어 표시 후 이후 제출 차단
    private void Clear()
    {
        _isCleared = true;
        _progressIndicatorView.Show(_count, ClearCount);
        _elevatorButton.SetInteractable(false);
        Debug.Log("8번 코딩 클리어");
    }
}
