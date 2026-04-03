using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Config;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");

        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique();

        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Role)
            .IsRequired()
            .HasConversion<int>();

        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();

        b.HasMany(x => x.FinancialRecords)
            .WithOne(x => x.CreatedBy)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
