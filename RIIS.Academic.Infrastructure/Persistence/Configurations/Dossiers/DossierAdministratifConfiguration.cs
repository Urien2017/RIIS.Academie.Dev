using RIIS.Academic.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public class DossierAdministratifConfiguration : IEntityTypeConfiguration<DossierAdministratif>
{
    public void Configure(EntityTypeBuilder<DossierAdministratif> builder)
    {
        builder.ToTable("DossiersAdministratifs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Statut).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.EtudiantId, x.ParcoursAcademiqueId }).IsUnique();

        builder.HasOne(x => x.Etudiant)
            .WithMany(x => x.DossiersAdministratifs)
            .HasForeignKey(x => x.EtudiantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ParcoursAcademique)
            .WithMany(x => x.DossiersAdministratifs)
            .HasForeignKey(x => x.ParcoursAcademiqueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
