-- RaceDay database schema and seed data
-- PROG6212 Part 1: System Planning and Database
-- Target: SQL Server / Azure SQL Database, run through SSMS
-- Matches RaceDay_ERD.png exactly: Roles, Users, Events, Categories, Enrolments, Results

-- Tables are dropped in reverse dependency order so the script can be re-run
-- cleanly against the same instance without manual cleanup first.
DROP TABLE IF EXISTS dbo.Results;

DROP TABLE IF EXISTS dbo.Enrolments;

DROP TABLE IF EXISTS dbo.Categories;

DROP TABLE IF EXISTS dbo.Events;

DROP TABLE IF EXISTS dbo.Users;

DROP TABLE IF EXISTS dbo.Roles;

-- 1. Roles
-- Lookup table for the two account types in the system. Keeping this separate
-- from Users, rather than a free-text column, is what enforces valid roles
-- at the database level and satisfies the six-entity minimum through a design
-- that actually models role-based access rather than padding the count.
CREATE TABLE dbo.Roles (
    RoleID   INT          IDENTITY (1, 1) PRIMARY KEY,
    RoleName VARCHAR (20) NOT NULL UNIQUE
);

-- 2. Users
-- Holds both Organisers and Participants. RoleID decides which one a given
-- row is, so the same table plays both sides of the relationships below
-- depending on context (OrganiserID on Events, ParticipantID on Enrolments).
CREATE TABLE dbo.Users (
    UserID            INT           IDENTITY (1, 1) PRIMARY KEY,
    RoleID            INT           NOT NULL,
    FullName          VARCHAR (100) NOT NULL,
    Email             VARCHAR (150) NOT NULL UNIQUE,
    PasswordHash      VARCHAR (255) NOT NULL,
    ProfilePictureUrl VARCHAR (255) NULL,
    CreatedAt         DATETIME      DEFAULT GETDATE() NOT NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES dbo.Roles (RoleID)
);

-- 3. Events
-- Created and owned by an Organiser. EventType is restricted to the three
-- categories the brief lists: run, walk, cycle.
CREATE TABLE dbo.Events (
    EventID        INT            IDENTITY (1, 1) PRIMARY KEY,
    OrganiserID    INT            NOT NULL,
    EventName      VARCHAR (150)  NOT NULL,
    Description    VARCHAR (1000) NULL,
    EventDate      DATETIME       NOT NULL,
    Location       VARCHAR (200)  NOT NULL,
    DistanceKm     DECIMAL (6, 2) NOT NULL,
    EventType      VARCHAR (20)   NOT NULL,
    BannerImageUrl VARCHAR (255)  NULL,
    CreatedAt      DATETIME       DEFAULT GETDATE() NOT NULL,
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID) REFERENCES dbo.Users (UserID),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);

-- 4. Categories
-- Age or distance categories, defined per event by its Organiser.
CREATE TABLE dbo.Categories (
    CategoryID   INT            IDENTITY (1, 1) PRIMARY KEY,
    EventID      INT            NOT NULL,
    CategoryName VARCHAR (100)  NOT NULL,
    MinAge       INT            NULL,
    MaxAge       INT            NULL,
    DistanceKm   DECIMAL (6, 2) NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID) REFERENCES dbo.Events (EventID)
);

-- 5. Enrolments
-- Links a Participant to one event under one category. The unique constraint
-- stops the same participant enrolling twice in the same event.
CREATE TABLE dbo.Enrolments (
    EnrolmentID   INT          IDENTITY (1, 1) PRIMARY KEY,
    ParticipantID INT          NOT NULL,
    EventID       INT          NOT NULL,
    CategoryID    INT          NOT NULL,
    EnrolmentDate DATETIME     DEFAULT GETDATE() NOT NULL,
    Status        VARCHAR (20) DEFAULT 'Confirmed' NOT NULL,
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID) REFERENCES dbo.Users (UserID),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventID) REFERENCES dbo.Events (EventID),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories (CategoryID),
    CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Pending', 'Cancelled')),
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantID, EventID)
);

