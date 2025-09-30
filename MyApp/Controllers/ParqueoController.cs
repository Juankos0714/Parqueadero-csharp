using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Services;
using System.Security.Claims;

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

        // Solo Funcionarios pueden ver todos los registros activos
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

        // Solo Funcionarios pueden registrar salidas
        [HttpPost]
        [Authorize(Roles = "Funcionario")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RegistrarSalida(int id)
        {
            var registro = await _context.RegistrosParqueo.FindAsync(id);
            if (registro == null) return NotFound();

            registro.FechaHoraSalida = DateTime.Now;
            registro.TiempoMinutos = (int)(registro.FechaHoraSalida.Value - registro.FechaHoraEntrada).TotalMinutes;
            registro.ValorPagado = await _parqueaderoService.CalcularTarifa(id);

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = $"Salida registrada. Valor a pagar: ${registro.ValorPagado:N0}";
            return RedirectToAction(nameof(Index));
        }

        // Solo Funcionarios pueden ver el historial completo
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

        // Aprendices pueden ver solo su historial
        [Authorize(Roles = "Aprendiz")]
        public async Task<IActionResult> MiHistorial()
        {
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var historial = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .ThenInclude(v => v.Usuario)
                .Where(r => r.Vehiculo.UsuarioId == usuarioId && r.FechaHoraSalida != null)
                .OrderByDescending(r => r.FechaHoraSalida)
                .ToListAsync();

            return View("MiHistorial", historial);
        }

        // Solo Funcionarios pueden ver reportes
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Reportes()
        {
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var ingresosMes = await _context.RegistrosParqueo
                .Where(r => r.FechaHoraSalida != null &&
                           r.FechaHoraSalida >= inicioMes &&
                           r.FechaHoraSalida < finMes)
                .SumAsync(r => r.ValorPagado ?? 0);

            var reporteVehiculos = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .Where(r => r.FechaHoraSalida != null &&
                           r.FechaHoraSalida >= inicioMes &&
                           r.FechaHoraSalida < finMes)
                .GroupBy(r => r.Vehiculo.Placa)
                .Select(g => new ReporteVehiculo
                {
                    Placa = g.Key,
                    TotalPagado = g.Sum(r => r.ValorPagado ?? 0)
                })
                .ToListAsync();

            ViewBag.IngresosMes = ingresosMes;
            ViewBag.ReporteVehiculos = reporteVehiculos;

            return View();
        }
    }

    public class ReporteVehiculo
    {
        public string Placa { get; set; } = string.Empty;
        public decimal TotalPagado { get; set; }
    }
}