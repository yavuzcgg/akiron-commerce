using Akiron.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Identity.Infrastructure.Persistence.Converters;

/// <summary>Stores <see cref="Email"/> as text, re-validating on the way out like every value object here.</summary>
public sealed class EmailConverter() : ValueConverter<Email, string>(
    email => email.Value,
    value => Email.Create(value));
