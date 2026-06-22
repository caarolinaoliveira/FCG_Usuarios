using FCG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Infrastructure.Mappings
{
    public class UsuarioMapping : IEntityTypeConfiguration<UsuarioEntity>
    {
        public void Configure(EntityTypeBuilder<UsuarioEntity> builder)
        {
            builder.ToTable("Usuarios");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.IdentityUserId)
                .IsRequired()
                .HasMaxLength(450); 
                
            builder.HasIndex(u => u.IdentityUserId)
                .IsUnique();

            builder.Property(u => u.DataNascimento)
                .IsRequired()
                .HasColumnType("date") 
                .HasConversion(
                    d => d.ToDateTime(TimeOnly.MinValue),
                    d => DateOnly.FromDateTime(d)
                );

            builder.Property(u => u.StatusConta)
                .IsRequired();

            builder.Property(u => u.DataCriacao)
                .IsRequired();

        }
    }
}