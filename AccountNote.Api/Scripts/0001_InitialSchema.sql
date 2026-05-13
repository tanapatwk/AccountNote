-- Migration: 0001_InitialSchema.sql

CREATE TABLE IF NOT EXISTS AccountType (
   Id        INTEGER PRIMARY KEY AUTOINCREMENT,
   IsPaid    INTEGER NOT NULL DEFAULT 1,  -- 1=รายจ่าย, 0=รายรับ
   Title     TEXT    NOT NULL,
   CreatedAt TEXT    NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS AccountChannel (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Title       TEXT NOT NULL,
    Description TEXT,
    CreatedAt   TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Transactions (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    AccDate     TEXT    NOT NULL CHECK(AccDate GLOB '????-??-??'),
    AccTypeId   INTEGER NOT NULL,
    AccChId     INTEGER NOT NULL,
    Description TEXT    NOT NULL,
    Amount      REAL    NOT NULL CHECK(Amount > 0),
    Remark      TEXT,
    CreatedAt   TEXT    NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (AccTypeId) REFERENCES AccountType(Id),
    FOREIGN KEY (AccChId)   REFERENCES AccountChannel(Id)
);

-- Index สำหรับ FK columns ที่ถูก query บ่อย
CREATE INDEX IF NOT EXISTS idx_transactions_type    ON Transactions(AccTypeId);
CREATE INDEX IF NOT EXISTS idx_transactions_channel ON Transactions(AccChId);
CREATE INDEX IF NOT EXISTS idx_transactions_date    ON Transactions(AccDate);