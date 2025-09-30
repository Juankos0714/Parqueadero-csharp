using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MyApp.Data;
using MyApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ParqueaderoService _parqueaderoService;

        public HomeController(ApplicationDbContext context, ParqueaderoService parqueaderoService)
        {
            _context = context;
            _parqueaderoService = parqueaderoService;
        }

        public async Task<IActionResult> Index()
        {
            var disponibilidad = await _parqueaderoService.ObtenerDisponibilidad();
            
            ViewBag.CarrosDentro = disponibilidad.carrosDentro;
            ViewBag.MotosDentro = disponibilidad.motosDentro;
            ViewBag.CarrosFuera = disponibilidad.carrosFuera;
            ViewBag.MotosFuera = disponibilidad.motosFuera;
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}