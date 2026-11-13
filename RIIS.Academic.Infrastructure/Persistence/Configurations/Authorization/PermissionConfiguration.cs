using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RIIS.Academic.Domain;

namespace RIIS.Academic.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Libelle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Route).HasMaxLength(250);
        builder.Property(x => x.Icon).HasMaxLength(60);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.ParentPermissionId, x.OrdreAffichage });

        builder.HasOne(x => x.ParentPermission)
            .WithMany(x => x.Enfants)
            .HasForeignKey(x => x.ParentPermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
