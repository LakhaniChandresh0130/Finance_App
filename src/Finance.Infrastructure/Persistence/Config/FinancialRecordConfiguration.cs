using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Config;

internal sealed class FinancialRecordConfiguration : IEntityTypeConfiguration<FinancialRecord>
{
    public void Configure(EntityTypeBuilder<FinancialRecord> b)
    {
        b.ToTable("financial_records");

        b.HasKey(x => x.Id);

        b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        b.Property(x => x.Type).IsRequired().HasConversion<int>();
        b.Property(x => x.Category).IsRequired().HasMaxLength(100);
        b.Property(x => x.RecordDate).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.Version)
            .IsRequired()
            .IsConcurrencyToken();

        b.HasIndex(x => new { x.IsDeleted, x.RecordDate })
            .HasDatabaseName("ix_financial_records_deleted_record_date");

        b.HasIndex(x => x.Category).HasDatabaseName("ix_financial_records_category");
        b.HasIndex(x => x.Type).HasDatabaseName("ix_financial_records_type");
    }
}
