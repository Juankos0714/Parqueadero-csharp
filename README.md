# Sistema de Gestión de Parqueadero

## Configuración de la Base de Datos

### Paso 1: Crear la base de datos
1. Abre MySQL Workbench o tu cliente MySQL preferido
2. Ejecuta el script `database_script.sql` que se encuentra en la raíz del proyecto
3. Esto creará la base de datos `ParqueaderoDb` con todas las tablas necesarias y datos de prueba

### Paso 2: Configurar la conexión
1. Abre el archivo `MyApp/appsettings.json`
2. Modifica la cadena de conexión con tus credenciales de MySQL:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=ParqueaderoDb;user=TU_USUARIO;password=TU_PASSWORD;"
  }
}
```

### Paso 3: Ejecutar la aplicación
```bash
cd MyApp
dotnet run
```

## Funcionalidades del Sistema

- **Gestión de Usuarios**: Registro de aprendices y funcionarios
- **Gestión de Vehículos**: Registro de carros y motos
- **Control de Parqueo**: Entrada y salida con control de capacidad
- **Tarifas**: Sistema de cobro por horas según tipo y ubicación
- **Reportes**: Ingresos mensuales y estadísticas
- **Reservas**: Sistema de reserva de cupos por 30 minutos

## Estructura de la Base de Datos

- **Usuarios**: Información de aprendices y funcionarios
- **Vehiculos**: Registro de vehículos asociados a usuarios
- **RegistrosParqueo**: Control de entrada/salida y pagos
- **ReservasCupos**: Sistema de reservas temporales