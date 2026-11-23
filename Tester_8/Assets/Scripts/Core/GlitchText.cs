using System.Text;
using UnityEngine;

// 고장 · 먹통 화면에 쓰는 깨진 글자 만들기
public static class GlitchText
{
    private const string GlitchChars = "█▓▒░#@%&?!/\\|<>";

    // 깨진 줄을 여러 개 붙임 (깨지지 않은 자리는 영문 소문자 또는 빈칸)
    public static void AppendLines(StringBuilder builder, int lineCount, int minLength, int maxLength, float glitchChance, bool isLetterFill)
    {
        for (int line = 0; line < lineCount; line++)
        {
            int length = Random.Range(minLength, maxLength);
            for (int i = 0; i < length; i++)
            {
                if (Random.value < glitchChance)
                {
                    builder.Append(GlitchChars[Random.Range(0, GlitchChars.Length)]);
                }
                else
                {
                    builder.Append(isLetterFill ? (char)Random.Range('a', 'z' + 1) : ' ');
                }
            }

            builder.AppendLine();
        }
    }
}