-- 6. Results
-- One result per enrolment, captured by the Organiser after the event.
-- The unique constraint on EnrolmentID enforces that one-to-one relationship.
CREATE TABLE dbo.Results (
    ResultID          INT      IDENTITY (1, 1) PRIMARY KEY,
    EnrolmentID       INT      NOT NULL UNIQUE,
    FinishTime        TIME     NULL,
    FinishingPosition INT      NULL,
    CapturedAt        DATETIME NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments (EnrolmentID)
);

-- ============================================================
-- Seed data
-- ============================================================
INSERT INTO dbo.Roles (
    RoleName
)
VALUES
    ('Organiser'),
    ('Participant');

-- PasswordHash values below are placeholder strings only, shaped like a real
-- hash so the column and its length make sense. Part 2 generates the real
-- hashes through the API's registration endpoint, not through this script.
INSERT INTO dbo.Users (
    RoleID,
    FullName,
    Email,
    PasswordHash,
    ProfilePictureUrl
)
VALUES
    (1, 'Thabo Nkosi', 'thabo.nkosi@raceday.co.za', 'AQAAAAIAAYagAAAAEJ4H2m9pQe7Zx1yV3sT6wL8bR4cN0dK5', NULL),
    (1, 'Amanda van der Merwe', 'amanda.vdm@raceday.co.za', 'AQAAAAIAAYagAAAAEL6P3n0qRf8Ay2zW4tU7xM9cS5dO1eL6', NULL),
    (2, 'Sipho Dlamini', 'sipho.dlamini@raceday.co.za', 'AQAAAAIAAYagAAAAEK7Q4o1rSg9Bz3aX5uV8yN0dT6eP2fM7', NULL),
    (2, 'Lerato Mokoena', 'lerato.mokoena@raceday.co.za', 'AQAAAAIAAYagAAAAEM8R5p2sTh0Ca4bY6vW9zO1eU7fQ3gN8', NULL);

INSERT INTO dbo.Events (
    OrganiserID,
    EventName,
    Description,
    EventDate,
    Location,
    DistanceKm,
    EventType,
    BannerImageUrl
)
VALUES
    (1, 'Cape Town Coastal 10K', 'A flat, scenic 10km run along the Sea Point promenade.', '2026-06-15 07:00:00', 'Sea Point Promenade, Cape Town', 10.00, 'Run', NULL),
    (1, 'Joburg City Cycle Challenge', 'Road cycling event through Sandton and surrounds, with 42km and 21km routes.', '2026-11-08 06:30:00', 'Sandton, Johannesburg', 42.00, 'Cycle', NULL),
    (2, 'Durban Beachfront Walkathon', 'Family-friendly 5km walk along the Durban beachfront in support of local charities.', '2026-12-06 08:00:00', 'Durban Beachfront', 5.00, 'Walk', NULL);

INSERT INTO dbo.Categories (
    EventID,
    CategoryName,
    MinAge,
    MaxAge,
    DistanceKm
)
VALUES
    (1, 'Under 20', 10, 19, 10.00),
    (1, 'Open 20-39', 20, 39, 10.00),
    (1, 'Veteran 40+', 40, 99, 10.00),
    (2, '42km Open', NULL, NULL, 42.00),
    (2, '21km Open', NULL, NULL, 21.00),
    (3, 'Family Walk', NULL, NULL, 5.00),
    (3, 'Senior Walk 60+', 60, 120, 5.00);

-- CategoryID 2 = Open 20-39 (Event 1), CategoryID 5 = 21km Open (Event 2),
-- CategoryID 6 = Family Walk (Event 3), matching the inserts above in order.
INSERT INTO dbo.Enrolments (
    ParticipantID,
    EventID,
    CategoryID,
    Status
)
VALUES
    (3, 1, 2, 'Confirmed'),
    (4, 1, 2, 'Confirmed'),
    (3, 2, 5, 'Confirmed'),
    (4, 3, 6, 'Pending');

-- Results only exist for the Cape Town Coastal 10K (EventID 1), the one event
-- with a date already in the past. The other two events have not happened
-- yet, so their enrolments correctly have no matching row here.
INSERT INTO dbo.Results (
    EnrolmentID,
    FinishTime,
    FinishingPosition,
    CapturedAt
)
VALUES
    (1, '00:52:18', 47, '2026-06-15 10:15:00'),
    (2, '00:58:03', 112, '2026-06-15 10:20:00');
