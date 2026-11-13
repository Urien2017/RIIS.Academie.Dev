using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public sealed class CahierTextePageConfiguration : IEntityTypeConfiguration<CahierTextePage>
{
    public void Configure(EntityTypeBuilder<CahierTextePage> builder)
    {
        builder.ToTable("CahiersTextesPages", table =>
            table.HasCheckConstraint(
                "CK_CahiersTextesPages_ConfianceOcr",
                "[ConfianceOcr] IS NULL OR ([ConfianceOcr] >= 0 AND [ConfianceOcr] <= 1)"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.FichierOriginalUri).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.EmpreinteSha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TexteOcr).HasMaxLength(10000);
        builder.Property(x => x.ConfianceOcr).HasPrecision(5, 4);
        builder.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.CahierTexteId, x.NumeroPage }).IsUnique();
        builder.HasIndex(x => x.EmpreinteSha256);

        builder.HasOne(x => x.CahierTexte)
            .WithMany(x => x.Pages)
            .HasForeignKey(x => x.CahierTexteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
