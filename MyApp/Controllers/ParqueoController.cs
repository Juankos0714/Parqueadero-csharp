using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Services;
using System.Security.Claims;
using System.Collections.Generic; // Para List
using System.Threading.Tasks;
using System; // Para Exception

namespace MyApp.Controllers
{
    [Authorize] // Requiere autenticación para todo el controlador
    public class ParqueoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ParqueaderoService _parqueaderoService;

        public ParqueoController(ApplicationDbContext context, ParqueaderoService parqueaderoService)
        {
            _context = context;
            _parqueaderoService = parqueaderoService;
        }

        // --- FUNCIONARIO: Vehículos Activos (Index) ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Index()
        {
            var registrosActivos = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .ThenInclude(v => v.Usuario)
                .Where(r => r.FechaHoraSalida == null)
                .OrderByDescending(r => r.FechaHoraEntrada)
                .ToListAsync();

            // También puedes mostrar la disponibilidad en el ViewBag
            var disponibilidad = await _parqueaderoService.ObtenerDisponibilidad();
            ViewBag.Disponibilidad = disponibilidad;

            return View(registrosActivos);
        }

        // --- FUNCIONARIO: Registrar Salida (Refactorizado) ---
        [HttpPost]
        [Authorize(Roles = "Funcionario")]
        [ValidateAntiForgeryToken] // Recomendado usar esto en lugar de IgnoreAntiforgeryToken
        public async Task<IActionResult> RegistrarSalida(int id)
        {
            try
            {
                // El servicio maneja la lógica de cálculo (CalcularTarifa) y el cierre de registro.
                var (valorPagado, tiempoMinutos) = await _parqueaderoService.RegistrarSalida(id);

                TempData["Success"] = $"Salida registrada. El valor pagado es de **${valorPagado:N0}** por **{tiempoMinutos}** minutos de parqueo.";
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error inesperado al registrar la salida.";
            }

            return RedirectToAction(nameof(Index));
        }

        // --- FUNCIONARIO: Historial Completo ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Historial()
        {
            var historial = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .ThenInclude(v => v.Usuario)
                .Where(r => r.FechaHoraSalida != null)
                .OrderByDescending(r => r.FechaHoraSalida)
                .ToListAsync();

            return View(historial);
        }

        // --- APRENDIZ: Mi Historial (Refactorizado con Servicio) ---
        [Authorize(Roles = "Aprendiz")]
        public async Task<IActionResult> MiHistorial()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Validar y obtener el ID del usuario
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
            {
                TempData["Error"] = "Error al identificar al usuario. Inicie sesión nuevamente.";
                return RedirectToAction("Login", "Account");
            }

            // Se delega la obtención del historial al servicio
            var historial = await _parqueaderoService.ObtenerRegistrosUsuario(usuarioId);

            return View("MiHistorial", historial);
        }

        // --- APRENDIZ: Reservar Cupo (NUEVO) ---
        [HttpPost]
        [Authorize(Roles = "Aprendiz")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReservarCupo(int vehiculoId)
        {
            try
            {
                await _parqueaderoService.ReservarCupo(vehiculoId);
                TempData["Success"] = "¡Reserva exitosa! Tienes **30 minutos** para ingresar y asegurar tu cupo 'Dentro'.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Warning"] = ex.Message;
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Vehículo no encontrado o no tienes permiso para reservarlo.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error inesperado al intentar reservar el cupo.";
            }

            // Asume que hay un controlador Vehiculos con una acción MisVehiculos para volver
            return RedirectToAction("MisVehiculos", "Vehiculos");
        }

        // --- FUNCIONARIO: Reportes (Refactorizado con Servicio) ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Reportes()
        {
            // Se delega toda la lógica de cálculo de reportes al servicio
            var (ingresosMes, reporteVehiculos) = await _parqueaderoService.ObtenerIngresosMes();

            ViewBag.IngresosMes = ingresosMes;
            ViewBag.ReporteVehiculos = reporteVehiculos;

            return View();
        }
    }

    // Se mantiene aquí para que sea visible en el namespace MyApp.Controllers
    public class ReporteVehiculo
    {
        public string Placa { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
    }
}