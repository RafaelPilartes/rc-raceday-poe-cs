-- RaceDay database schema and seed data
-- PROG6212 Part 1: System Planning and Database
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