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
    public DbSet<RelacionAsignatura> RelacionesAsignatura => Set<RelacionAsignatura>();
    public DbSet<ProyeccionOferta> ProyeccionesOferta => Set<ProyeccionOferta>();
    public DbSet<GrupoOferta> GruposOferta => Set<GrupoOferta>();
    public DbSet<SesionOferta> SesionesOferta => Set<SesionOferta>();
    public DbSet<SolicitudOferta> SolicitudesOferta => Set<SolicitudOferta>();
    public DbSet<UsuarioAplicacion> UsuariosAplicacion => Set<UsuarioAplicacion>();
    public DbSet<TokenRecuperacionPassword> TokensRecuperacion => Set<TokenRecuperacionPassword>();
    public DbSet<PlanificacionPregrado> PlanificacionPregrado => Set<PlanificacionPregrado>();

    private static readonly JsonSerializerOptions JsonOpts = new();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Salon>().HasKey(s => s.Codigo);
        mb.Entity<UsuarioAplicacion>().HasIndex(u => u.Correo).IsUnique();
        mb.Entity<TokenRecuperacionPassword>().HasIndex(t => t.TokenHash).IsUnique();
        mb.Entity<TokenRecuperacionPassword>().HasOne(t => t.UsuarioAplicacion).WithMany().HasForeignKey(t => t.UsuarioAplicacionId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<PlanificacionPregrado>().HasIndex(p => p.NombreAsignatura);
        mb.Entity<Programa>().HasKey(p => p.Codigo);

        mb.Entity<RelacionAsignatura>()
            .HasOne<Asignatura>().WithMany().HasForeignKey(r => r.AsignaturaId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<RelacionAsignatura>()
            .HasOne<Asignatura>().WithMany().HasForeignKey(r => r.RelacionadaId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<RelacionAsignatura>()
            .HasIndex(r => new { r.AsignaturaId, r.RelacionadaId, r.Tipo }).IsUnique();

        mb.Entity<ProyeccionOferta>().HasMany(p => p.Grupos)
            .WithOne(g => g.Proyeccion).HasForeignKey(g => g.ProyeccionOfertaId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<ProyeccionOferta>().HasMany(p => p.Solicitudes)
            .WithOne(s => s.Proyeccion).HasForeignKey(s => s.ProyeccionOfertaId)
            .OnDelete(DeleteBehavior.Cascade);
        mb.Entity<SolicitudOferta>().HasOne(s => s.Asignatura).WithMany()
            .HasForeignKey(s => s.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<GrupoOferta>().HasOne(g => g.Asignatura).WithMany()
            .HasForeignKey(g => g.AsignaturaId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<GrupoOferta>().HasOne(g => g.Profesor).WithMany()
            .HasForeignKey(g => g.ProfesorId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<GrupoOferta>().HasOne(g => g.Salon).WithMany()
            .HasForeignKey(g => g.SalonCodigo).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<GrupoOferta>().HasMany(g => g.Sesiones)
            .WithOne(s => s.Grupo).HasForeignKey(s => s.GrupoOfertaId)
            .OnDelete(DeleteBehavior.Cascade);

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
        mb.Entity<Salon>()
            .Property(s => s.Disponibilidad)
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
