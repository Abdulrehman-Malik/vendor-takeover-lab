-- "Hotfix" run by hand in SSMS by a DBA. It is NOT an EF Core migration,
-- so orders-api's migration history and C# model know nothing about this column.
-- scripts/plant-deviations.sh applies it to simulate the vendor's habit (exercise E9).
USE SalesDb;
GO

IF COL_LENGTH('orders.Orders', 'Priority') IS NULL
    ALTER TABLE orders.Orders ADD Priority INT NOT NULL CONSTRAINT DF_Orders_Priority DEFAULT (0);
GO
