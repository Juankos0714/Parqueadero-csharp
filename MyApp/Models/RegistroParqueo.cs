using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class RegistroParqueo
    {
        public int Id { get; set; }
        
        public int VehiculoId { get; set; }
        
        public DateTime FechaHoraEntrada { get; set; } = DateTime.Now;
        
        public DateTime? FechaHoraSalida { get; set; }
        
        public string Estado { get; set; } = "Dentro"; // Dentro, Fuera, Salido
        
        public decimal? ValorPagado { get; set; }
        
        public int TiempoMinutos { get; set; }
        
        // Navegación
        public virtual Vehiculo Vehiculo { get; set; } = null!;
    }
}