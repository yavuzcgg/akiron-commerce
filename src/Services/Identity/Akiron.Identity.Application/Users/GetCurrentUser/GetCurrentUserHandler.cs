using Akiron.Identity.Application.Common;
using Akiron.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Identity.Application.Users.GetCurrentUser;

/// <summary>
/// Returns the account behind a token. The user id comes from the verified <c>sub</c>
/// claim, never from the request — there is nothing here a caller can point elsewhere.
/// </summary>
public sealed class GetCurrentUserHandler(IIdentityDbContext dbContext)
{
    public async Task<UserResponse> HandleAsync(UserId userId, CancellationToken cancellationToken)
    {
        // A token can outlive its account by up to its 15 minutes; that is a 404, not a crash.
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw IdentityErrors.UserNotFound(userId);

        return UserResponse.From(user);
    }
}
