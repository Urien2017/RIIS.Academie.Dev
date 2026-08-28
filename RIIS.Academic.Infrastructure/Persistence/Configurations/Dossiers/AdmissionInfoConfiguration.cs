using RIIS.Academic.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public class AdmissionInfoConfiguration : IEntityTypeConfiguration<AdmissionInfo>
{
    public void Configure(EntityTypeBuilder<AdmissionInfo> builder)
    {
        builder.ToTable("AdmissionInfos", table =>
            table.HasCheckConstraint(
                "CK_AdmissionInfos_AnneeBac",
                "[AnneeObtentionBaccalaureat] IS NULL OR [AnneeObtentionBaccalaureat] BETWEEN 1950 AND 2100"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SerieBaccalaureat).HasMaxLength(30);
        builder.Property(x => x.MentionBaccalaureat).HasMaxLength(30);
        builder.Property(x => x.DiplomeEntree).HasMaxLength(150);
        builder.Property(x => x.SpecialiteDiplomeEntree).HasMaxLength(150);
        builder.Property(x => x.NumeroEquivalence).HasMaxLength(80);
        builder.Property(x => x.DiplomeEquivalence).HasMaxLength(150);
        builder.HasIndex(x => x.EtudiantId).IsUnique();

        builder.HasOne(x => x.Etudiant)
            .WithOne(x => x.AdmissionInfo)
            .HasForeignKey<AdmissionInfo>(x => x.EtudiantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
