using Klasly.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Klasly.Tests;

/// <summary>
/// A phone's FCM token belongs to the last student who logged in on it (spec assumption 6), so a shared phone never gets
/// another student's grade. Uses seeded students Bryan (user 5) and Carla (user 6), which no other test class touches.
/// </summary>
public class FcmTokenTests(DatabaseFixture db)
{
    private const int BryanUserId = 5;
    private const int CarlaUserId = 6;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_token_is_saved_on_the_caller()
    {
        await SetAsync(BryanUserId, "bryan-phone-token");

        Assert.Equal("bryan-phone-token", await TokenOfAsync(BryanUserId));
    }

    [Fact]
    public async Task The_same_token_moves_from_the_previous_student_to_the_caller()
    {
        await SetAsync(BryanUserId, "shared-phone-token");

        await SetAsync(CarlaUserId, "shared-phone-token");

        Assert.Null(await TokenOfAsync(BryanUserId));
        Assert.Equal("shared-phone-token", await TokenOfAsync(CarlaUserId));
    }

    private async Task SetAsync(int userId, string token)
    {
        await using var context = db.CreateContext();
        await new UserRepository(context).SetFcmTokenAsync(userId, token, Ct);
    }

    private async Task<string?> TokenOfAsync(int userId)
    {
        await using var context = db.CreateContext();
        return await context.Users.Where(u => u.UserId == userId).Select(u => u.FcmToken).SingleAsync(Ct);
    }
}
