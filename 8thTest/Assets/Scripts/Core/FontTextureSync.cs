using UnityEngine;

// 동적 폰트 텍스처가 다시 만들어지면 글자 재질의 텍스처도 갱신 (한글처럼 글자가 많이 추가되면 텍스처가 새로 만들어짐)
public class FontTextureSync : MonoBehaviour
{
    // 글자에 쓰는 동적 폰트
    [SerializeField] private Font _font;

    // 깊이 테스트 글자 재질
    [SerializeField] private Material _textMaterial;

    private void OnEnable() => Font.textureRebuilt += HandleTextureRebuilt;

    private void Start() => SyncTexture();

    private void OnDisable() => Font.textureRebuilt -= HandleTextureRebuilt;

    private void HandleTextureRebuilt(Font font)
    {
        if (font != _font) return;

        SyncTexture();
    }

    private void SyncTexture() => _textMaterial.mainTexture = _font.material.mainTexture;
}
