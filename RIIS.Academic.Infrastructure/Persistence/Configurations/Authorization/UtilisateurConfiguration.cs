using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.ToTable("Utilisateurs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NomUtilisateur).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MotDePasseHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.NomUtilisateur).IsUnique();

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
