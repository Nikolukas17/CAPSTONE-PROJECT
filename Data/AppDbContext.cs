using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OfertaAcademica.Web.Models.Entities;

namespace OfertaAcademica.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();
    public DbSet<Profesor>   Profesores  => Set<Profesor>();
    public DbSet<Salon>      Salones     => Set<Salon>();
    public DbSet<Programa>   Programas   => Set<Programa>();

    private static readonly JsonSerializerOptions JsonOpts = new();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Salon>()  .HasKey(s => s.Codigo);
        mb.Entity<Programa>().HasKey(p => p.Codigo);

        // Asignatura -> Programa (sin propiedad de navegación).
        mb.Entity<Asignatura>()
            .HasOne<Programa>()
            .WithMany()
            .HasForeignKey(a => a.ProgramaCodigo)
            .OnDelete(DeleteBehavior.Restrict);

        // Materias del profesor en su propia tabla (ProfesorMaterias).
        mb.Entity<Profesor>().OwnsMany(p => p.Materias, m =>
        {
            m.ToTable("ProfesorMaterias");
            m.WithOwner().HasForeignKey("ProfesorId");
            m.Property<int>("Id");
            m.HasKey("Id");
            m.HasIndex(x => x.Codigo);
        });

        foreach (var tipo in new[] { typeof(Asignatura), typeof(Profesor), typeof(Salon) })
            mb.Entity(tipo).Property<string>(TextoBusqueda.Columna).IsRequired().HasDefaultValue("");

        var dispConverter = new ValueConverter<List<EstadoFranja>, string>(
            v => JsonSerializer.Serialize(v, JsonOpts),
            v => JsonSerializer.Deserialize<List<EstadoFranja>>(v, JsonOpts) ?? DisponibilidadSemanal.Vacia());

        var dispComparer = new ValueComparer<List<EstadoFranja>>(
            (a, b) => a!.SequenceEqual(b!),
            c => c.Aggregate(0, (h, v) => HashCode.Combine(h, (int)v)),
            c => c.ToList());

        mb.Entity<Asignatura>()
            .Property(a => a.Disponibilidad)
            .HasConversion(dispConverter, dispComparer)
            .HasColumnType("TEXT");

        mb.Entity<Profesor>()
            .Property(p => p.Disponibilidad)
            .HasConversion(dispConverter, dispComparer)
            .HasColumnType("TEXT");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ActualizarTextoBusqueda();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ActualizarTextoBusqueda();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ActualizarTextoBusqueda()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

            var texto = entry.Entity switch
            {
                Asignatura a => TextoBusqueda.Componer(a.Codigo, a.CodigoCompartido, a.Nombre),
                Profesor p   => TextoBusqueda.Componer(p.Nombre, p.Correo),
                Salon s      => TextoBusqueda.Componer(s.Codigo, s.Nombre),
                _ => null
            };

            if (texto is not null)
                entry.Property(TextoBusqueda.Columna).CurrentValue = texto;
        }
    }
}
