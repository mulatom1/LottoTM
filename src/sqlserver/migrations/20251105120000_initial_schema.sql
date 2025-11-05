-- =====================================================================================
-- migration: 20251105120000_initial_schema.sql
-- description: initial database schema for lottotm mvp
-- version: 2.1
-- date: 2025-11-05
-- database: sql server 2022
-- orm: entity framework core 9
--
-- affected tables:
--   - Users (create)
--   - Tickets (create)
--   - TicketNumbers (create)
--   - Draws (create)
--   - DrawNumbers (create)
--
-- special notes:
--   - normalized structure: separate tables for ticket/draw numbers
--   - int identity used for all primary keys for simplicity
--   - cascade delete configured for all foreign keys
--   - utc timestamps used throughout (getutcdate())
--   - isadmin flag added to users for access control
--   - createdbyuserid tracking added to draws table
-- =====================================================================================

begin transaction;

-- =====================================================================================
-- table: Users
-- description: stores user authentication and profile data
--              each user has unique email, hashed password, and admin flag
-- =====================================================================================

create table Users (
    Id int primary key identity(1,1),
    Email nvarchar(255) not null,
    PasswordHash nvarchar(255) not null,
    IsAdmin bit not null default 0,
    CreatedAt datetime2 not null default getutcdate(),
    constraint UQ_Users_Email unique (Email)
);

-- index for fast user lookup during login (o(log n) performance)
create index IX_Users_Email on Users(Email);

-- =====================================================================================
-- table: Tickets
-- description: metadata for user ticket sets (the numbers are in TicketNumbers table)
--              int identity used for id (simple autoincrement primary key)
--              each user can have maximum 100 tickets (validated in backend)
-- security note: always filter by userid from jwt token to prevent unauthorized access
-- =====================================================================================

create table Tickets (
    Id int primary key identity(1,1),
    UserId int not null,
    CreatedAt datetime2 not null default getutcdate(),
    constraint FK_Tickets_Users foreign key (UserId) 
        references Users(Id) on delete cascade
);

-- index for filtering tickets by user (critical for performance with 100 tickets/user)
create index IX_Tickets_UserId on Tickets(UserId);

-- =====================================================================================
-- table: TicketNumbers
-- description: stores individual numbers for each ticket (normalized structure)
--              each ticket has exactly 6 numbers in positions 1-6
--              numbers must be in range 1-49
--              position uniqueness enforced per ticket
-- implementation notes:
--   - creating ticket requires transaction: 1 ticket + 6 ticketnumbers
--   - use ef core .include(t => t.numbers) for eager loading
--   - number uniqueness within ticket validated in backend
--   - duplicate ticket detection validated in backend (order-independent)
-- =====================================================================================

create table TicketNumbers (
    Id int primary key identity(1,1),
    TicketId int not null,
    Number int not null,
    Position tinyint not null,
    constraint FK_TicketNumbers_Tickets foreign key (TicketId) 
        references Tickets(Id) on delete cascade,
    constraint CHK_TicketNumbers_Number check (Number between 1 and 49),
    constraint CHK_TicketNumbers_Position check (Position between 1 and 6),
    constraint UQ_TicketNumbers_TicketPosition unique (TicketId, Position)
);

-- index for join optimization when loading ticket with numbers
create index IX_TicketNumbers_TicketId on TicketNumbers(TicketId);

-- index for queries like "find all tickets containing number 7"
-- useful for statistics and post-mvp features
create index IX_TicketNumbers_Number on TicketNumbers(Number);

-- =====================================================================================
-- table: Draws
-- description: global registry of lotto draw results (shared by all users)
--              each draw date is unique (one draw per day)
--              draws are created by users with isadmin = true
--              createdbyuserid tracks which admin entered the result
-- access control:
--   - read: all users (for verification)
--   - write: admin users only (backend validation required)
-- =====================================================================================

create table Draws (
    Id int primary key identity(1,1),
    DrawDate date not null,
    CreatedAt datetime2 not null default getutcdate(),
    CreatedByUserId int not null,
    constraint UQ_Draws_DrawDate unique (DrawDate),
    constraint FK_Draws_Users foreign key (CreatedByUserId) 
        references Users(Id) on delete cascade
);

-- note: UQ_Draws_DrawDate constraint automatically creates unique index for date range queries
-- no need for explicit index creation

-- index for tracking queries (who created this draw?)
-- also supports cascade delete performance
create index IX_Draws_CreatedByUserId on Draws(CreatedByUserId);

-- =====================================================================================
-- table: DrawNumbers
-- description: stores individual numbers for each draw (normalized structure)
--              each draw has exactly 6 numbers in positions 1-6
--              numbers must be in range 1-49
--              position uniqueness enforced per draw
-- implementation notes:
--   - creating draw requires transaction: 1 draw + 6 drawnumbers
--   - use ef core .include(d => d.numbers) for eager loading
--   - number uniqueness within draw validated in backend
-- =====================================================================================

create table DrawNumbers (
    Id int primary key identity(1,1),
    DrawId int not null,
    Number int not null,
    Position tinyint not null,
    constraint FK_DrawNumbers_Draws foreign key (DrawId) 
        references Draws(Id) on delete cascade,
    constraint CHK_DrawNumbers_Number check (Number between 1 and 49),
    constraint CHK_DrawNumbers_Position check (Position between 1 and 6),
    constraint UQ_DrawNumbers_DrawPosition unique (DrawId, Position)
);

-- index for join optimization when loading draw with numbers
create index IX_DrawNumbers_DrawId on DrawNumbers(DrawId);

-- index for queries like "find all draws containing number 7"
-- useful for statistics and analysis
create index IX_DrawNumbers_Number on DrawNumbers(Number);

-- =====================================================================================
-- commit transaction
-- all tables, constraints, and indexes created successfully
-- =====================================================================================

commit transaction;

-- =====================================================================================
-- verification queries (optional - for testing after migration)
-- =====================================================================================

-- verify all tables were created
-- select name from sys.tables where name in ('Users', 'Tickets', 'TicketNumbers', 'Draws', 'DrawNumbers');

-- verify all indexes were created
-- select t.name as tablename, i.name as indexname, i.type_desc
-- from sys.indexes i
-- inner join sys.tables t on i.object_id = t.object_id
-- where t.name in ('Users', 'Tickets', 'TicketNumbers', 'Draws', 'DrawNumbers')
-- order by t.name, i.name;

-- verify all foreign keys were created
-- select 
--     fk.name as foreignkey,
--     tp.name as parenttable,
--     tr.name as referencedtable
-- from sys.foreign_keys fk
-- inner join sys.tables tp on fk.parent_object_id = tp.object_id
-- inner join sys.tables tr on fk.referenced_object_id = tr.object_id
-- where tp.name in ('Tickets', 'TicketNumbers', 'Draws', 'DrawNumbers')
-- order by tp.name;

-- =====================================================================================
-- end of migration
-- =====================================================================================
