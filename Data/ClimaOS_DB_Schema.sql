-- ============================================================
--  ClimaOS_DB — Script complet de creare a bazei de date
--  Versiune: 2.1 (structurată și normalizată, validată MySQL)
--  Autor: ClimaOS Team
-- ============================================================

CREATE DATABASE IF NOT EXISTS ClimaOS_DB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE ClimaOS_DB;

-- Curățăm tabelele vechi pentru a aplica noua structură (atenție: șterge datele existente!)
-- Ordinea este importantă din cauza Foreign Keys
DROP TABLE IF EXISTS SystemLogs;
DROP TABLE IF EXISTS WeatherHistory;
DROP TABLE IF EXISTS WeatherAlerts;
DROP TABLE IF EXISTS UserFavorites;
DROP TABLE IF EXISTS Locations;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Roles;

-- ============================================================
-- 2. MODULUL DE UTILIZATORI ȘI AUTENTIFICARE
-- ============================================================

CREATE TABLE Roles (
    RoleId   INT AUTO_INCREMENT PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);

INSERT INTO Roles (RoleName) VALUES ('User'), ('Admin');

CREATE TABLE Users (
    UserId       INT AUTO_INCREMENT PRIMARY KEY,
    RoleId       INT          NOT NULL DEFAULT 1,
    FullName     VARCHAR(100) NOT NULL,
    Email        VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(256) NOT NULL,
    IsActive     BOOLEAN      NOT NULL DEFAULT TRUE,
    CreatedAt    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt  DATETIME     NULL,
    CONSTRAINT fk_users_role FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId) ON DELETE RESTRICT ON UPDATE CASCADE,
    INDEX idx_users_email (Email)
);

-- ============================================================
-- 3. MODULUL DE LOCAȚII
-- ============================================================

CREATE TABLE Locations (
    LocationId  INT AUTO_INCREMENT PRIMARY KEY,
    CityName    VARCHAR(100)   NOT NULL,
    CountryCode VARCHAR(10)    NOT NULL DEFAULT 'MD',
    Latitude    DECIMAL(9, 6)  NULL,
    Longitude   DECIMAL(9, 6)  NULL,
    INDEX idx_locations_city (CityName)
);

CREATE TABLE UserFavorites (
    FavoriteId INT AUTO_INCREMENT PRIMARY KEY,
    UserId     INT      NOT NULL,
    LocationId INT      NOT NULL,
    AddedAt    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_fav_user     FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT fk_fav_location FOREIGN KEY (LocationId)
        REFERENCES Locations(LocationId) ON DELETE CASCADE,
    CONSTRAINT uq_user_location UNIQUE (UserId, LocationId)
);

-- ============================================================
-- 4. MODULUL DE DATE METEO
-- ============================================================

CREATE TABLE WeatherHistory (
    HistoryId     INT AUTO_INCREMENT PRIMARY KEY,
    LocationId    INT          NOT NULL,
    RequestedBy   INT          NULL,
    Temperature   DECIMAL(5,2) NULL,
    FeelsLike     DECIMAL(5,2) NULL,
    TempMin       DECIMAL(5,2) NULL,
    TempMax       DECIMAL(5,2) NULL,
    Humidity      INT          NULL,
    WindSpeed     DECIMAL(6,2) NULL,
    Pressure      INT          NULL,
    ConditionName VARCHAR(100) NULL,
    RecordedAt    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_wh_location FOREIGN KEY (LocationId)
        REFERENCES Locations(LocationId) ON DELETE CASCADE,
    CONSTRAINT fk_wh_user     FOREIGN KEY (RequestedBy)
        REFERENCES Users(UserId) ON DELETE SET NULL,
    INDEX idx_wh_location_date (LocationId, RecordedAt)
);

-- ============================================================
-- 5. MODULUL DE ALERTE METEO
-- ============================================================

CREATE TABLE WeatherAlerts (
    AlertId    INT AUTO_INCREMENT PRIMARY KEY,
    Title      VARCHAR(200) NOT NULL,
    Message    TEXT         NULL,
    LocationId INT          NULL,
    Severity   VARCHAR(50)  NOT NULL DEFAULT 'Informare'
                CHECK (Severity IN ('Informare', 'Avertisment', 'Sever', 'Extrem')),
    StartsAt   DATETIME     NOT NULL,
    EndsAt     DATETIME     NOT NULL,
    CreatedAt  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_alert_location FOREIGN KEY (LocationId)
        REFERENCES Locations(LocationId) ON DELETE SET NULL
);

-- ============================================================
-- 6. MODULUL DE AUDIT — JURNALE DE SISTEM
-- ============================================================

CREATE TABLE SystemLogs (
    LogId          INT AUTO_INCREMENT PRIMARY KEY,
    UserId         INT          NULL,
    ActionType     VARCHAR(100) NOT NULL,
    Status         VARCHAR(20)  NOT NULL
                    CHECK (Status IN ('succes', 'eroare')),
    ErrorMessage   TEXT         NULL,
    ResponseTimeMs INT          NULL,
    LogDate        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_log_user FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE SET NULL,
    INDEX idx_logs_date (LogDate),
    INDEX idx_logs_status (Status)
);

-- ============================================================
-- 7. DATE INIȚIALE (SEED DATA)
-- ============================================================

INSERT INTO Users (RoleId, FullName, Email, PasswordHash)
VALUES
    (2, 'Admin Principal',  'admin@climaos.com', 'hashed_admin_pass'),
    (1, 'Ion Popescu',      'ion@exemplu.com',   'hashed_password_123');

INSERT INTO Locations (CityName, CountryCode, Latitude, Longitude)
VALUES
    ('Chișinău', 'MD', 47.005270, 28.857500),
    ('Bălți',    'MD', 47.762900, 27.929800),
    ('Cahul',    'MD', 45.907200, 28.190600),
    ('Ungheni',  'MD', 47.211100, 27.799600);

INSERT INTO UserFavorites (UserId, LocationId) VALUES (2, 1), (2, 2);

INSERT INTO SystemLogs (UserId, ActionType, Status, ResponseTimeMs)
VALUES
    (1, 'API_WEATHER_FETCH', 'succes', 124),
    (2, 'API_WEATHER_FETCH', 'succes',  98),
    (2, 'API_WEATHER_FETCH', 'eroare', 450);
