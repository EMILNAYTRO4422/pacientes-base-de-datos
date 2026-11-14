/*
    Modelo de datos: Sistema de Registro de Pacientes
    Motor: SQL Server
    Autor: Emil Gomez Rodriguez - 2026-0935

    Tablas:
      - TiposSangre       : catalogo de tipos de sangre (8 filas)
      - Pacientes         : datos del paciente. Relacion N a 1 con TiposSangre
      - AuditoriaPacientes: historial de cambios. Relacion N a 1 con Pacientes

    Decisiones de diseno:
      - Llaves primarias INT IDENTITY(1,1).
      - Cedula UNIQUE: no pueden existir dos pacientes con la misma cedula.
      - Borrado logico: Pacientes.Activo (1 = activo, 0 = eliminado). No se pierde el historial.
      - FechaCreacion / FechaModificacion con DEFAULT GETDATE().
      - Indices en los campos que mas se buscan (apellido/nombre y tipo de sangre).

    Ejecutar este script completo en SSMS ANTES de correr la aplicacion.
    ATENCION: si la base ya existe, el script borra y vuelve a crear las tablas.
*/

IF DB_ID(N'RegistroPacientesDB') IS NULL
    CREATE DATABASE RegistroPacientesDB;
GO

USE RegistroPacientesDB;
GO

-- =========================================================
-- 1. LIMPIEZA (primero las tablas hijas)
-- =========================================================
IF OBJECT_ID(N'dbo.AuditoriaPacientes', N'U') IS NOT NULL DROP TABLE dbo.AuditoriaPacientes;
IF OBJECT_ID(N'dbo.Pacientes',          N'U') IS NOT NULL DROP TABLE dbo.Pacientes;
IF OBJECT_ID(N'dbo.TiposSangre',        N'U') IS NOT NULL DROP TABLE dbo.TiposSangre;
GO

-- =========================================================
-- 2. TABLAS
-- =========================================================
CREATE TABLE dbo.TiposSangre (
    Id     INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(3)       NOT NULL,
    CONSTRAINT PK_TiposSangre PRIMARY KEY (Id),
    CONSTRAINT UQ_TiposSangre_Nombre UNIQUE (Nombre)
);

CREATE TABLE dbo.Pacientes (
    Id                INT IDENTITY(1,1) NOT NULL,
    Nombre            NVARCHAR(50)      NOT NULL,
    Apellido          NVARCHAR(50)      NOT NULL,
    Cedula            NVARCHAR(11)      NOT NULL,
    FechaNacimiento   DATE              NOT NULL,
    Telefono          NVARCHAR(10)      NOT NULL,
    TipoSangreId      INT               NOT NULL,
    Direccion         NVARCHAR(200)     NULL,
    Activo            BIT               NOT NULL CONSTRAINT DF_Pacientes_Activo DEFAULT 1,
    FechaCreacion     DATETIME          NOT NULL CONSTRAINT DF_Pacientes_FechaCreacion DEFAULT GETDATE(),
    FechaModificacion DATETIME          NOT NULL CONSTRAINT DF_Pacientes_FechaModificacion DEFAULT GETDATE(),
    CONSTRAINT PK_Pacientes PRIMARY KEY (Id),
    CONSTRAINT UQ_Pacientes_Cedula UNIQUE (Cedula),
    CONSTRAINT CK_Pacientes_Cedula CHECK (LEN(Cedula) = 11),
    CONSTRAINT CK_Pacientes_Telefono CHECK (LEN(Telefono) = 10),
    CONSTRAINT FK_Pacientes_TiposSangre FOREIGN KEY (TipoSangreId) REFERENCES dbo.TiposSangre (Id)
);

CREATE TABLE dbo.AuditoriaPacientes (
    Id         INT IDENTITY(1,1) NOT NULL,
    PacienteId INT               NOT NULL,
    Accion     NVARCHAR(20)      NOT NULL,
    Detalle    NVARCHAR(200)     NULL,
    Fecha      DATETIME          NOT NULL CONSTRAINT DF_Auditoria_Fecha DEFAULT GETDATE(),
    CONSTRAINT PK_AuditoriaPacientes PRIMARY KEY (Id),
    CONSTRAINT FK_Auditoria_Pacientes FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes (Id)
);
GO

-- =========================================================
-- 3. INDICES (campos que mas se usan en busquedas y uniones)
-- =========================================================
CREATE INDEX IX_Pacientes_Apellido_Nombre ON dbo.Pacientes (Apellido, Nombre);
CREATE INDEX IX_Pacientes_TipoSangreId    ON dbo.Pacientes (TipoSangreId);
CREATE INDEX IX_Auditoria_PacienteId      ON dbo.AuditoriaPacientes (PacienteId);
GO

-- =========================================================
-- 4. DATOS INICIALES
-- =========================================================
INSERT INTO dbo.TiposSangre (Nombre)
VALUES (N'A+'), (N'A-'), (N'B+'), (N'B-'), (N'AB+'), (N'AB-'), (N'O+'), (N'O-');

INSERT INTO dbo.Pacientes (Nombre, Apellido, Cedula, FechaNacimiento, Telefono, TipoSangreId, Direccion)
SELECT N'Ana', N'Perez', N'00112345678', '1990-03-15', N'8095551234', t.Id, N'Calle Duarte #12, Santo Domingo'
FROM dbo.TiposSangre AS t WHERE t.Nombre = N'O+'
UNION ALL
SELECT N'Luis', N'Gomez', N'00187654321', '2000-10-10', N'8295550000', t.Id, N'Av. Independencia #45, Santo Domingo'
FROM dbo.TiposSangre AS t WHERE t.Nombre = N'AB-'
UNION ALL
SELECT N'Maria', N'Rodriguez', N'40212345678', '1985-07-22', N'8495557788', t.Id, N'Calle Mella #8, Santiago'
FROM dbo.TiposSangre AS t WHERE t.Nombre = N'A+';
GO

-- =========================================================
-- 5. VERIFICACION
-- =========================================================
SELECT p.Id, p.Nombre, p.Apellido, p.Cedula, p.FechaNacimiento, p.Telefono, t.Nombre AS TipoSangre, p.Direccion, p.Activo
FROM dbo.Pacientes AS p
INNER JOIN dbo.TiposSangre AS t ON t.Id = p.TipoSangreId
ORDER BY p.Id;
GO
