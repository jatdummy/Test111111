using System.Text;
using NUnit.Framework;

// 깨진 글자 만들기 (줄 수, 깨질 확률에 따른 글자)
public class GlitchTextTests
{
    [Test]
    public void AppendsRequestedLineCount()
    {
        var builder = new StringBuilder();

        GlitchText.AppendLines(builder, 6, 8, 30, 0.5f, true);

        Assert.AreEqual(6, builder.ToString().Split('\n').Length - 1);
    }

    [Test]
    public void ZeroChanceFillsLettersOnly()
    {
        var builder = new StringBuilder();

        GlitchText.AppendLines(builder, 4, 8, 30, 0f, true);

        foreach (char c in builder.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty))
        {
            Assert.IsTrue(c >= 'a' && c <= 'z', $"'{c}'");
        }
    }

    [Test]
    public void FullChanceHasNoLettersOrSpaces()
    {
        var builder = new StringBuilder();

        GlitchText.AppendLines(builder, 4, 8, 30, 1.1f, false);

        foreach (char c in builder.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty))
        {
            Assert.IsFalse(c == ' ' || (c >= 'a' && c <= 'z'), $"'{c}'");
        }
    }
}
