using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class Vehiculo
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Placa { get; set; } = string.Empty;
        
        [Required]
        public string Tipo { get; set; } = string.Empty; // Carro o Moto
        
        [Required]
        public string Marca { get; set; } = string.Empty;
        
        [Required]
        public string Modelo { get; set; } = string.Empty;
        
        public int UsuarioId { get; set; }
        
        // Navegación
        public virtual Usuario Usuario { get; set; } = null!;
        public virtual ICollection<RegistroParqueo> RegistrosParqueo { get; set; } = new List<RegistroParqueo>();
    }
}