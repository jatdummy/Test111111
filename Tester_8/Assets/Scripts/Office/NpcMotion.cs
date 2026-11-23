using UnityEngine;

// 코드로 NPC를 옮길 때 쓰는 이동 · 회전 (ColleagueWalker, EndingDirector 공용)
public static class NpcMotion
{
    // 이보다 가까운 방향은 무시 (제자리에서 방향이 튀지 않게)
    private const float MinFaceDistance = 0.01f;

    // 목표 지점 쪽으로 한 프레임만큼 걸음, 도착했으면 true
    public static bool StepTowards(Transform body, Vector3 target, float speed)
    {
        body.position = Vector3.MoveTowards(body.position, target, speed * Time.deltaTime);
        return body.position == target;
    }

    // 몸을 주어진 방향으로 조금씩 돌림 (꺾이는 지점에서 한 번에 휙 돌지 않게)
    public static void TurnTowards(Transform body, Vector3 direction, float degreesPerSecond)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < MinFaceDistance) return;

        body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), degreesPerSecond * Time.deltaTime);
    }
}
