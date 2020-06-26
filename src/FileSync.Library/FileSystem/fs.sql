--
-- File generated with SQLiteStudio v3.2.1 on Fri Jun 26 10:55:35 2020
--
-- Text encoding used: System
--
PRAGMA foreign_keys = off;
BEGIN TRANSACTION;

-- Table: files
DROP TABLE IF EXISTS files;

CREATE TABLE files (
    id            INTEGER  PRIMARY KEY AUTOINCREMENT,
    path          VARCHAR  UNIQUE,
    size          INT,
    last_modified DATETIME
);


-- Index: files_index_last_modified
DROP INDEX IF EXISTS files_index_last_modified;

CREATE INDEX files_index_last_modified ON files (
    last_modified
);


-- Index: files_index_path
DROP INDEX IF EXISTS files_index_path;

CREATE INDEX files_index_path ON files (
    path
);


COMMIT TRANSACTION;
PRAGMA foreign_keys = on;
