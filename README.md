# RaceDay — Event Management System

RaceDay is a web-based event management platform built for the South African road running, walking, and cycling community. It replaces the paper registration sheets and disconnected spreadsheets that most local road events still rely on. Event Organisers use it to publish events, set up age or distance categories, and capture results. Participants use it to browse events, enter the category of their choice, and track their own race history over time.

This repository covers the three parts of the PROG6212 Portfolio of Evidence. Part 1, documented below, is the planning stage: an ERD, a full API endpoint plan, and a SQL script that builds and seeds the database. No application code is written yet. Parts 2 and 3 build on this plan directly, so the schema here is what the API in Part 2 is coded against.

## Roles

**Organiser** — creates, edits, and deletes events, defines the age or distance categories for each event, views who has enrolled, and captures finish times and positions once an event has taken place.

**Participant** — browses upcoming events, enrols in an event by picking a category, and can see their own enrolments and results at any time.

Both roles are stored in the same `Users` table and distinguished by a `RoleID` foreign key into a separate `Roles` table, rather than a free-text field. That decision is explained in more depth in the endpoint plan and the schema comments.

## Repository structure (Part 1)

```
/docs
  RaceDay_ERD.png              Entity relationship diagram, six entities, all keys and cardinalities marked
  RaceDay_API_Endpoint_Plan.md API endpoint plan, one row per endpoint across all six functional areas
  raceday_schema.sql           Full T-SQL schema plus seed data
```

## Running the SQL script

1. Open SQL Server Management Studio and connect to the target instance (an Azure SQL Database or a local SQL Server both work).
2. Open `raceday_schema.sql`.
3. Execute the whole script. It drops the six tables if they already exist, in dependency order, then recreates them and inserts the seed data.
4. Confirm the seed data with a quick check, for example `SELECT * FROM dbo.Events;` should return three rows.

The script has to run without errors on a clean instance before submission. Test it on your own Azure SQL Database before recording the video.

## Continuous Integration

A GitHub Actions workflow runs on every push and checks that Part 1 is complete: the `/docs` folder contains the ERD image, the endpoint plan, and the SQL script; the endpoint plan covers all six functional areas; and the SQL script defines all six tables with their seed data.

![Successful Part 1 validation build](docs/ci-green.png)

## Video walkthrough

`[The unlisted YouTube link here.]`

The video covers the reasoning behind the ERD, including why roles are normalised into their own table, the choices made in the endpoint plan, and a live run of `raceday_schema.sql` in SSMS.

## AI tools disclosure

Gimini was used during Part 1 to help review the ERD, and to write the GitHub Actions validation workflow in `.github/workflows/part1-validate.yml`. The SQL script was tested manually in SSMS against an Azure SQL Database.
