using System.Collections.Generic;
using UnityEngine;

// 사무실을 원본으로 되돌리고 문제의 변화를 적용
public class OfficeController : MonoBehaviour
{
    private const int OriginalCupCount = 3;
    private const float SunkenDepth = 0.45f;
    private const float HeadTurnedYaw = 180f;
    private const float PosterFlipAngle = 180f;

    [Header("조명")]
    // 천장 조명 몸체(번호 순서)
    [SerializeField] private Renderer[] _lampRenderers;

    // 천장 조명 빛(번호 순서)
    [SerializeField] private Light[] _lampLights;

    // 켜진 조명 머티리얼
    [SerializeField] private Material _lampOnMaterial;

    // 꺼진 조명 머티리얼
    [SerializeField] private Material _lampOffMaterial;

    // 빨간 조명 머티리얼
    [SerializeField] private Material _lampRedMaterial;

    // 기본 조명 색
    [SerializeField] private Color _lampColor = new(1f, 0.95f, 0.88f);

    // 빨간 조명 색
    [SerializeField] private Color _lampRedColor = new(1f, 0.12f, 0.08f);

    [Header("책상")]
    // 의자(번호 순서)
    [SerializeField] private Transform[] _chairs;

    // 모니터 화면(번호 순서)
    [SerializeField] private Renderer[] _monitorScreens;

    // 켜진 화면 머티리얼
    [SerializeField] private Material _screenOnMaterial;

    // 꺼진 화면 머티리얼
    [SerializeField] private Material _screenOffMaterial;

    // 텍스처 누락 화면 머티리얼
    [SerializeField] private Material _missingTextureMaterial;

    [Header("소품")]
    // 탕비 테이블의 컵(앞에서부터 개수만큼 표시)
    [SerializeField] private GameObject[] _cups;

    // 화분
    [SerializeField] private GameObject _plant;

    // 벽 포스터
    [SerializeField] private Transform _poster;

    [Header("직원")]
    // 등을 보이고 일하는 시니어
    [SerializeField] private WorkerCharacter _senior;

    // 맞은편에서 걸어오는 동료
    [SerializeField] private WorkerCharacter _coworker;

    // 동료가 쳐다볼 대상(플레이어 카메라)
    [SerializeField] private Transform _stareTarget;

    private Vector3[] _chairPositions;
    private Quaternion _posterRotation;
    private bool[] _isLampOn;
    private bool _isLampRed;

    public WorkerCharacter Senior => _senior;
    public WorkerCharacter Coworker => _coworker;

    private void Awake() => CacheOriginalState();

    // 원본으로 복원한 뒤 변화 목록 적용(null이면 원본 그대로)
    public void Build(IReadOnlyList<OfficeEffect> effects)
    {
        ResetToOriginal();

        if (effects != null)
        {
            foreach (OfficeEffect effect in effects)
            {
                ApplyEffect(effect);
            }
        }

        RefreshLamps();
    }

    private void CacheOriginalState()
    {
        _chairPositions = new Vector3[_chairs.Length];
        for (int i = 0; i < _chairs.Length; i++)
        {
            _chairPositions[i] = _chairs[i].localPosition;
        }

        _posterRotation = _poster.localRotation;
        _isLampOn = new bool[_lampRenderers.Length];
    }

    private void ResetToOriginal()
    {
        for (int i = 0; i < _isLampOn.Length; i++)
        {
            _isLampOn[i] = true;
        }

        _isLampRed = false;

        for (int i = 0; i < _chairs.Length; i++)
        {
            _chairs[i].gameObject.SetActive(true);
            _chairs[i].localPosition = _chairPositions[i];
        }

        foreach (Renderer screen in _monitorScreens)
        {
            screen.sharedMaterial = _screenOnMaterial;
        }

        SetCupCount(OriginalCupCount);
        _plant.SetActive(true);
        _poster.localRotation = _posterRotation;
        _senior.ResetState();
        _coworker.ResetState();
    }

    private void ApplyEffect(OfficeEffect effect)
    {
        int value = effect.Value;

        switch (effect.Type)
        {
            case OfficeEffectType.LightsOff:
                for (int i = 0; i < value && i < _isLampOn.Length; i++)
                {
                    _isLampOn[i] = false;
                }
                break;
            case OfficeEffectType.LightsRed:
                _isLampRed = true;
                break;
            case OfficeEffectType.ChairMissing:
                if (IsValidIndex(value, _chairs.Length))
                    _chairs[value].gameObject.SetActive(false);
                break;
            case OfficeEffectType.ChairSunken:
                if (IsValidIndex(value, _chairs.Length))
                    _chairs[value].localPosition = _chairPositions[value] + Vector3.down * SunkenDepth;
                break;
            case OfficeEffectType.MonitorOff:
                if (IsValidIndex(value, _monitorScreens.Length))
                    _monitorScreens[value].sharedMaterial = _screenOffMaterial;
                break;
            case OfficeEffectType.MonitorTextureMissing:
                if (IsValidIndex(value, _monitorScreens.Length))
                    _monitorScreens[value].sharedMaterial = _missingTextureMaterial;
                break;
            case OfficeEffectType.SeniorTPose:
                _senior.SetTPose(true);
                break;
            case OfficeEffectType.SeniorHeadTurned:
                _senior.SetHeadYaw(HeadTurnedYaw);
                break;
            case OfficeEffectType.SeniorAbsent:
                _senior.gameObject.SetActive(false);
                break;
            case OfficeEffectType.CoworkerTPose:
                _coworker.SetTPose(true);
                break;
            case OfficeEffectType.CoworkerStaring:
                _coworker.SetStareTarget(_stareTarget);
                break;
            case OfficeEffectType.CoworkerClipping:
                _coworker.SetClipping(true);
                break;
            case OfficeEffectType.CupCount:
                SetCupCount(value);
                break;
            case OfficeEffectType.PlantMissing:
                _plant.SetActive(false);
                break;
            case OfficeEffectType.PosterFlipped:
                _poster.localRotation = _posterRotation * Quaternion.Euler(0f, 0f, PosterFlipAngle);
                break;
        }
    }

    // 조명 켜짐과 색을 몸체와 빛에 반영
    private void RefreshLamps()
    {
        Material litMaterial = _isLampRed ? _lampRedMaterial : _lampOnMaterial;
        Color litColor = _isLampRed ? _lampRedColor : _lampColor;

        for (int i = 0; i < _isLampOn.Length; i++)
        {
            _lampRenderers[i].sharedMaterial = _isLampOn[i] ? litMaterial : _lampOffMaterial;
            _lampLights[i].enabled = _isLampOn[i];
            _lampLights[i].color = litColor;
        }
    }

    private void SetCupCount(int count)
    {
        for (int i = 0; i < _cups.Length; i++)
        {
            _cups[i].SetActive(i < count);
        }
    }

    private static bool IsValidIndex(int index, int length) => index >= 0 && index < length;
}
