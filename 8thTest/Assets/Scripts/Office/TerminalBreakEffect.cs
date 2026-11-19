using System.Collections;
using UnityEngine;

// QA 어시스턴트 단말이 고장 날 때의 연출 (떨림 · 유리 조각 · 꺾임 · 섬광 · 카메라 흔들림 · 연기 · 불꽃)
public class TerminalBreakEffect : MonoBehaviour
{
    // 고장 직전 깜빡임 (불꽃 확률과 개수, 모니터 떨림 각도)
    private const float GlitchSparkChance = 0.35f;
    private const int GlitchSparkMin = 2;
    private const int GlitchSparkMax = 6;
    private const float GlitchShakeAngle = 1.5f;

    // 터질 때 (섬광 시간, 불꽃 개수, 조각이 퍼지는 정도, 위로 튀는 정도, 힘 배율 범위, 회전 속도)
    private const float ExplodeFlashSeconds = 0.4f;
    private const int ExplodeSparkCount = 90;
    private const float ShardSpread = 0.8f;
    private const float ShardLift = 0.4f;
    private const float ShardForceMin = 0.6f;
    private const float ShardForceMax = 1.2f;
    private const float ShardSpin = 20f;

    // 모니터 꺾임 (최대로 꺾이는 시점 비율, 그때 꺾이는 배율)
    private const float KickPeakTime = 0.2f;
    private const float KickOvershoot = 1.4f;

    // 섬광이 지직거릴 때 가장 어두운 비율
    private const float FlashFlickerMin = 0.4f;

    // 부서진 뒤 합선 불꽃 (개수 범위, 섬광 세기 배율, 섬광 시간)
    private const int ShortSparkMin = 6;
    private const int ShortSparkMax = 16;
    private const float ShortFlashScale = 0.25f;
    private const float ShortFlashSeconds = 0.12f;

    // 고장 나면 보이는 화면 금
    [SerializeField] private GameObject _cracks;

    // 부서질 때 뒤로 꺾이는 모니터 (몸체 · 화면 · 글자 · 금이 자식, 기준점은 받침대 위)
    [SerializeField] private Transform _monitor;

    // 부서진 뒤 모니터가 꺾여 있는 각도
    [SerializeField] private Vector3 _brokenTilt = new(-16f, 11f, -7f);

    // 꺾일 때 튕기는 시간(초)
    [SerializeField, Min(0.01f)] private float _kickSeconds = 0.5f;

    // 화면에서 튀는 불꽃
    [SerializeField] private ParticleSystem _sparks;

    // 부서진 뒤 피어오르는 연기
    [SerializeField] private ParticleSystem _smoke;

    // 터질 때 번쩍이는 불빛
    [SerializeField] private Light _flash;

    // 터질 때 번쩍이는 세기
    [SerializeField, Min(0f)] private float _flashIntensity = 8f;

    // 화면에서 떨어져 나가는 유리 조각들 (평소엔 꺼져 있음)
    [SerializeField] private Rigidbody[] _shards;

    // 유리 조각이 튀어 나가는 세기
    [SerializeField, Min(0f)] private float _shardForce = 2.5f;

    // 터질 때 흔들 플레이어 카메라
    [SerializeField] private Transform _cameraPivot;

    // 카메라 흔들림 세기와 시간(초)
    [SerializeField, Min(0f)] private float _shakeAmount = 0.07f;
    [SerializeField, Min(0.01f)] private float _shakeSeconds = 0.5f;

    // 부서진 뒤 이따금 불꽃이 튀는 간격(초, 최소 ~ 최대)
    [SerializeField] private Vector2 _sparkInterval = new(1.2f, 3.5f);

    private Quaternion _monitorRotation;

    private void Awake() => _monitorRotation = _monitor.localRotation;

    // 고장 직전 떨림 (이따금 불꽃이 튀고 모니터가 흔들림)
    public void Tremble()
    {
        if (Random.value < GlitchSparkChance)
        {
            _sparks.Emit(Random.Range(GlitchSparkMin, GlitchSparkMax));
        }

        _monitor.localRotation = _monitorRotation * Quaternion.Euler(Random.insideUnitSphere * GlitchShakeAngle);
    }

    // 터짐: 유리 조각, 불꽃, 섬광, 모니터가 뒤로 꺾이고 화면이 흔들림
    public void Break()
    {
        _cracks.SetActive(true);

        // 꺾인 모니터, 계속 피어오르는 연기, 이따금 튀는 불꽃
        _monitor.localRotation = _monitorRotation * Quaternion.Euler(_brokenTilt);
        _smoke.Play();
        StartCoroutine(SparkNowRoutine());

        Explode();
        StartCoroutine(KickRoutine());
        StartCoroutine(FlashRoutine(_flashIntensity, ExplodeFlashSeconds));
        StartCoroutine(ShakeRoutine());
    }

    private void Explode()
    {
        _sparks.Emit(ExplodeSparkCount);
        Vector3 outward = -_monitor.forward;
        foreach (Rigidbody shard in _shards)
        {
            shard.gameObject.SetActive(true);
            shard.transform.SetParent(null, true);
            shard.isKinematic = false;

            // 화면 앞쪽으로 흩어지며 떨어짐
            Vector3 spread = Random.insideUnitSphere * ShardSpread + Vector3.up * ShardLift;
            shard.linearVelocity = (outward + spread) * _shardForce * Random.Range(ShardForceMin, ShardForceMax);
            shard.angularVelocity = Random.insideUnitSphere * ShardSpin;
        }
    }

    // 뒤로 확 꺾였다가 반동으로 조금 돌아와 멈춤
    private IEnumerator KickRoutine()
    {
        _monitor.localRotation = _monitorRotation;
        float elapsed = 0f;
        while (elapsed < _kickSeconds)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _kickSeconds;
            float amount = t < KickPeakTime ? Mathf.Lerp(0f, KickOvershoot, t / KickPeakTime) : Mathf.Lerp(KickOvershoot, 1f, Mathf.SmoothStep(0f, 1f, (t - KickPeakTime) / (1f - KickPeakTime)));
            _monitor.localRotation = _monitorRotation * Quaternion.Euler(_brokenTilt * amount);
            yield return null;
        }

        _monitor.localRotation = _monitorRotation * Quaternion.Euler(_brokenTilt);
    }

    // 불빛이 지직거리며 꺼짐
    private IEnumerator FlashRoutine(float intensity, float seconds)
    {
        _flash.enabled = true;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            _flash.intensity = intensity * (1f - elapsed / seconds) * Random.Range(FlashFlickerMin, 1f);
            yield return null;
        }

        _flash.enabled = false;
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 origin = _cameraPivot.localPosition;
        float elapsed = 0f;
        while (elapsed < _shakeSeconds)
        {
            elapsed += Time.deltaTime;
            float strength = _shakeAmount * (1f - elapsed / _shakeSeconds);
            _cameraPivot.localPosition = origin + Random.insideUnitSphere * strength;
            yield return null;
        }

        _cameraPivot.localPosition = origin;
    }

    // 부서진 뒤에도 가끔 합선된 듯 불꽃이 튐
    private IEnumerator SparkNowRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(_sparkInterval.x, _sparkInterval.y));
            _sparks.Emit(Random.Range(ShortSparkMin, ShortSparkMax));
            yield return FlashRoutine(_flashIntensity * ShortFlashScale, ShortFlashSeconds);
        }
    }
}
