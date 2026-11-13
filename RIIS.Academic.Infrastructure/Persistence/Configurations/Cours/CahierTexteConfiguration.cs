using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public sealed class CahierTexteConfiguration : IEntityTypeConfiguration<CahierTexte>
{
    public void Configure(EntityTypeBuilder<CahierTexte> builder)
    {
        builder.ToTable("CahiersTextes", table =>
            table.HasCheckConstraint(
                "CK_CahiersTextes_Dates",
                "[DateCloture] IS NULL OR [DateOuverture] IS NULL OR [DateCloture] >= [DateOuverture]"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.EnseignantNom).HasMaxLength(200);
        builder.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new
        {
            x.AnneeAcademiqueId,
            x.ParcoursAcademiqueId,
            x.ClassePedagogiqueId
        }).IsUnique();
        builder.HasIndex(x => new { x.ParcoursAcademiqueId, x.Statut });

        builder.HasOne(x => x.AnneeAcademique)
            .WithMany()
            .HasForeignKey(x => x.AnneeAcademiqueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ParcoursAcademique)
            .WithMany()
            .HasForeignKey(x => x.ParcoursAcademiqueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ClassePedagogique)
            .WithMany()
            .HasForeignKey(x => x.ClassePedagogiqueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.EnseignantUtilisateur)
            .WithMany()
            .HasForeignKey(x => x.EnseignantUtilisateurId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
