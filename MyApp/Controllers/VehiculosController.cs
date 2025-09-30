using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Controllers
{
    public class VehiculosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ParqueaderoService _parqueaderoService;

        public VehiculosController(ApplicationDbContext context, ParqueaderoService parqueaderoService)
        {
            _context = context;
            _parqueaderoService = parqueaderoService;
        }

        public async Task<IActionResult> Index()
        {
            var vehiculos = await _context.Vehiculos
                .Include(v => v.Usuario)
                .ToListAsync();
            return View(vehiculos);
        }

        public async Task<IActionResult> Create()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            ViewBag.Usuarios = usuarios;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string Placa, string Tipo, string Marca, string Modelo, int UsuarioId)
        {
            try
            {
                Console.WriteLine($"=== DATOS RECIBIDOS ===");
                Console.WriteLine($"Placa: '{Placa}'");
                Console.WriteLine($"Tipo: '{Tipo}'");
                Console.WriteLine($"Marca: '{Marca}'");
                Console.WriteLine($"Modelo: '{Modelo}'");
                Console.WriteLine($"UsuarioId: {UsuarioId}");
                Console.WriteLine($"======================");

                // Validaciones
                if (string.IsNullOrWhiteSpace(Placa))
                {
                    TempData["Error"] = "La placa es obligatoria";
                    ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                    return View();
                }

                if (string.IsNullOrWhiteSpace(Tipo))
                {
                    TempData["Error"] = "El tipo de vehículo es obligatorio";
                    ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                    return View();
                }

                if (string.IsNullOrWhiteSpace(Marca))
                {
                    TempData["Error"] = "La marca es obligatoria";
                    ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                    return View();
                }

                if (string.IsNullOrWhiteSpace(Modelo))
                {
                    TempData["Error"] = "El modelo es obligatorio";
                    ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                    return View();
                }

                if (UsuarioId <= 0)
                {
                    TempData["Error"] = "Debe seleccionar un propietario";
                    ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                    return View();
                }

                // Verificar que el usuario existe
                var usuario = await _context.Usuarios.FindAsync(UsuarioId);
                if (usuario == null)
                {
                    TempData["Error"] = "El usuario seleccionado no existe";
                    ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                    return View();
                }

                // Crear el vehículo
                var vehiculo = new Vehiculo
                {
                    Placa = Placa.Trim().ToUpper(),
                    Tipo = Tipo.Trim(),
                    Marca = Marca.Trim(),
                    Modelo = Modelo.Trim(),
                    UsuarioId = UsuarioId
                };

                _context.Add(vehiculo);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = "Vehículo registrado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                TempData["Error"] = "Error al registrar el vehículo: " + ex.Message;
                ViewBag.Usuarios = await _context.Usuarios.ToListAsync();
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Ingresar(int id)
        {
            var mensaje = await _parqueaderoService.IngresarVehiculo(id);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reservar(int id)
        {
            var reservado = await _parqueaderoService.ReservarCupo(id);
            TempData["Mensaje"] = reservado ? "Cupo reservado por 30 minutos" : "No es necesario reservar cupo";
            return RedirectToAction(nameof(Index));
        }
    }
}