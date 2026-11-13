using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public sealed class DossierEtudiantConfiguration : IEntityTypeConfiguration<DossierEtudiant>
{
    public void Configure(EntityTypeBuilder<DossierEtudiant> builder)
    {
        builder.ToTable("DossiersEtudiants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Matricule).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => x.Matricule).IsUnique();
        builder.HasIndex(x => new { x.EtudiantId, x.ParcoursAcademiqueId }).IsUnique();

        builder.HasOne(x => x.Etudiant)
            .WithMany(x => x.DossiersEtudiants)
            .HasForeignKey(x => x.EtudiantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ParcoursAcademique)
            .WithMany(x => x.DossiersEtudiants)
            .HasForeignKey(x => x.ParcoursAcademiqueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DossierAdministratif)
            .WithOne(x => x.DossierEtudiant)
            .HasForeignKey<DossierAdministratif>(x => x.DossierEtudiantId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.DossierAcademique)
            .WithOne(x => x.DossierEtudiant)
            .HasForeignKey<DossierAcademique>(x => x.DossierEtudiantId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.DossierFinancier)
            .WithOne(x => x.DossierEtudiant)
            .HasForeignKey<DossierFinancier>(x => x.DossierEtudiantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
