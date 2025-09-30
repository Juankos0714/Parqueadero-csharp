using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Services;

namespace MyApp.Controllers
{
    public class ParqueoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ParqueaderoService _parqueaderoService;

        public ParqueoController(ApplicationDbContext context, ParqueaderoService parqueaderoService)
        {
            _context = context;
            _parqueaderoService = parqueaderoService;
        }

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

        [HttpPost]
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