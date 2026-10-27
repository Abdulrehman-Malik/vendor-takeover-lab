-- SQL Agent job kept AS CODE in Git (the good practice).
-- Safe to re-run: it drops and recreates the job.
--   bash scripts/sql.sh -i db/agent-jobs/ArchiveOldOrders.sql
USE msdb;
GO

IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'ArchiveOldOrders')
    EXEC msdb.dbo.sp_delete_job @job_name = N'ArchiveOldOrders', @delete_unused_schedule = 1;
GO

EXEC msdb.dbo.sp_add_job
     @job_name = N'ArchiveOldOrders',
     @description = N'Deletes orders older than 2 years. Source: db/agent-jobs/ArchiveOldOrders.sql';

EXEC msdb.dbo.sp_add_jobstep
     @job_name = N'ArchiveOldOrders',
     @step_name = N'Delete old orders',
     @subsystem = N'TSQL',
     @database_name = N'SalesDb',
     @command = N'DELETE FROM orders.Orders WHERE CreatedAt < DATEADD(year, -2, SYSUTCDATETIME());';

-- Every 5 minutes, so you can see it run during the lab. A real job would run nightly.
EXEC msdb.dbo.sp_add_jobschedule
     @job_name = N'ArchiveOldOrders',
     @name = N'Every5Minutes',
     @freq_type = 4,              -- daily
     @freq_interval = 1,
     @freq_subday_type = 4,       -- unit: minutes
     @freq_subday_interval = 5;

EXEC msdb.dbo.sp_add_jobserver @job_name = N'ArchiveOldOrders';
GO

SELECT name, enabled, date_created FROM msdb.dbo.sysjobs WHERE name = N'ArchiveOldOrders';
GO
