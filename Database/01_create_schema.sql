-- FixFlow schema only. This script creates FixFlowDb if it is absent.
-- It never drops, alters, or switches to any other database.
CREATE DATABASE IF NOT EXISTS `FixFlowDb`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `FixFlowDb`;

CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `FullName` VARCHAR(100) NOT NULL,
    `UserName` VARCHAR(80) NOT NULL,
    `PasswordHash` VARCHAR(255) NOT NULL,
    `Role` VARCHAR(30) NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_Users_UserName` (`UserName`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Customers` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `FullName` VARCHAR(120) NOT NULL,
    `PhoneNumber` VARCHAR(30) NULL,
    `Email` VARCHAR(120) NULL,
    `Address` VARCHAR(250) NULL,
    `Notes` VARCHAR(500) NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Customers_FullName` (`FullName`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Equipment` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `CustomerId` INT NOT NULL,
    `Category` VARCHAR(50) NOT NULL,
    `Brand` VARCHAR(80) NULL,
    `Model` VARCHAR(100) NULL,
    `SerialNumber` VARCHAR(100) NULL,
    `Capacity` VARCHAR(80) NULL,
    `Notes` VARCHAR(500) NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Equipment_CustomerId_SerialNumber` (`CustomerId`, `SerialNumber`),
    CONSTRAINT `FK_Equipment_Customers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `RepairJobs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `JobReference` VARCHAR(30) NOT NULL,
    `CustomerId` INT NOT NULL,
    `EquipmentId` INT NOT NULL,
    `TechnicianId` INT NULL,
    `ReceiveDate` DATE NOT NULL,
    `ReportedFault` VARCHAR(1500) NOT NULL,
    `ConditionAndAccessories` VARCHAR(1500) NULL,
    `ExpectedCompletionDate` DATE NULL,
    `Status` VARCHAR(30) NOT NULL,
    `InternalNotes` VARCHAR(1000) NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    `UpdatedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_RepairJobs_JobReference` (`JobReference`),
    KEY `IX_RepairJobs_Status_ReceiveDate` (`Status`, `ReceiveDate`),
    KEY `IX_RepairJobs_CustomerId` (`CustomerId`),
    KEY `IX_RepairJobs_EquipmentId` (`EquipmentId`),
    KEY `IX_RepairJobs_TechnicianId` (`TechnicianId`),
    CONSTRAINT `FK_RepairJobs_Customers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RepairJobs_Equipment_EquipmentId` FOREIGN KEY (`EquipmentId`) REFERENCES `Equipment` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RepairJobs_Users_TechnicianId` FOREIGN KEY (`TechnicianId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `RepairEstimates` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RepairJobId` INT NOT NULL,
    `PartsAmount` DECIMAL(18,2) NOT NULL DEFAULT 0,
    `LabourAmount` DECIMAL(18,2) NOT NULL DEFAULT 0,
    `Description` VARCHAR(1200) NULL,
    `ApprovalStatus` VARCHAR(20) NOT NULL,
    `RespondedAtUtc` DATETIME(6) NULL,
    `ResponseNotes` VARCHAR(500) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_RepairEstimates_RepairJobId` (`RepairJobId`),
    CONSTRAINT `FK_RepairEstimates_RepairJobs_RepairJobId` FOREIGN KEY (`RepairJobId`) REFERENCES `RepairJobs` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `JobNotes` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RepairJobId` INT NOT NULL,
    `CreatedByUserId` INT NOT NULL,
    `NoteText` VARCHAR(2000) NOT NULL,
    `IsDiagnosis` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_JobNotes_RepairJobId` (`RepairJobId`),
    CONSTRAINT `FK_JobNotes_RepairJobs_RepairJobId` FOREIGN KEY (`RepairJobId`) REFERENCES `RepairJobs` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_JobNotes_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Parts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Sku` VARCHAR(50) NOT NULL,
    `Name` VARCHAR(150) NOT NULL,
    `Description` VARCHAR(500) NULL,
    `StockQuantity` INT NOT NULL DEFAULT 0,
    `ReorderPoint` INT NOT NULL DEFAULT 0,
    `CostPrice` DECIMAL(18,2) NOT NULL DEFAULT 0,
    `SellingPrice` DECIMAL(18,2) NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_Parts_Sku` (`Sku`)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `JobParts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RepairJobId` INT NOT NULL,
    `PartId` INT NOT NULL,
    `Quantity` INT NOT NULL,
    `UnitPrice` DECIMAL(18,2) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_JobParts_RepairJobId_PartId` (`RepairJobId`, `PartId`),
    CONSTRAINT `FK_JobParts_RepairJobs_RepairJobId` FOREIGN KEY (`RepairJobId`) REFERENCES `RepairJobs` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_JobParts_Parts_PartId` FOREIGN KEY (`PartId`) REFERENCES `Parts` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `StockMovements` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `PartId` INT NOT NULL,
    `QuantityChange` INT NOT NULL,
    `MovementType` VARCHAR(30) NOT NULL,
    `Reason` VARCHAR(300) NULL,
    `RepairJobId` INT NULL,
    `CreatedByUserId` INT NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_StockMovements_PartId` (`PartId`),
    KEY `IX_StockMovements_RepairJobId` (`RepairJobId`),
    KEY `IX_StockMovements_CreatedByUserId` (`CreatedByUserId`),
    CONSTRAINT `FK_StockMovements_Parts_PartId`
        FOREIGN KEY (`PartId`) REFERENCES `Parts` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_StockMovements_RepairJobs_RepairJobId`
        FOREIGN KEY (`RepairJobId`) REFERENCES `RepairJobs` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_StockMovements_Users_CreatedByUserId`
        FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `Payments` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RepairJobId` INT NOT NULL,
    `Amount` DECIMAL(18,2) NOT NULL,
    `PaymentMethod` VARCHAR(30) NOT NULL,
    `ReceiptNumber` VARCHAR(80) NOT NULL,
    `Notes` VARCHAR(250) NULL,
    `PaidAtUtc` DATETIME(6) NOT NULL,
    `ReceivedByUserId` INT NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_Payments_ReceiptNumber` (`ReceiptNumber`),
    KEY `IX_Payments_RepairJobId` (`RepairJobId`),
    CONSTRAINT `FK_Payments_RepairJobs_RepairJobId` FOREIGN KEY (`RepairJobId`) REFERENCES `RepairJobs` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Payments_Users_ReceivedByUserId` FOREIGN KEY (`ReceivedByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS `JobStatusHistories` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RepairJobId` INT NOT NULL,
    `FromStatus` VARCHAR(30) NOT NULL,
    `ToStatus` VARCHAR(30) NOT NULL,
    `ChangedByUserId` INT NOT NULL,
    `Notes` VARCHAR(500) NULL,
    `ChangedAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_JobStatusHistories_RepairJobId` (`RepairJobId`),
    CONSTRAINT `FK_JobStatusHistories_RepairJobs_RepairJobId` FOREIGN KEY (`RepairJobId`) REFERENCES `RepairJobs` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_JobStatusHistories_Users_ChangedByUserId` FOREIGN KEY (`ChangedByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB;
