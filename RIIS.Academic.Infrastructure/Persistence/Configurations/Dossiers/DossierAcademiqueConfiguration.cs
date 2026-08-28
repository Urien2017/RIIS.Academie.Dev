using RIIS.Academic.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public class DossierAcademiqueConfiguration : IEntityTypeConfiguration<DossierAcademique>
{
    public void Configure(EntityTypeBuilder<DossierAcademique> builder)
    {
        builder.ToTable("DossiersAcademiques");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Statut).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Version).IsRowVersion();
        builder.HasIndex(x => new { x.EtudiantId, x.ParcoursAcademiqueId }).IsUnique();
        builder.HasIndex(x => x.ClassePedagogiqueId);

        builder.HasOne(x => x.Etudiant)
            .WithMany(x => x.DossiersAcademiques)
            .HasForeignKey(x => x.EtudiantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ParcoursAcademique)
            .WithMany(x => x.DossiersAcademiques)
            .HasForeignKey(x => x.ParcoursAcademiqueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ClassePedagogique)
            .WithMany()
            .HasForeignKey(x => x.ClassePedagogiqueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
