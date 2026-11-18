using UnityEngine;

// 책장 여닫이문과 서랍 (이상현상, 코드 override, 핫픽스가 함께 씀)
public class Cabinet : MonoBehaviour
{
    // 문짝들의 경첩 축 (문짝이 자식, 양쪽으로 열리는 여닫이문)
    [SerializeField] private Transform[] _doorHinges;

    // 문짝마다 열리는 각도 (경첩이 양 끝에 있어 서로 부호가 반대)
    [SerializeField] private float[] _doorOpenAngles = { -100f, 100f };

    // 문과 함께 앞으로 빠지는 서랍
    [SerializeField] private Transform _drawer;

    // 서랍이 빠지는 방향과 거리 (월드 기준, 책장 앞쪽)
    [SerializeField] private Vector3 _drawerPull = new(-0.3f, 0f, 0f);

    private Quaternion[] _doorRotations;
    private Vector3 _drawerPosition;

    private void Awake()
    {
        _doorRotations = new Quaternion[_doorHinges.Length];
        for (int i = 0; i < _doorHinges.Length; i++)
        {
            _doorRotations[i] = _doorHinges[i].localRotation;
        }

        _drawerPosition = _drawer.position;
    }

    // 문짝을 양쪽으로 활짝 열고 서랍을 앞으로 빼냄
    public void Open()
    {
        for (int i = 0; i < _doorHinges.Length; i++)
        {
            _doorHinges[i].localRotation = _doorRotations[i] * Quaternion.Euler(0f, _doorOpenAngles[i], 0f);
        }

        _drawer.position = _drawerPosition + _drawerPull;
    }

    // 문을 닫고 서랍을 원래대로
    public void Close()
    {
        for (int i = 0; i < _doorHinges.Length; i++)
        {
            _doorHinges[i].localRotation = _doorRotations[i];
        }

        _drawer.position = _drawerPosition;
    }
}
