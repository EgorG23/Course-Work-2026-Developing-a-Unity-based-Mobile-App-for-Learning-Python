using NUnit.Framework;

public class TerminalAnswerCheckerTests
{
    [TestCase("python --version")]
    [TestCase("PYTHON -v")]
    public void PythonVersion_AcceptsSupportedCommands(string input)
    {
        Assert.That(TerminalAnswerChecker.IsCorrect(TerminalTask.PythonVersion, input), Is.True);
    }

    [Test]
    public void UserCredentials_AcceptsNormalizedQuotesAndLineEndings()
    {
        const string input = "user_name = «Alex»\r\nuser_password = 1234567890\r\nprint(user_name)\r\nprint(user_password)";
        Assert.That(TerminalAnswerChecker.IsCorrect(TerminalTask.UserCredentials, input), Is.True);
    }

    [Test]
    public void Arithmetic_RequiresAllFourOperations()
    {
        const string correct = "a=25\nb=5\nprint(a+b)\nprint(a-b)\nprint(a*b)\nprint(a/b)";
        const string incomplete = "a=25\nb=5\nprint(a+b)";

        Assert.That(TerminalAnswerChecker.IsCorrect(TerminalTask.Arithmetic, correct), Is.True);
        Assert.That(TerminalAnswerChecker.IsCorrect(TerminalTask.Arithmetic, incomplete), Is.False);
    }

    [Test]
    public void Formula_AcceptsExpectedExpression()
    {
        Assert.That(TerminalAnswerChecker.IsCorrect(TerminalTask.Formula, "x = 5\nf = x**2 + 2*x - 19\nprint(f)"), Is.True);
    }

    [TestCase("print(143 % 10)")]
    [TestCase("print(str(143)[-1])")]
    public void LastDigit_AcceptsSingleLineSolutions(string input)
    {
        Assert.That(TerminalAnswerChecker.IsCorrect(TerminalTask.LastDigit, input), Is.True);
    }

    [Test]
    public void LastDigit_RejectsMultipleLines()
    {
        Assert.That(TerminalAnswerChecker.IsCorrect(TerminalTask.LastDigit, "x = 143\nprint(x % 10)"), Is.False);
    }
}
