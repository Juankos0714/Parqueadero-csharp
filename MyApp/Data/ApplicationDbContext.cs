using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<RegistroParqueo> RegistrosParqueo { get; set; }
        public DbSet<ReservaCupo> ReservasCupos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Las relaciones se manejan por convención de Entity Framework
            // y las restricciones están definidas en el script SQL
            base.OnModelCreating(modelBuilder);
        }
    }
}