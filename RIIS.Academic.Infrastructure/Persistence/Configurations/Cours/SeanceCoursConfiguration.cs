using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public sealed class SeanceCoursConfiguration : IEntityTypeConfiguration<SeanceCours>
{
    public void Configure(EntityTypeBuilder<SeanceCours> builder)
    {
        builder.ToTable("SeancesCours", table =>
            table.HasCheckConstraint(
                "CK_SeancesCours_ConfianceExtraction",
                "[ConfianceExtraction] IS NULL OR ([ConfianceExtraction] >= 0 AND [ConfianceExtraction] <= 1)"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nature).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Statut).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Module).HasMaxLength(200);
        builder.Property(x => x.NumeroChapitre).HasMaxLength(50);
        builder.Property(x => x.TitreLecon).HasMaxLength(300);
        builder.Property(x => x.Paragraphe).HasMaxLength(2000);
        builder.Property(x => x.PointsEssentiels).HasMaxLength(5000);
        builder.Property(x => x.PointsPrincipaux).HasMaxLength(5000);
        builder.Property(x => x.ExercicesApplications).HasMaxLength(3000);
        builder.Property(x => x.DevoirsMaison).HasMaxLength(3000);
        builder.Property(x => x.Evaluation).HasMaxLength(2000);
        builder.Property(x => x.CompetencesObjectifs).HasMaxLength(3000);
        builder.Property(x => x.Observation).HasMaxLength(2000);
        builder.Property(x => x.ConfianceExtraction).HasPrecision(5, 4);
        builder.HasIndex(x => new { x.CahierTexteId, x.DateCours });
        builder.HasIndex(x => new { x.MaquetteElementConstitutifId, x.DateCours });

        builder.HasOne(x => x.CahierTexte)
            .WithMany(x => x.Seances)
            .HasForeignKey(x => x.CahierTexteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.MaquetteElementConstitutif)
            .WithMany(x => x.SeancesCours)
            .HasForeignKey(x => x.MaquetteElementConstitutifId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CahierTextePage)
            .WithMany(x => x.Seances)
            .HasForeignKey(x => x.CahierTextePageId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
