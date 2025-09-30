using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class ReservaCupo
    {
        public int Id { get; set; }
        
        public int VehiculoId { get; set; }
        
        public DateTime FechaHoraReserva { get; set; } = DateTime.Now;
        
        public DateTime FechaHoraVencimiento { get; set; }
        
        public bool Activa { get; set; } = true;
        
        // Navegación
        public virtual Vehiculo Vehiculo { get; set; } = null!;
    }
}