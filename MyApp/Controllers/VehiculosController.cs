using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;
using MyApp.Services;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyApp.Controllers
{
    [Authorize] // Requiere autenticación
    public class VehiculosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ParqueaderoService _parqueaderoService;

        public VehiculosController(ApplicationDbContext context, ParqueaderoService parqueaderoService)
        {
            _context = context;
            _parqueaderoService = parqueaderoService;
        }

        // --- Funcionario: Ver todos los vehículos ---
        [Authorize(Roles = "Funcionario")]
        public async Task<IActionResult> Index()
        {
            var vehiculos = await _context.Vehiculos
                .Include(v => v.Usuario)
                .ToListAsync();
            return View(vehiculos);
        }

        // --- Aprendiz: Ver solo sus vehículos ---
        [Authorize(Roles = "Aprendiz")]
        public async Task<IActionResult> MisVehiculos()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int usuarioId))
            {
                TempData["Error"] = "Error de autenticación. Inicie sesión.";
                return RedirectToAction("Login", "Account");
            }

            var vehiculos = await _context.Vehiculos
                .Include(v => v.Usuario)
                // Se incluye la reserva activa para mostrar el estado en la vista
                .Include(v => v.ReservasCupos.Where(r => r.Activa && r.FechaHoraVencimiento > DateTime.Now))
                .Where(v => v.UsuarioId == usuarioId)
                .ToListAsync();

            return View("MisVehiculos", vehiculos);
        }

        // --- Aprendiz: Formulario de Creación ---
        [Authorize(Roles = "Aprendiz")]
        public async Task<IActionResult> Create()
        {
            // 1. Obtener y validar el ID del usuario actual
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int usuarioId) || usuarioId == 0)
            {
                // Error de sesión. Redirigir al login.
                TempData["Error"] = "Error de sesión. No se pudo identificar al usuario. Vuelve a iniciar sesión.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
            {
                // Usuario autenticado no existe en la base de datos (problema de datos)
                TempData["Error"] = "Tu perfil de usuario no fue encontrado. Contacta a soporte.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Usuario = usuario;
            return View();
        }

        // --- Aprendiz: Crear vehículo (POST) ---
        [HttpPost]
        [Authorize(Roles = "Aprendiz")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Placa, string Tipo, string Marca, string Modelo)
        {
            // 1. Obtener y validar el ID del usuario actual
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int usuarioId) || usuarioId == 0)
            {
                TempData["Error"] = "Error de sesión. No se pudo identificar al usuario para registrar el vehículo.";
                return RedirectToAction("Login", "Account");
            }

            // El resto de la lógica debe ir dentro del try/catch
            try
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);

                if (usuario == null)
                {
                    TempData["Error"] = "Tu perfil de usuario no fue encontrado. Contacta a soporte.";
                    return RedirectToAction("Login", "Account");
                }

                // Validaciones básicas de modelo
                if (string.IsNullOrWhiteSpace(Placa) || string.IsNullOrWhiteSpace(Tipo) || string.IsNullOrWhiteSpace(Marca) || string.IsNullOrWhiteSpace(Modelo))
                {
                    TempData["Error"] = "Todos los campos son obligatorios.";
                    ViewBag.Usuario = usuario; // Necesario para mostrar la vista de nuevo con el error
                                               // Devolver un objeto Vehiculo parcial con los datos ingresados
                    return View(new Vehiculo { Placa = Placa, Tipo = Tipo, Marca = Marca, Modelo = Modelo });
                }

                var vehiculo = new Vehiculo
                {
                    Placa = Placa.Trim().ToUpper(),
                    Tipo = Tipo.Trim(),
                    Marca = Marca.Trim(),
                    Modelo = Modelo.Trim(),
                    UsuarioId = usuarioId // 👈 ¡Asignación crucial y ahora segura!
                };

                _context.Add(vehiculo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Vehículo registrado exitosamente.";
                return RedirectToAction(nameof(MisVehiculos));
            }
            catch (Exception ex)
            {
                // Se captura cualquier error de base de datos (ej: duplicidad de placa)
                TempData["Error"] = "Error al registrar el vehículo: " + ex.Message;

                // Volver a cargar el usuario (siempre que el usuarioId sea válido)
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                ViewBag.Usuario = usuario;

                // Devolver un objeto Vehiculo parcial con los datos ingresados
                return View(new Vehiculo { Placa = Placa, Tipo = Tipo, Marca = Marca, Modelo = Modelo });
            }
        }


        // --- Funcionario: Ingresar vehículo (DELEGADO AL SERVICIO) ---
        [HttpPost]
        [Authorize(Roles = "Funcionario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ingresar(int id)
        {
            try
            {
                // El servicio maneja la lógica de validación de cupo y reserva
                string mensaje = await _parqueaderoService.IngresarVehiculo(id);
                TempData["Success"] = mensaje;
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Vehículo no encontrado.";
            }
            catch (InvalidOperationException ex)
            {
                // Captura errores de negocio: Ya tiene registro activo o parqueadero lleno sin reserva válida
                TempData["Warning"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error inesperado al intentar ingresar el vehículo.";
            }
            return RedirectToAction("Index", "Parqueo"); // Redirige a la lista de vehículos activos del Funcionario
        }

        // --- Aprendiz: Reservar cupo (DELEGADO AL SERVICIO) ---
        [HttpPost]
        [Authorize(Roles = "Aprendiz")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int usuarioId))
            {
                TempData["Error"] = "Error de autenticación. Inicie sesión.";
                return RedirectToAction(nameof(MisVehiculos));
            }

            // 1. Verificación de propiedad (Seguridad)
            var vehiculo = await _context.Vehiculos.FindAsync(id);
            if (vehiculo == null || vehiculo.UsuarioId != usuarioId)
            {
                TempData["Error"] = "Acceso denegado: Vehículo no encontrado o no pertenece a tu cuenta.";
                return RedirectToAction(nameof(MisVehiculos));
            }

            try
            {
                // 2. Llamar al servicio para la lógica de negocio (Reserva)
                await _parqueaderoService.ReservarCupo(id);
                TempData["Success"] = $"¡Reserva exitosa para la placa **{vehiculo.Placa}**! Tienes **30 minutos** para ingresar y asegurar tu cupo 'Dentro'.";
            }
            catch (InvalidOperationException ex)
            {
                // Captura los errores de negocio (Ya hay cupo, ya tiene reserva activa)
                TempData["Warning"] = ex.Message;
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Vehículo no encontrado.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error inesperado al intentar realizar la reserva.";
            }
            return RedirectToAction(nameof(MisVehiculos));
        }
    }
}