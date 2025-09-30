-- Script para crear la base de datos del Sistema de Parqueadero
-- Ejecutar en MySQL

-- Crear la base de datos
CREATE DATABASE IF NOT EXISTS ParqueaderoDb;
USE ParqueaderoDb;

-- Tabla de Usuarios
CREATE TABLE IF NOT EXISTS Usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    Rol VARCHAR(50) NOT NULL DEFAULT 'Aprendiz',
    FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabla de Vehículos
CREATE TABLE IF NOT EXISTS Vehiculos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Placa VARCHAR(10) NOT NULL UNIQUE,
    Tipo VARCHAR(50) NOT NULL,
    Marca VARCHAR(100) NOT NULL,
    Modelo VARCHAR(100) NOT NULL,
    UsuarioId INT NOT NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE CASCADE
);

-- Tabla de Registros de Parqueo
CREATE TABLE IF NOT EXISTS RegistrosParqueo (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    VehiculoId INT NOT NULL,
    FechaHoraEntrada DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaHoraSalida DATETIME NULL,
    Estado VARCHAR(50) NOT NULL DEFAULT 'Dentro',
    ValorPagado DECIMAL(10,2) NULL,
    TiempoMinutos INT NOT NULL DEFAULT 0,
    FOREIGN KEY (VehiculoId) REFERENCES Vehiculos(Id) ON DELETE CASCADE
);

-- Tabla de Reservas de Cupos
CREATE TABLE IF NOT EXISTS ReservasCupos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    VehiculoId INT NOT NULL,
    FechaHoraReserva DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaHoraVencimiento DATETIME NOT NULL,
    Activa BOOLEAN NOT NULL DEFAULT TRUE,
    FOREIGN KEY (VehiculoId) REFERENCES Vehiculos(Id) ON DELETE CASCADE
);

-- Insertar datos de prueba
INSERT INTO Usuarios (Nombre, Email, Rol) VALUES 
('Juan Pérez', 'juan.perez@sena.edu.co', 'Aprendiz'),
('María García', 'maria.garcia@sena.edu.co', 'Funcionario'),
('Carlos López', 'carlos.lopez@sena.edu.co', 'Aprendiz'),
('Ana Rodríguez', 'ana.rodriguez@sena.edu.co', 'Aprendiz');

INSERT INTO Vehiculos (Placa, Tipo, Marca, Modelo, UsuarioId) VALUES 
('ABC123', 'Carro', 'Toyota', 'Corolla', 1),
('XYZ789', 'Moto', 'Honda', 'CB150', 3),
('DEF456', 'Carro', 'Chevrolet', 'Spark', 4);

-- Verificar que las tablas se crearon correctamente
SHOW TABLES;

-- Verificar datos insertados
SELECT 'Usuarios' as Tabla, COUNT(*) as Registros FROM Usuarios
UNION ALL
SELECT 'Vehiculos' as Tabla, COUNT(*) as Registros FROM Vehiculos
UNION ALL
SELECT 'RegistrosParqueo' as Tabla, COUNT(*) as Registros FROM RegistrosParqueo
UNION ALL
SELECT 'ReservasCupos' as Tabla, COUNT(*) as Registros FROM ReservasCupos;