using MonoSlice.Modules.Exams.Domain;
using MonoSlice.Shared.Abstractions.Exceptions;
using Xunit;

namespace MonoSlice.Modules.Exams.Tests;

public class ExamRuleTests
{
    [Fact]
    public void Create_CustomRule_ShouldInitializeCorrectly()
    {
        var rule = ExamRule.Create(
            name: "Custom Quiz Policy",
            description: "Custom rule for university test",
            isSystemPreset: false,
            canTabSwitch: false,
            maxTabSwitchesAllowed: 2,
            restrictClipboardAndMouse: true,
            forceFullscreen: true,
            keyboardDetection: true,
            requireCamera: true,
            snapshotIntervalSeconds: 30,
            requireMicrophone: false,
            maxAllowedViolations: 4,
            autoDisqualifyOnExceed: true);

        Assert.NotEqual(Guid.Empty, rule.Id);
        Assert.Equal("Custom Quiz Policy", rule.Name);
        Assert.False(rule.IsSystemPreset);
        Assert.False(rule.CanTabSwitch);
        Assert.Equal(2, rule.MaxTabSwitchesAllowed);
        Assert.True(rule.RestrictClipboardAndMouse);
        Assert.True(rule.ForceFullscreen);
        Assert.True(rule.KeyboardDetection);
        Assert.True(rule.RequireCamera);
        Assert.Equal(30, rule.SnapshotIntervalSeconds);
        Assert.False(rule.RequireMicrophone);
        Assert.Equal(4, rule.MaxAllowedViolations);
        Assert.True(rule.AutoDisqualifyOnExceed);

        var config = rule.ToConfig();
        Assert.Equal("Custom Quiz Policy", config.Name);
        Assert.Equal(30, config.SnapshotIntervalSeconds);
        Assert.Equal(4, config.MaxAllowedViolations);
    }

    [Fact]
    public void Presets_ShouldHaveExpectedSecuritySettings()
    {
        var strict = ExamRuleConfig.StrictProctored();
        Assert.False(strict.CanTabSwitch);
        Assert.True(strict.ForceFullscreen);
        Assert.True(strict.RequireCamera);
        Assert.True(strict.RequireMicrophone);
        Assert.True(strict.AutoDisqualifyOnExceed);

        var openBook = ExamRuleConfig.OpenBook();
        Assert.True(openBook.CanTabSwitch);
        Assert.False(openBook.ForceFullscreen);
        Assert.True(openBook.RequireCamera);
        Assert.False(openBook.RequireMicrophone);
        Assert.False(openBook.AutoDisqualifyOnExceed);

        var practice = ExamRuleConfig.Practice();
        Assert.True(practice.CanTabSwitch);
        Assert.False(practice.ForceFullscreen);
        Assert.False(practice.RequireCamera);
        Assert.False(practice.RequireMicrophone);
        Assert.False(practice.AutoDisqualifyOnExceed);
    }

    [Fact]
    public void Create_InvalidRule_ShouldThrowValidationException()
    {
        Assert.Throws<ValidationException>(() =>
            ExamRule.Create(name: "   "));

        Assert.Throws<BusinessRuleException>(() =>
            ExamRule.Create(name: "Invalid Violations", maxAllowedViolations: 0));

        Assert.Throws<BusinessRuleException>(() =>
            ExamRule.Create(name: "Invalid Interval", snapshotIntervalSeconds: 5));
    }
}
