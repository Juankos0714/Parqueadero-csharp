using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class Vehiculo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La placa es obligatoria")]
        [StringLength(10)]
        public string Placa { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo es obligatorio")]
        public string Tipo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La marca es obligatoria")]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "El modelo es obligatorio")]
        public string Modelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un propietario")]
        public int UsuarioId { get; set; }

        // Navegación
        public virtual Usuario Usuario { get; set; } = null!;
        public virtual ICollection<RegistroParqueo> RegistrosParqueo { get; set; } = new List<RegistroParqueo>();
    }
}