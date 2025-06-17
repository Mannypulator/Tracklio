using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracklio.Shared.Domain.Entities;

namespace Tracklio.Shared.Persistence.Configuration;

public class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OneTimePassword).IsRequired().HasMaxLength(7);
        builder.Property(x => x.Email).IsRequired();
    }
}