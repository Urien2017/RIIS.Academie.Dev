using RIIS.Academic.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public class DocumentPersonnelConfiguration : IEntityTypeConfiguration<DocumentPersonnel>
{
    public void Configure(EntityTypeBuilder<DocumentPersonnel> builder)
    {
        builder.ToTable("DocumentsPersonnels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NomDocument).HasMaxLength(150).IsRequired();
        builder.Property(x => x.UrlFichier).HasMaxLength(500).IsRequired();
        builder.Property(x => x.VerifiePar).HasMaxLength(150);
        builder.Property(x => x.Observation).HasMaxLength(1000);
        builder.HasIndex(x => new { x.EtudiantId, x.NomDocument });

        builder.HasOne(x => x.Etudiant)
            .WithMany(x => x.DocumentsPersonnels)
            .HasForeignKey(x => x.EtudiantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
