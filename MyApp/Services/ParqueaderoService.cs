using Microsoft.EntityFrameworkCore;
using MyApp.Controllers;
using MyApp.Data;
using MyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Services
{
    public class ParqueaderoService
    {
        private readonly ApplicationDbContext _context;
        private const int MAX_CARROS = 20;
        private const int MAX_MOTOS = 20;

        // Tarifas por hora
        private const decimal TARIFA_CARRO_DENTRO = 2000;
        private const decimal TARIFA_CARRO_FUERA = 1500;
        private const decimal TARIFA_MOTO_DENTRO = 1500;
        private const decimal TARIFA_MOTO_FUERA = 1000;

        public ParqueaderoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------
        // DISPONIBILIDAD y UTILIDAD
        // -------------------------------------------------------------------

        // Retorna la disponibilidad de espacios
        public async Task<(int carrosDentro, int motosDentro, int carrosFuera, int motosFuera)> ObtenerDisponibilidad()
        {
            var registrosActivos = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .Where(r => r.FechaHoraSalida == null)
                .ToListAsync();

            var carrosDentro = registrosActivos.Count(r => r.Vehiculo.Tipo == "Carro" && r.Estado == "Dentro");
            var motosDentro = registrosActivos.Count(r => r.Vehiculo.Tipo == "Moto" && r.Estado == "Dentro");
            var carrosFuera = registrosActivos.Count(r => r.Vehiculo.Tipo == "Carro" && r.Estado == "Fuera");
            var motosFuera = registrosActivos.Count(r => r.Vehiculo.Tipo == "Moto" && r.Estado == "Fuera");

            return (carrosDentro, motosDentro, carrosFuera, motosFuera);
        }

        // Nuevo: Para MiHistorial del Aprendiz
        public async Task<List<RegistroParqueo>> ObtenerRegistrosUsuario(int usuarioId)
        {
            return await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .ThenInclude(v => v.Usuario)
                .Where(r => r.Vehiculo.UsuarioId == usuarioId && r.FechaHoraSalida != null)
                .OrderByDescending(r => r.FechaHoraEntrada)
                .ToListAsync();
        }

        // Método auxiliar para obtener la tarifa por hora
        private decimal GetTarifaPorHora(string tipo, string estado)
        {
            if (tipo == "Carro")
            {
                return estado == "Dentro" ? TARIFA_CARRO_DENTRO : TARIFA_CARRO_FUERA;
            }
            // Moto
            return estado == "Dentro" ? TARIFA_MOTO_DENTRO : TARIFA_MOTO_FUERA;
        }

        // -------------------------------------------------------------------
        // INGRESO (Funcionario)
        // -------------------------------------------------------------------
        public async Task<string> IngresarVehiculo(int vehiculoId)
        {
            var vehiculo = await _context.Vehiculos
                .Include(v => v.Usuario) // Incluir Usuario para el chequeo de Rol/Reserva
                .FirstOrDefaultAsync(v => v.Id == vehiculoId);

            if (vehiculo == null) throw new KeyNotFoundException("Vehículo no encontrado.");

            // 1. Validar que no tenga un registro activo
            var registroActivo = await _context.RegistrosParqueo
                .AnyAsync(r => r.VehiculoId == vehiculoId && r.FechaHoraSalida == null);

            if (registroActivo) throw new InvalidOperationException($"El vehículo {vehiculo.Placa} ya tiene un registro activo.");

            var disponibilidad = await ObtenerDisponibilidad();
            string estado = "Fuera";
            bool cupoDentroDisponible = false;

            // 2. Asignación automática de estado inicial
            if (vehiculo.Tipo == "Carro" && disponibilidad.carrosDentro < MAX_CARROS)
            {
                estado = "Dentro";
                cupoDentroDisponible = true;
            }
            else if (vehiculo.Tipo == "Moto" && disponibilidad.motosDentro < MAX_MOTOS)
            {
                estado = "Dentro";
                cupoDentroDisponible = true;
            }

            // 3. Lógica de Reserva (Solo para Aprendices cuando el parqueadero está lleno)
            if (vehiculo.Usuario.Rol == "Aprendiz" && !cupoDentroDisponible)
            {
                // Si está lleno, debe tener una reserva activa para entrar.
                var reservaActiva = await _context.ReservasCupos
                    .Where(r => r.VehiculoId == vehiculoId && r.Activa && r.FechaHoraVencimiento > DateTime.Now)
                    .FirstOrDefaultAsync();

                if (reservaActiva != null)
                {
                    // Si el Aprendiz tiene reserva, se le garantiza un cupo "Dentro" (aunque esté lleno)
                    estado = "Dentro";
                    // Se cancela la reserva ya que se está utilizando
                    reservaActiva.Activa = false;
                    _context.ReservasCupos.Update(reservaActiva);
                }
                else
                {
                    // Si está lleno y NO tiene reserva, no se le permite ingresar.
                    throw new InvalidOperationException("El parqueadero interno está lleno y el Aprendiz no tiene reserva activa.");
                }
            }

            // 4. Si es Funcionario o si hay cupo "Dentro" (cupoDentroDisponible es true), se ingresa sin problema.

            var registro = new RegistroParqueo
            {
                VehiculoId = vehiculoId,
                Estado = estado,
                FechaHoraEntrada = DateTime.Now
            };

            _context.RegistrosParqueo.Add(registro);
            await _context.SaveChangesAsync();

            return $"Vehículo ingresado **{estado.ToLower()}** del parqueadero. Ubicación asignada: **{estado}**.";
        }

        // -------------------------------------------------------------------
        // SALIDA (Funcionario)
        // -------------------------------------------------------------------

        public async Task<(decimal valorPagado, int tiempoMinutos)> RegistrarSalida(int registroId)
        {
            var registro = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .FirstOrDefaultAsync(r => r.Id == registroId && r.FechaHoraSalida == null);

            if (registro == null) throw new KeyNotFoundException("Registro activo no encontrado.");

            registro.FechaHoraSalida = DateTime.Now;

            TimeSpan tiempoTranscurrido = registro.FechaHoraSalida.Value - registro.FechaHoraEntrada;

            // El cobro es por hora completa desde el momento del ingreso (redondeo hacia arriba)
            var horas = Math.Ceiling(tiempoTranscurrido.TotalHours);
            registro.TiempoMinutos = (int)tiempoTranscurrido.TotalMinutes;

            // Calcular el valor a pagar
            decimal tarifaPorHora = GetTarifaPorHora(registro.Vehiculo.Tipo, registro.Estado);
            registro.ValorPagado = tarifaPorHora * (decimal)horas;

            registro.Estado = "Salido"; // Marcar como salido

            _context.RegistrosParqueo.Update(registro);
            await _context.SaveChangesAsync();

            return (registro.ValorPagado.Value, registro.TiempoMinutos);
        }

        // -------------------------------------------------------------------
        // RESERVA (Aprendiz)
        // -------------------------------------------------------------------

        public async Task ReservarCupo(int vehiculoId)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(vehiculoId);
            if (vehiculo == null) throw new KeyNotFoundException("Vehículo no encontrado.");

            // 1. Validar que el vehículo no esté ya activo
            var registroActivo = await _context.RegistrosParqueo
                .AnyAsync(r => r.VehiculoId == vehiculoId && r.FechaHoraSalida == null);

            if (registroActivo) throw new InvalidOperationException("El vehículo ya está dentro del parqueadero.");

            // 2. Validar si hay cupo disponible (No permite reservar si hay cupos)
            var disponibilidad = await ObtenerDisponibilidad();
            bool hayCupo = (vehiculo.Tipo == "Carro" && disponibilidad.carrosDentro < MAX_CARROS) ||
                           (vehiculo.Tipo == "Moto" && disponibilidad.motosDentro < MAX_MOTOS);

            if (hayCupo) throw new InvalidOperationException("Hay cupo disponible interno. Puedes ingresar directamente.");

            // 3. Validar que no tenga reserva activa (no duplicar)
            var reservaActiva = await _context.ReservasCupos
               .AnyAsync(r => r.VehiculoId == vehiculoId && r.Activa && r.FechaHoraVencimiento > DateTime.Now);

            if (reservaActiva) throw new InvalidOperationException("Ya tienes una reserva activa. Espera a que caduque o úsala.");

            // 4. Crear Reserva
            var reserva = new ReservaCupo
            {
                VehiculoId = vehiculoId,
                FechaHoraReserva = DateTime.Now,
                FechaHoraVencimiento = DateTime.Now.AddMinutes(30), // 30 minutos de validez
                Activa = true
            };

            _context.ReservasCupos.Add(reserva);
            await _context.SaveChangesAsync();
        }

        // Nuevo: Limpieza de Reservas Vencidas (ejecutable como Background Service)
        public async Task LimpiarReservasVencidas()
        {
            var reservasVencidas = await _context.ReservasCupos
                .Where(r => r.Activa && r.FechaHoraVencimiento <= DateTime.Now)
                .ToListAsync();

            if (reservasVencidas.Any())
            {
                foreach (var reserva in reservasVencidas)
                {
                    reserva.Activa = false;
                }
                _context.ReservasCupos.UpdateRange(reservasVencidas);
                await _context.SaveChangesAsync();
            }
        }

        // -------------------------------------------------------------------
        // REPORTES (Funcionario)
        // -------------------------------------------------------------------

        // Nuevo: Obtiene el total de ingresos y los pagos agrupados por vehículo
        public async Task<(decimal ingresosMes, List<ReporteVehiculo> reporteVehiculos)> ObtenerIngresosMes()
        {
            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finMes = inicioMes.AddMonths(1);

            var registrosMes = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .Where(r => r.FechaHoraSalida != null &&
                           r.FechaHoraSalida >= inicioMes &&
                           r.FechaHoraSalida < finMes)
                .ToListAsync();

            var ingresosMes = registrosMes.Sum(r => r.ValorPagado ?? 0);

            var reporteVehiculos = registrosMes
                .GroupBy(r => r.Vehiculo.Placa)
                .Select(g => new ReporteVehiculo
                {
                    Placa = g.Key,
                    TotalPagado = g.Sum(r => r.ValorPagado ?? 0)
                })
                .ToList();

            return (ingresosMes, reporteVehiculos);
        }
    }
}

