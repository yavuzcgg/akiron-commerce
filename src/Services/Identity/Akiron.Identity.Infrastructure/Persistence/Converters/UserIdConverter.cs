using Akiron.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Identity.Infrastructure.Persistence.Converters;

/// <summary>Stores <see cref="UserId"/> as the plain uuid it wraps.</summary>
public sealed class UserIdConverter() : ValueConverter<UserId, Guid>(
    id => id.Value,
    value => new UserId(value));
