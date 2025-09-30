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
            ViewData["UsuarioId"] = new SelectList(await _context.Usuarios.ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([Bind("Placa,Tipo,Marca,Modelo,UsuarioId")] Vehiculo vehiculo)
        {
            try
            {
                // NO incluyas Id en el Bind
                if (ModelState.IsValid)
                {
                    _context.Add(vehiculo);
                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = "Vehículo registrado exitosamente";
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
                TempData["Error"] = "Error al registrar el vehículo: " + ex.Message;
            }
            ViewData["UsuarioId"] = new SelectList(await _context.Usuarios.ToListAsync(), "Id", "Nombre", vehiculo.UsuarioId);
            return View(vehiculo);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Ingresar(int id)
        {
            var mensaje = await _parqueaderoService.IngresarVehiculo(id);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Reservar(int id)
        {
            var reservado = await _parqueaderoService.ReservarCupo(id);
            TempData["Mensaje"] = reservado ? "Cupo reservado por 30 minutos" : "No es necesario reservar cupo";
            return RedirectToAction(nameof(Index));
        }
    }
}