using RIIS.Academic.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public class DossierFinancierConfiguration : IEntityTypeConfiguration<DossierFinancier>
{
    public void Configure(EntityTypeBuilder<DossierFinancier> builder)
    {
        builder.ToTable("DossiersFinanciers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StatutAdministratif).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.StatutFinancier).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.StatutGlobal).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Observation).HasMaxLength(1000);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.EtudiantId, x.ParcoursAcademiqueId }).IsUnique();

        builder.HasOne(x => x.Etudiant)
            .WithMany(x => x.DossiersFinanciers)
            .HasForeignKey(x => x.EtudiantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ParcoursAcademique)
            .WithMany(x => x.DossiersFinanciers)
            .HasForeignKey(x => x.ParcoursAcademiqueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
