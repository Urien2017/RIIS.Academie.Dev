using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public sealed class AffectationMaquetteConfiguration : IEntityTypeConfiguration<AffectationMaquette>
{
    public void Configure(EntityTypeBuilder<AffectationMaquette> builder)
    {
        builder.ToTable("AffectationsMaquettes", table =>
            table.HasCheckConstraint(
                "CK_AffectationsMaquettes_Dates",
                "[DateFin] IS NULL OR [DateDebut] IS NULL OR [DateFin] >= [DateDebut]"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Observation).HasMaxLength(1000);
        builder.HasIndex(x => new { x.ParcoursAcademiqueId, x.MaquettePedagogiqueId }).IsUnique();
        builder.HasIndex(x => new { x.ParcoursAcademiqueId, x.EstActive, x.EstPrincipale });

        builder.HasOne(x => x.ParcoursAcademique)
            .WithMany(x => x.AffectationsMaquettes)
            .HasForeignKey(x => x.ParcoursAcademiqueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaquettePedagogique)
            .WithMany(x => x.AffectationsMaquettes)
            .HasForeignKey(x => x.MaquettePedagogiqueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
