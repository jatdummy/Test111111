using NUnit.Framework;

// 22:00 출근 기준 시각 글자
public class LoopTimeTests
{
    [TestCase(0, "22:00")]
    [TestCase(1, "23:00")]
    [TestCase(2, "00:00")]
    [TestCase(8, "06:00")]
    public void FormatsHoursFromTenPm(int hoursPassed, string expected) => Assert.AreEqual(expected, LoopTime.Format(hoursPassed));
}
