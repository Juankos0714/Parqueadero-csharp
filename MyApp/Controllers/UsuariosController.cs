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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Email,Rol")] Usuario usuario)
        {
            try
            {
                // NO incluyas Id en el Bind
                if (ModelState.IsValid)
                {
                    _context.Add(usuario);
                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = "Usuario creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Mostrar errores de validación
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["Error"] = "Errores de validación: " + string.Join(", ", errors);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear el usuario: " + ex.Message;
            }
            return View(usuario);
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