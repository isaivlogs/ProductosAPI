-- ============================================================================
--  productos.sql  —  Script de la base de datos (MySQL 8)
-- ============================================================================
--  La API ya crea todo esto sola al arrancar (Data/InicializadorDb.cs).
--  Este archivo sirve si prefieres crearla a mano en MySQL Workbench o con:
--      mysql -u root -p < Database/productos.sql
--  También lo usa compose.yaml para preparar el contenedor de MySQL.
-- ============================================================================

CREATE DATABASE IF NOT EXISTS productos_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE productos_db;

CREATE TABLE IF NOT EXISTS productos (
    id              INT            NOT NULL AUTO_INCREMENT,
    nombre          VARCHAR(100)   NOT NULL,
    descripcion     VARCHAR(500)   NOT NULL DEFAULT '',
    categoria       VARCHAR(50)    NOT NULL DEFAULT '',
    precio          DECIMAL(10,2)  NOT NULL DEFAULT 0,
    stock           INT            NOT NULL DEFAULT 0,
    activo          TINYINT(1)     NOT NULL DEFAULT 1,
    fecha_registro  DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
);

-- Datos de ejemplo (solo si la tabla está vacía)
INSERT INTO productos (nombre, descripcion, categoria, precio, stock, activo)
SELECT * FROM (
    SELECT 'Audífonos inalámbricos' AS nombre, 'Bluetooth 5.3 con cancelación de ruido' AS descripcion, 'Audio' AS categoria, 899.00 AS precio, 25 AS stock, 1 AS activo
    UNION ALL SELECT 'Teclado mecánico',    'Switches rojos, retroiluminado RGB', 'Periféricos', 1249.50, 12, 1
    UNION ALL SELECT 'Mouse gamer',         '16000 DPI, 7 botones programables',  'Periféricos',  549.90, 40, 1
    UNION ALL SELECT 'Monitor 27 pulgadas', 'IPS 2K a 165 Hz',                    'Pantallas',   5299.00,  6, 1
    UNION ALL SELECT 'Cable USB-C',         'Carga rápida 100 W, 2 metros',       'Accesorios',   189.00,  0, 0
) AS datos
WHERE NOT EXISTS (SELECT 1 FROM productos);

-- Consultas útiles para revisar desde Workbench
-- SELECT * FROM productos;
-- SELECT * FROM productos WHERE nombre LIKE '%mouse%';
