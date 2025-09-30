using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Rol { get; set; } = "Aprendiz"; // Aprendiz o Funcionario
        
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        
        // Navegación
        public virtual ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}