using UnityEngine;

// 시작 계단실의 막는 벽: 승인 계단과 롤백 계단이 이 계단실로 함께 도착한다는 걸 숨김
// 들어온 경로의 계단실과 똑같이 보이게, 반대쪽 경로로 이어지는 계단 입구를 벽으로 막음 (원래 길이 없던 것처럼)
public class StairwellRouteView : MonoBehaviour
{
    // 위층으로 가는 계단 입구를 막는 벽 (승인 경로로 도착했을 때 세움, 승인 계단실에는 늘 있음)
    [SerializeField] private GameObject _upShutter;

    // 아래층으로 가는 계단 입구를 막는 벽 (롤백 경로로 도착했을 때 세움, 롤백 계단실에는 늘 있음)
    [SerializeField] private GameObject _downShutter;

    // isFromApproval : 승인 계단으로 올라왔으면 true, 롤백 계단으로 내려왔으면 false
    public void ShowArrival(bool isFromApproval)
    {
        _upShutter.SetActive(isFromApproval);
        _downShutter.SetActive(!isFromApproval);
    }
}
