// 플레이어가 E로 상호작용하는 대상
public interface IInteractable
{
    // 화면에 띄울 안내 문구
    string PromptText { get; }

    // 지금 상호작용할 수 있는지 여부
    bool CanInteract { get; }

    // 상호작용 실행
    void Interact();
}
