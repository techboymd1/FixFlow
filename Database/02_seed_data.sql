-- Development-only seed data for FixFlowDb.
-- Run 01_create_schema.sql first. These accounts all use ChangeMe123! and MUST be
-- changed or deleted before any real workshop use. They are intentionally not production defaults.
USE `FixFlowDb`;

INSERT INTO `Users` (`FullName`, `UserName`, `PasswordHash`, `Role`, `IsActive`, `CreatedAtUtc`)
SELECT 'FixFlow Owner', 'owner', '120000.Rml4Rmxvd0RldlNhbHQhIQ==.bl8alT5+hmVpfH/+SrQnvqUdhHkKCaLhtJ09Ew+TQLE=', 'Owner/Admin', 1, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Users` WHERE `UserName` = 'owner');

INSERT INTO `Users` (`FullName`, `UserName`, `PasswordHash`, `Role`, `IsActive`, `CreatedAtUtc`)
SELECT 'Front Desk User', 'frontdesk', '120000.Rml4Rmxvd0RldlNhbHQhIQ==.bl8alT5+hmVpfH/+SrQnvqUdhHkKCaLhtJ09Ew+TQLE=', 'Front Desk', 1, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Users` WHERE `UserName` = 'frontdesk');

INSERT INTO `Users` (`FullName`, `UserName`, `PasswordHash`, `Role`, `IsActive`, `CreatedAtUtc`)
SELECT 'Workshop Technician', 'technician', '120000.Rml4Rmxvd0RldlNhbHQhIQ==.bl8alT5+hmVpfH/+SrQnvqUdhHkKCaLhtJ09Ew+TQLE=', 'Technician', 1, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Users` WHERE `UserName` = 'technician');

INSERT INTO `Parts` (`Sku`, `Name`, `Description`, `StockQuantity`, `ReorderPoint`, `CostPrice`, `SellingPrice`, `IsActive`, `CreatedAtUtc`)
SELECT 'FF-FUSE-10A', '10A blade fuse', 'General workshop spare', 20, 5, 100.00, 250.00, 1, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Parts` WHERE `Sku` = 'FF-FUSE-10A');

INSERT INTO `Parts` (`Sku`, `Name`, `Description`, `StockQuantity`, `ReorderPoint`, `CostPrice`, `SellingPrice`, `IsActive`, `CreatedAtUtc`)
SELECT 'FF-CAP-1000', '1000uF capacitor', '35V electrolytic capacitor', 12, 5, 180.00, 450.00, 1, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Parts` WHERE `Sku` = 'FF-CAP-1000');

INSERT INTO `Parts` (`Sku`, `Name`, `Description`, `StockQuantity`, `ReorderPoint`, `CostPrice`, `SellingPrice`, `IsActive`, `CreatedAtUtc`)
SELECT 'FF-PLUG-13A', '13A plug top', 'Replacement plug top', 3, 5, 350.00, 700.00, 1, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Parts` WHERE `Sku` = 'FF-PLUG-13A');
