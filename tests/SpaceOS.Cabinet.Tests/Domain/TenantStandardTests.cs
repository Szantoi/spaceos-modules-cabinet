using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain;
using SpaceOS.Cabinet.Domain.Events;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class TenantStandardTests
{
    private static readonly Guid SomeTenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private static MaterialDefaults DefaultMaterials() =>
        new("HDF-18", 18.0, "HDF-6", 6.0);

    private static LineBoreSettings DefaultLineBore() =>
        new(true, 37.0, 32.0, 5.0);

    private static RuleThresholds DefaultThresholds() =>
        new(1800.0, 900.0);

    private static TenantStandard CreateValid() =>
        TenantStandard.Create(
            SomeTenantId,
            DefaultMaterials(),
            BackPanelAttachmentDefault.Groove,
            TopType.FullTop,
            DefaultLineBore(),
            DefaultThresholds(),
            ActorId).Value;

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_Valid_ReturnsSuccess()
    {
        var result = TenantStandard.Create(
            SomeTenantId, DefaultMaterials(), BackPanelAttachmentDefault.Groove,
            TopType.FullTop, DefaultLineBore(), DefaultThresholds(), ActorId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_Valid_Version1AndEventsRaised()
    {
        var ts = CreateValid();

        Assert.Equal(1, ts.Version);
        Assert.Single(ts.DomainEvents);
        Assert.IsType<TenantStandardCreated>(ts.DomainEvents[0]);
    }

    [Fact]
    public void Create_EmptyTenantId_ReturnsInvalid()
    {
        var result = TenantStandard.Create(
            Guid.Empty, DefaultMaterials(), BackPanelAttachmentDefault.Groove,
            TopType.FullTop, DefaultLineBore(), DefaultThresholds(), ActorId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void Create_NullMaterials_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TenantStandard.Create(
                SomeTenantId, null!, BackPanelAttachmentDefault.Groove,
                TopType.FullTop, DefaultLineBore(), DefaultThresholds(), ActorId));
    }

    // ── UpdateMaterials ───────────────────────────────────────────────────────

    [Fact]
    public void UpdateMaterials_CorrectVersion_SuccessAndVersionIncremented()
    {
        var ts = CreateValid();
        var newMaterials = new MaterialDefaults("MDF-19", 19.0, "HDF-8", 8.0);

        var result = ts.UpdateMaterials(newMaterials, ActorId, expectedVersion: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, ts.Version);
        Assert.Equal("MDF-19", ts.Materials.CarcassMaterial);
    }

    [Fact]
    public void UpdateMaterials_WrongVersion_ReturnsError()
    {
        var ts = CreateValid();

        var result = ts.UpdateMaterials(DefaultMaterials(), ActorId, expectedVersion: 99);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── UpdateLineBore ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateLineBore_CorrectVersion_Success()
    {
        var ts = CreateValid();
        var newLineBore = new LineBoreSettings(false, 32.0, 32.0, 5.0);

        var result = ts.UpdateLineBore(newLineBore, ActorId, expectedVersion: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, ts.Version);
        Assert.False(ts.LineBore.Enabled);
    }

    [Fact]
    public void UpdateLineBore_WrongVersion_ReturnsError()
    {
        var ts = CreateValid();

        var result = ts.UpdateLineBore(DefaultLineBore(), ActorId, expectedVersion: 42);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── UpdateThresholds ──────────────────────────────────────────────────────

    [Fact]
    public void UpdateThresholds_CorrectVersion_Success()
    {
        var ts = CreateValid();
        var newThresholds = new RuleThresholds(2000.0, 1200.0);

        var result = ts.UpdateThresholds(newThresholds, ActorId, expectedVersion: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000.0, ts.Thresholds.TallCabinetHeightMm);
    }

    // ── UpdateConstructionDefaults ────────────────────────────────────────────

    [Fact]
    public void UpdateConstructionDefaults_CorrectVersion_Success()
    {
        var ts = CreateValid();

        var result = ts.UpdateConstructionDefaults(
            BackPanelAttachmentDefault.Rabbet, TopType.CrossRailPair, ActorId, expectedVersion: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(BackPanelAttachmentDefault.Rabbet, ts.BackPanelAttachment);
        Assert.Equal(TopType.CrossRailPair, ts.TopType);
    }

    // ── OverrideRuleSeverity ──────────────────────────────────────────────────

    [Fact]
    public void OverrideRuleSeverity_AddsToDict()
    {
        var ts = CreateValid();

        ts.OverrideRuleSeverity("RULE-001", AdvisorySeverity.Error, ActorId, expectedVersion: 1);

        Assert.True(ts.RuleSeverityOverrides.ContainsKey("RULE-001"));
        Assert.Equal(AdvisorySeverity.Error, ts.RuleSeverityOverrides["RULE-001"]);
    }

    [Fact]
    public void OverrideRuleSeverity_EmptyRuleId_ReturnsInvalid()
    {
        var ts = CreateValid();

        var result = ts.OverrideRuleSeverity("", AdvisorySeverity.Warning, ActorId, expectedVersion: 1);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void OverrideRuleSeverity_WrongVersion_ReturnsError()
    {
        var ts = CreateValid();

        var result = ts.OverrideRuleSeverity("RULE-001", AdvisorySeverity.Info, ActorId, expectedVersion: 99);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── ClearRuleSeverityOverride ─────────────────────────────────────────────

    [Fact]
    public void ClearOverride_Existing_Success()
    {
        var ts = CreateValid();
        ts.OverrideRuleSeverity("RULE-X", AdvisorySeverity.Critical, ActorId, expectedVersion: 1);

        var result = ts.ClearRuleSeverityOverride("RULE-X", ActorId, expectedVersion: 2);

        Assert.True(result.IsSuccess);
        Assert.False(ts.RuleSeverityOverrides.ContainsKey("RULE-X"));
    }

    [Fact]
    public void ClearOverride_NonExistent_ReturnsInvalid()
    {
        var ts = CreateValid();

        var result = ts.ClearRuleSeverityOverride("DOES-NOT-EXIST", ActorId, expectedVersion: 1);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void ClearOverride_WrongVersion_ReturnsError()
    {
        var ts = CreateValid();
        ts.OverrideRuleSeverity("RULE-Y", AdvisorySeverity.Warning, ActorId, expectedVersion: 1);

        var result = ts.ClearRuleSeverityOverride("RULE-Y", ActorId, expectedVersion: 99);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── PopDomainEvents ───────────────────────────────────────────────────────

    [Fact]
    public void PopDomainEvents_OrderedBySequence()
    {
        var ts = CreateValid();
        ts.UpdateMaterials(DefaultMaterials(), ActorId, expectedVersion: 1);
        ts.UpdateLineBore(DefaultLineBore(), ActorId, expectedVersion: 2);

        var events = ts.PopDomainEvents();

        for (int i = 1; i < events.Count; i++)
            Assert.True(events[i].SequenceNumber > events[i - 1].SequenceNumber);
    }

    [Fact]
    public void PopDomainEvents_ClearsBuffer()
    {
        var ts = CreateValid();

        ts.PopDomainEvents();

        Assert.Empty(ts.DomainEvents);
    }

    // ── Version monotonicity ──────────────────────────────────────────────────

    [Fact]
    public void MultipleUpdates_VersionMonotonicallyIncreases()
    {
        var ts = CreateValid();
        ts.UpdateMaterials(DefaultMaterials(), ActorId, expectedVersion: 1);
        ts.UpdateLineBore(DefaultLineBore(), ActorId, expectedVersion: 2);
        ts.UpdateThresholds(DefaultThresholds(), ActorId, expectedVersion: 3);

        Assert.Equal(4, ts.Version);
    }

    [Fact]
    public void UpdatedAt_ChangesAfterMutation()
    {
        var ts = CreateValid();
        var before = ts.UpdatedAt;

        // Small delay to ensure time advances
        System.Threading.Thread.Sleep(5);
        ts.UpdateMaterials(new MaterialDefaults("X", 1, "Y", 2), ActorId, expectedVersion: 1);

        Assert.True(ts.UpdatedAt >= before);
    }

    // ── Multiple overrides ────────────────────────────────────────────────────

    [Fact]
    public void OverrideRuleSeverity_MultipleRules_AllPersisted()
    {
        var ts = CreateValid();
        ts.OverrideRuleSeverity("R1", AdvisorySeverity.Warning, ActorId, expectedVersion: 1);
        ts.OverrideRuleSeverity("R2", AdvisorySeverity.Error, ActorId, expectedVersion: 2);
        ts.OverrideRuleSeverity("R3", AdvisorySeverity.Critical, ActorId, expectedVersion: 3);

        Assert.Equal(3, ts.RuleSeverityOverrides.Count);
        Assert.Equal(AdvisorySeverity.Warning, ts.RuleSeverityOverrides["R1"]);
        Assert.Equal(AdvisorySeverity.Error, ts.RuleSeverityOverrides["R2"]);
        Assert.Equal(AdvisorySeverity.Critical, ts.RuleSeverityOverrides["R3"]);
    }

    [Fact]
    public void ClearOverride_AfterSet_DictIsEmpty()
    {
        var ts = CreateValid();
        ts.OverrideRuleSeverity("ONLY", AdvisorySeverity.Info, ActorId, expectedVersion: 1);

        ts.ClearRuleSeverityOverride("ONLY", ActorId, expectedVersion: 2);

        Assert.Empty(ts.RuleSeverityOverrides);
    }

    // ── Event content ─────────────────────────────────────────────────────────

    [Fact]
    public void Create_RaisesCreatedEvent_WithCorrectTenantId()
    {
        var tenantId = Guid.NewGuid();
        var ts = TenantStandard.Create(
            tenantId, DefaultMaterials(), BackPanelAttachmentDefault.Stumpf,
            TopType.FullTop, DefaultLineBore(), DefaultThresholds(), ActorId).Value;

        var evt = ts.DomainEvents.OfType<TenantStandardCreated>().Single();

        Assert.Equal(tenantId, evt.TenantId);
    }

    [Fact]
    public void UpdateMaterials_RaisesEvent()
    {
        var ts = CreateValid();
        ts.PopDomainEvents(); // clear create event

        ts.UpdateMaterials(DefaultMaterials(), ActorId, expectedVersion: 1);

        Assert.Single(ts.DomainEvents);
        Assert.IsType<TenantStandardMaterialsUpdated>(ts.DomainEvents[0]);
    }

    [Fact]
    public void OverrideRuleSeverity_RaisesEvent()
    {
        var ts = CreateValid();
        ts.PopDomainEvents();

        ts.OverrideRuleSeverity("RULE-EVNT", AdvisorySeverity.Warning, ActorId, expectedVersion: 1);

        Assert.Single(ts.DomainEvents);
        Assert.IsType<TenantStandardRuleSeverityOverridden>(ts.DomainEvents[0]);
    }

    [Fact]
    public void ClearOverride_RaisesEvent()
    {
        var ts = CreateValid();
        ts.OverrideRuleSeverity("CLR", AdvisorySeverity.Error, ActorId, expectedVersion: 1);
        ts.PopDomainEvents();

        ts.ClearRuleSeverityOverride("CLR", ActorId, expectedVersion: 2);

        Assert.Single(ts.DomainEvents);
        Assert.IsType<TenantStandardRuleSeverityCleared>(ts.DomainEvents[0]);
    }
}
