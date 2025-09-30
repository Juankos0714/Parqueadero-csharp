using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Usuarios.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string Nombre, string Email, string Rol)
        {
            try
            {
                // Debug: Log de lo que llega
                Console.WriteLine($"=== DATOS RECIBIDOS ===");
                Console.WriteLine($"Nombre: '{Nombre}'");
                Console.WriteLine($"Email: '{Email}'");
                Console.WriteLine($"Rol: '{Rol}'");
                Console.WriteLine($"======================");

                // Crear el usuario manualmente
                var usuario = new Usuario
                {
                    Nombre = Nombre?.Trim() ?? string.Empty,
                    Email = Email?.Trim() ?? string.Empty,
                    Rol = Rol?.Trim() ?? string.Empty,
                    FechaRegistro = DateTime.Now
                };

                // Validar manualmente
                if (string.IsNullOrWhiteSpace(usuario.Nombre))
                {
                    TempData["Error"] = "El nombre es obligatorio";
                    return View(usuario);
                }

                if (string.IsNullOrWhiteSpace(usuario.Email))
                {
                    TempData["Error"] = "El email es obligatorio";
                    return View(usuario);
                }

                if (!usuario.Email.Contains("@"))
                {
                    TempData["Error"] = "El email no es válido";
                    return View(usuario);
                }

                if (string.IsNullOrWhiteSpace(usuario.Rol))
                {
                    TempData["Error"] = "Debe seleccionar un rol";
                    return View(usuario);
                }

                _context.Add(usuario);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Usuario creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                TempData["Error"] = "Error al crear el usuario: " + ex.Message;
                return View(new Usuario());
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios
                .Include(u => u.Vehiculos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (usuario == null) return NotFound();

            return View(usuario);
        }
    }
}