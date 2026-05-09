using System.IO;
using AltTester.AltTesterSDK.Driver;
using NUnit.Framework;

namespace OthelloE2E;

// End-to-end tests against an instrumented Unity build (Editor Play mode or
// a built player) running the AltTester server.
//
// Required before running:
//   1. AltTester SDK package installed (Packages/manifest.json — already added).
//   2. AltTester > AltTester Editor menu opened in Unity, "AltTester" toggled
//      ON for the current build target. This injects the server into builds.
//   3. AltTester Server desktop app running on localhost:13000 (default port).
//   4. Either: hit Play in Editor, or run the instrumented Player build.
//
// Run from this folder:  dotnet test
[TestFixture]
public class OthelloE2ETests
{
    AltDriver _driver = null!;
    string _artifactsDir = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _artifactsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "artifacts");
        Directory.CreateDirectory(_artifactsDir);
    }

    [SetUp]
    public void SetUp()
    {
        _driver = new AltDriver(host: "127.0.0.1", port: 13000);
        _driver.LoadScene("Game");
    }

    [TearDown]
    public void TearDown()
    {
        // Capture a screenshot per test for AI / human review.
        var name = TestContext.CurrentContext.Test.MethodName ?? "test";
        var path = Path.Combine(_artifactsDir, $"{name}.png");
        _driver.GetPNGScreenshot(path);

        _driver.Stop();
    }

    [Test]
    public void TitleScreen_ShowsLanguageButton()
    {
        var langBtn = _driver.FindObject(By.NAME, "Btn_lang_mode");
        Assert.That(langBtn.enabled, Is.True);
    }

    [Test]
    public void TopBar_DoesNotContainLanguageButton()
    {
        // Regression: the in-game TopBar must NOT contain the language toggle.
        var hits = _driver.FindObjects(By.NAME, "Btn_lang_btn");
        Assert.That(hits, Is.Empty,
            "Btn_lang_btn must not exist anywhere in the scene — language toggle is title-only.");
    }

    [Test]
    public void VsAi_StartsAndShowsHomeButton()
    {
        _driver.FindObject(By.NAME, "Btn_vs_ai").Tap();

        var home = _driver.WaitForObject(By.NAME, "Btn_title_btn", timeout: 5);
        Assert.That(home.enabled, Is.True);

        // Mode select must be hidden once a game starts.
        Assert.That(_driver.FindObjects(By.NAME, "ModeSelectPanel"),
            Has.All.Property("enabled").False.Or.Property("activeInHierarchy").False);
    }

    [Test]
    public void VsAi_BoardPresentsValidMoveDots()
    {
        _driver.FindObject(By.NAME, "Btn_vs_ai").Tap();
        // After BeginTurn, the four legal opening moves for black should each
        // show a ValidDot child.
        var dots = _driver.FindObjects(By.NAME, "ValidDot");
        var visible = 0;
        foreach (var d in dots)
            if (d.enabled) visible++;
        Assert.That(visible, Is.EqualTo(4), "Opening position has 4 legal moves for black.");
    }
}
