using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;
using MyApp.Services;
using System.Security.Claims;

namespace MyApp.Controllers
{
    [Authorize]
    public class ParqueoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ParqueaderoService _parqueaderoService;

        public ParqueoController(ApplicationDbContext context, ParqueaderoService parqueaderoService)
        {
            _context = context;
            _parqueaderoService = parqueaderoService;
        }

        // --- Funcionario: Ver vehículos activos en parqueadero ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Index()
        {
            var registrosActivos = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .ThenInclude(v => v.Usuario)
                .Where(r => r.FechaHoraSalida == null)
                .OrderByDescending(r => r.FechaHoraEntrada)
                .ToListAsync();

            return View(registrosActivos);
        }

        // --- Funcionario: Registrar salida de vehículo ---
        [HttpPost]
        [Authorize(Roles = "Funcionario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarSalida(int id)
        {
            try
            {
                var (valorPagado, tiempoMinutos) = await _parqueaderoService.RegistrarSalida(id);
                TempData["Success"] = $"Salida registrada exitosamente. Valor a pagar: ${valorPagado:N0}. Tiempo: {tiempoMinutos} minutos.";
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Registro no encontrado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al registrar la salida: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // --- Funcionario: Ver historial completo ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Historial()
        {
            var registros = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .ThenInclude(v => v.Usuario)
                .Where(r => r.FechaHoraSalida != null)
                .OrderByDescending(r => r.FechaHoraSalida)
                .ToListAsync();

            return View(registros);
        }

        // --- Funcionario: Ver reportes ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Reportes()
        {
            var (ingresosMes, reporteVehiculos) = await _parqueaderoService.ObtenerIngresosMes();

            ViewBag.IngresosMes = ingresosMes;
            ViewBag.ReporteVehiculos = reporteVehiculos;

            return View();
        }

        // --- Funcionario: Ver disponibilidad ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Disponibilidad()
        {
            var disponibilidad = await _parqueaderoService.ObtenerDisponibilidad();

            ViewBag.CarrosDentro = disponibilidad.carrosDentro;
            ViewBag.MotosDentro = disponibilidad.motosDentro;
            ViewBag.CarrosFuera = disponibilidad.carrosFuera;
            ViewBag.MotosFuera = disponibilidad.motosFuera;
            ViewBag.MaxCarros = 20;
            ViewBag.MaxMotos = 20;

            return View();
        }

        // --- Aprendiz: Ver su historial ---
        [Authorize(Roles = "Aprendiz")]
        public async Task<IActionResult> MiHistorial()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int usuarioId))
            {
                TempData["Error"] = "Error de autenticación.";
                return RedirectToAction("Login", "Account");
            }

            var registros = await _parqueaderoService.ObtenerRegistrosUsuario(usuarioId);
            return View(registros);
        }
    }

    // Clase auxiliar para reportes
    public class ReporteVehiculo
    {
        public string Placa { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
    }
}