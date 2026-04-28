using Ardalis.Result;
using SpaceOS.Cabinet.Catalog;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Catalog;

public class RatingAndFlagTests
{
    private static readonly Guid EntryId = Guid.NewGuid();
    private static readonly Guid OwnerTenantId = Guid.NewGuid();
    private static readonly Guid RaterTenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    // ── CatalogEntryRating.Create ─────────────────────────────────────────────

    [Fact]
    public void Rating_Create_Valid_ReturnsSuccess()
    {
        var result = CatalogEntryRating.Create(
            EntryId, RaterTenantId, UserId, 4, "Good product", OwnerTenantId);

        Assert.True(result.IsSuccess);
        Assert.Equal(EntryId, result.Value.CatalogEntryId);
        Assert.Equal(4, result.Value.Stars);
    }

    [Fact]
    public void Rating_Create_SelfRating_ReturnsInvalid()
    {
        var result = CatalogEntryRating.Create(
            EntryId, OwnerTenantId, UserId, 5, null, OwnerTenantId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void Rating_Create_Stars0_ReturnsInvalid()
    {
        var result = CatalogEntryRating.Create(
            EntryId, RaterTenantId, UserId, 0, null, OwnerTenantId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void Rating_Create_Stars6_ReturnsInvalid()
    {
        var result = CatalogEntryRating.Create(
            EntryId, RaterTenantId, UserId, 6, null, OwnerTenantId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void Rating_Create_CommentOver500_ReturnsInvalid()
    {
        var longComment = new string('a', 501);
        var result = CatalogEntryRating.Create(
            EntryId, RaterTenantId, UserId, 3, longComment, OwnerTenantId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void Rating_Create_NullComment_Allowed()
    {
        var result = CatalogEntryRating.Create(
            EntryId, RaterTenantId, UserId, 3, null, OwnerTenantId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Comment);
    }

    [Fact]
    public void Rating_UpdateStars_ChangesValue()
    {
        var rating = CatalogEntryRating.Create(
            EntryId, RaterTenantId, UserId, 3, "ok", OwnerTenantId).Value;

        rating.UpdateStars(5, "great");

        Assert.Equal(5, rating.Stars);
        Assert.Equal("great", rating.Comment);
    }

    // ── CatalogEntryFlag.Create ───────────────────────────────────────────────

    [Fact]
    public void Flag_Create_Valid_ReturnsSuccess()
    {
        var result = CatalogEntryFlag.Create(
            EntryId, RaterTenantId, UserId, FlagReason.Spam, null, OwnerTenantId);

        Assert.True(result.IsSuccess);
        Assert.Equal(FlagState.Active, result.Value.State);
    }

    [Fact]
    public void Flag_Create_SelfFlag_ReturnsInvalid()
    {
        var result = CatalogEntryFlag.Create(
            EntryId, OwnerTenantId, UserId, FlagReason.Spam, null, OwnerTenantId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void Flag_Create_StripsPiiEmail()
    {
        var result = CatalogEntryFlag.Create(
            EntryId, RaterTenantId, UserId, FlagReason.Other,
            "contact user@example.com for details", OwnerTenantId);

        Assert.True(result.IsSuccess);
        Assert.Contains("[email]", result.Value.Note);
        Assert.DoesNotContain("user@example.com", result.Value.Note);
    }

    [Fact]
    public void Flag_Create_StripsPiiPhone()
    {
        var result = CatalogEntryFlag.Create(
            EntryId, RaterTenantId, UserId, FlagReason.Other,
            "call +36 20 123 4567 now", OwnerTenantId);

        Assert.True(result.IsSuccess);
        Assert.Contains("[phone]", result.Value.Note);
        Assert.DoesNotContain("+36 20 123 4567", result.Value.Note);
    }

    [Fact]
    public void Flag_Create_NullNote_Allowed()
    {
        var result = CatalogEntryFlag.Create(
            EntryId, RaterTenantId, UserId, FlagReason.BrokenContent, null, OwnerTenantId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Note);
    }

    [Fact]
    public void Flag_Create_TruncatesLongNote()
    {
        var longNote = new string('x', 1200);

        var result = CatalogEntryFlag.Create(
            EntryId, RaterTenantId, UserId, FlagReason.Other, longNote, OwnerTenantId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.Note);
        Assert.True(result.Value.Note!.Length <= 1000);
    }

    [Fact]
    public void Flag_Resolve_SetsStateAndTimestamp()
    {
        var flag = CatalogEntryFlag.Create(
            EntryId, RaterTenantId, UserId, FlagReason.Spam, null, OwnerTenantId).Value;

        flag.Resolve(FlagState.AdminCleared, UserId);

        Assert.Equal(FlagState.AdminCleared, flag.State);
        Assert.NotNull(flag.ResolvedAt);
        Assert.Equal(UserId, flag.ResolvedByUserId);
    }

    [Fact]
    public void Flag_DefaultState_IsActive()
    {
        var flag = CatalogEntryFlag.Create(
            EntryId, RaterTenantId, UserId, FlagReason.Plagiarism, null, OwnerTenantId).Value;

        Assert.Equal(FlagState.Active, flag.State);
    }
}
