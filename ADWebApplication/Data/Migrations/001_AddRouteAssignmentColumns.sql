-- ============================================
-- Route Assignment Feature - Database Migration
-- Database: MySQL
-- ============================================

USE `in5nite-database`;

-- ============================================
-- CRITICAL MIGRATIONS (Required)
-- ============================================

-- 1. Add columns to routeassignment table
ALTER TABLE `routeassignment` 
ADD COLUMN `assignedDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'When the assignment was created',
ADD COLUMN `collectionDate` DATE NOT NULL COMMENT 'Scheduled collection day',
ADD COLUMN `status` VARCHAR(20) NOT NULL DEFAULT 'Pending' COMMENT 'Assignment status: Pending, Active, Completed';

-- 2. Add column to routeplan table
ALTER TABLE `routeplan` 
ADD COLUMN `routeName` VARCHAR(100) NOT NULL DEFAULT 'Unnamed Route' COMMENT 'Descriptive name for the route';

-- ============================================
-- OPTIONAL MIGRATION
-- ============================================

-- 3. Add status column to routestop table (optional - can use CollectionDetails.CollectionStatus instead)
-- ALTER TABLE `routestop` 
-- ADD COLUMN `status` VARCHAR(20) DEFAULT 'Pending' COMMENT 'Stop status: Pending, Collected, Skipped';

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

-- Verify routeassignment table structure
DESCRIBE `routeassignment`;

-- Verify routeplan table structure
DESCRIBE `routeplan`;

-- Verify routestop table structure (if optional migration was run)
-- DESCRIBE `routestop`;

-- ============================================
-- ROLLBACK SCRIPT (if needed)
-- ============================================

-- To rollback routeassignment changes:
-- ALTER TABLE `routeassignment` 
-- DROP COLUMN `assignedDate`,
-- DROP COLUMN `collectionDate`,
-- DROP COLUMN `status`;

-- To rollback routeplan changes:
-- ALTER TABLE `routeplan` 
-- DROP COLUMN `routeName`;

-- To rollback routestop changes (if optional migration was run):
-- ALTER TABLE `routestop` 
-- DROP COLUMN `status`;
