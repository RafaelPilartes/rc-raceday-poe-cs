# RaceDay API Endpoint Plan

Part 1, Section B. Every non-public endpoint depends on an active server-side session created at login, which stores the authenticated user's ID and role. "Owner" in the Role Required column means the endpoint checks that the resource (event or category) belongs to the logged-in Organiser, not just that the caller holds the Organiser role.

## Authentication

| HTTP Method | Route              | Description                                                                    | Role Required       | Request Body                        | Expected Response                                                                                                     |
| ----------- | ------------------ | ------------------------------------------------------------------------------ | ------------------- | ----------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| POST        | /api/auth/register | Creates a new account and assigns the selected role, Organiser or Participant. | None (public)       | { fullName, email, password, role } | 201 Created with the new user id. 400 Bad Request for invalid input. 409 Conflict if the email is already registered. |
| POST        | /api/auth/login    | Verifies credentials and starts a session for the authenticated user.          | None (public)       | { email, password }                 | 200 OK with the user's role and a confirmed session. 401 Unauthorized for invalid credentials.                        |
| POST        | /api/auth/logout   | Ends the current session.                                                      | Any (authenticated) | None                                | 200 OK. 401 Unauthorized if no session is active.                                                                     |

## User Profile

| HTTP Method | Route        | Description                                                          | Role Required       | Request Body                    | Expected Response                                                   |
| ----------- | ------------ | -------------------------------------------------------------------- | ------------------- | ------------------------------- | ------------------------------------------------------------------- |
| GET         | /api/profile | Returns the logged-in user's own profile.                            | Any (authenticated) | None                            | 200 OK with profile data. 401 Unauthorized.                         |
| PUT         | /api/profile | Updates the logged-in user's profile, including the profile picture. | Any (authenticated) | { fullName, profilePictureUrl } | 200 OK with the updated profile. 400 Bad Request. 401 Unauthorized. |
