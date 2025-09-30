using MyApp.Data;
using MyApp.Models;
using Microsoft.EntityFrameworkCore;

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

        public async Task<string> IngresarVehiculo(int vehiculoId)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(vehiculoId);
            if (vehiculo == null) return "Vehículo no encontrado";

            var disponibilidad = await ObtenerDisponibilidad();
            string estado = "Fuera";

            if (vehiculo.Tipo == "Carro" && disponibilidad.carrosDentro < MAX_CARROS)
                estado = "Dentro";
            else if (vehiculo.Tipo == "Moto" && disponibilidad.motosDentro < MAX_MOTOS)
                estado = "Dentro";

            var registro = new RegistroParqueo
            {
                VehiculoId = vehiculoId,
                Estado = estado,
                FechaHoraEntrada = DateTime.Now
            };

            _context.RegistrosParqueo.Add(registro);
            await _context.SaveChangesAsync();

            return $"Vehículo ingresado {estado.ToLower()} del parqueadero";
        }

        public async Task<decimal> CalcularTarifa(int registroId)
        {
            var registro = await _context.RegistrosParqueo
                .Include(r => r.Vehiculo)
                .FirstOrDefaultAsync(r => r.Id == registroId);

            if (registro == null) return 0;

            var tiempoTranscurrido = DateTime.Now - registro.FechaHoraEntrada;
            var horas = Math.Ceiling(tiempoTranscurrido.TotalHours);

            decimal tarifa = 0;
            if (registro.Vehiculo.Tipo == "Carro")
                tarifa = registro.Estado == "Dentro" ? TARIFA_CARRO_DENTRO : TARIFA_CARRO_FUERA;
            else
                tarifa = registro.Estado == "Dentro" ? TARIFA_MOTO_DENTRO : TARIFA_MOTO_FUERA;

            return tarifa * (decimal)horas;
        }

        public async Task<bool> ReservarCupo(int vehiculoId)
        {
            var vehiculo = await _context.Vehiculos.FindAsync(vehiculoId);
            if (vehiculo == null) return false;

            var disponibilidad = await ObtenerDisponibilidad();
            
            // Verificar si hay cupo disponible
            bool hayCupo = (vehiculo.Tipo == "Carro" && disponibilidad.carrosDentro < MAX_CARROS) ||
                          (vehiculo.Tipo == "Moto" && disponibilidad.motosDentro < MAX_MOTOS);

            if (hayCupo) return false; // No necesita reserva

            var reserva = new ReservaCupo
            {
                VehiculoId = vehiculoId,
                FechaHoraVencimiento = DateTime.Now.AddMinutes(30)
            };

            _context.ReservasCupos.Add(reserva);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}