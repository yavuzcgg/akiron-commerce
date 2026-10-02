using Akiron.Identity.Application.Common;
using Akiron.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Akiron.Identity.Application.Users.Register;

/// <summary>
/// Creates an account. Registering does not log the user in: the token comes from
/// /login, so there is exactly one place tokens are issued.
/// </summary>
public sealed class RegisterHandler(IIdentityDbContext dbContext, IPasswordHasher passwordHasher)
{
    public async Task<UserResponse> HandleAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        // A courtesy for the ordinary case. Two registrations racing past this both see
        // "free"; the unique index on users.email decides, and the loser gets the same 409.
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw IdentityErrors.EmailTaken();
        }

        var user = User.Register(email, request.DisplayName, passwordHasher.Hash(request.Password!));

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return UserResponse.From(user);
    }
}
