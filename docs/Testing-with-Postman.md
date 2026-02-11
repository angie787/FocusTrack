# Testing FocusTrack with Postman (Backend Only)

This guide walks through testing all functionalities using Postman, Visual Studio, and Docker. The backend has no frontend; the Gateway accepts **Bearer tokens** from Keycloak so you can call APIs from Postman.

---

## 1. Start the stack

### Visual Studio + Docker (your setup)

**Step 1 – Start infrastructure with Docker**

From the repo root in a terminal:

```bash
docker compose up -d keycloak postgres-sessions postgres-rewards rabbitmq jaeger
```

Wait until Keycloak is up (e.g. open http://localhost:9080). Leave **gateway**, **session-api**, **reward-worker**, **notification-api** **not** started in Docker so you can run them from Visual Studio.

**Step 2 – Run the .NET apps from Visual Studio**

1. In Visual Studio: **Solution** → right‑click → **Configure Startup Projects** → **Multiple startup projects**.
2. Set **FocusTrack.Gateway.Api** and **FocusTrack.Session.Api** to **Start** (and optionally **FocusTrack.Notification.Api**, **FocusTrack.RewardWorker** if you want them).
3. Start with **F5** or **Start**. The Gateway uses **Development** settings and talks to:
   - **Keycloak:** http://localhost:9080 (from Docker)
   - **Session API:** http://localhost:5115 (from VS)

**URLs for Postman (Visual Studio + Docker):**

| Service     | URL                      |
|------------|---------------------------|
| **Gateway** (use this as `base_url`) | http://localhost:5029 |
| **Session API** (direct, optional)  | http://localhost:5115 |
| **Keycloak**                        | http://localhost:9080 |

If you use the **https** profile for the Gateway, use the HTTPS URL from launchSettings (e.g. https://localhost:7262). For simplicity, use the **http** profile so `base_url` = **http://localhost:5029**.

---

### Alternative: full stack in Docker

From the repo root:

```bash
docker compose up -d
```

Then:

- **Gateway:** http://localhost:5000  
- **Keycloak:** http://localhost:9080  
- **Session API (direct):** http://localhost:5001  

Use **http://localhost:5000** as `base_url` in Postman.

---

## 2. Get an access token from Keycloak

The Gateway expects a **Bearer token** from Keycloak. Get one using the token endpoint.

### Enable Direct Access Grant (Resource Owner Password) in Keycloak

1. Open **Keycloak Admin Console:** http://localhost:9080 (user: `admin`, password: `admin`).
2. Realm: **focus-track**.
3. **Clients** → **focus-track-client** (or create it with Client ID `focus-track-client`, Client authentication ON, Valid redirect URIs `http://localhost:5000/*`).
4. **Settings** → **Capability config** → enable **Direct access grants** (Resource Owner Password Credentials) → Save.
5. **Credentials** tab → copy **Secret** (you need it below).
6. Create a test user: **Users** → **Add user** (e.g. username `testuser`, email optional) → Save → **Credentials** tab → Set password (e.g. `testuser`) and turn OFF “Temporary”.
7. **Realm roles** → ensure **User** (and optionally **Admin**) exist; assign **User** (and **Admin** for admin tests) to the test user (see [Keycloak-Roles-Setup.md](Keycloak-Roles-Setup.md)).

### Request token in Postman

**Request:** `POST {{keycloak_url}}/realms/focus-track/protocol/openid-connect/token`  
(With Visual Studio + Docker: `keycloak_url` = http://localhost:9080.)  

**Body:** `x-www-form-urlencoded`

| Key          | Value                |
|-------------|----------------------|
| grant_type  | password             |
| client_id   | focus-track-client   |
| client_secret | \<your client secret\> |
| username    | testuser             |
| password    | testuser             |

**Response:** JSON with `access_token`, `refresh_token`, `expires_in`. Copy **access_token** and use it as:

```
Authorization: Bearer <access_token>
```

For **admin** tests, use a user that has the **Admin** realm role (same token request, different user).

---

## 3. Postman setup

1. **Import** the collection: `docs/FocusTrack-Postman-Collection.json`.
2. **Set collection variables** (or create an environment):
   - **Visual Studio + Docker:** `base_url` = **http://localhost:5029**, `keycloak_url` = **http://localhost:9080**.
   - **Full Docker:** `base_url` = **http://localhost:5000**, `keycloak_url` = **http://localhost:9080**.
   - `access_token` = (leave empty; set manually after “Get Token” or use a script).
3. For requests that need auth, set **Authorization** → Type **Bearer Token** → Token = `{{access_token}}` (or paste the token).

---

## 4. First 5 requests (Visual Studio + Docker)

Do these in order to confirm everything works:

1. **Health**  
   `GET http://localhost:5029/healthz`  
   → Expect **200** and `"status":"healthy"`.

2. **Get Token**  
   `POST http://localhost:9080/realms/focus-track/protocol/openid-connect/token`  
   Body (x-www-form-urlencoded): `grant_type=password`, `client_id=focus-track-client`, `client_secret=<your secret>`, `username=testuser`, `password=testuser`.  
   → Copy **access_token** from the response into the collection variable **access_token** (or your environment).

3. **Create Session**  
   `POST http://localhost:5029/api/sessions`  
   Header: **Authorization: Bearer** \<paste token\>.  
   Body (JSON): `{"topic":"Test session","startTime":"2025-04-11T08:15:00Z","mode":1}`.  
   → Expect **201**. Copy the **id** from the response into **session_id**.

4. **Get Session**  
   `GET http://localhost:5029/api/sessions/{{session_id}}`  
   Header: **Authorization: Bearer** \<token\>.  
   → Expect **200** and the same session.

5. **List Sessions**  
   `GET http://localhost:5029/api/sessions?page=1&pageSize=20`  
   Header: **Authorization: Bearer** \<token\>.  
   → Expect **200** and an array containing your session.

If all five succeed, the stack and Postman are set up correctly. Then run the rest of the flows in section 5.

---

## 5. Test flow by feature

### Health

| Request | Expected |
|--------|----------|
| `GET {{base_url}}/healthz` | 200, `"status":"healthy"` |
| `GET {{base_url}}/api/health/readyz` | 200, `"status":"ready"` (if exposed under `/api`) |

(Gateway may expose health at `/healthz` and `/api/health/healthz`, `/api/health/readyz`; adjust URL if needed.)

### U2 – Session CRUD

All with **Authorization: Bearer {{access_token}}**.

1. **Create session**  
   `POST {{base_url}}/api/sessions`  
   Body (JSON):
   ```json
   {
     "topic": "Data Structures – Arrays",
     "startTime": "2025-04-11T08:15:00Z",
     "mode": 1
   }
   ```
   Mode: 0=Reading, 1=Coding, 2=VideoCourse, 3=Practice.  
   Expect **201** and session in response (note `id`).

2. **Get session by id**  
   `GET {{base_url}}/api/sessions/{{session_id}}`  
   Expect **200** and same session.

3. **List sessions**  
   `GET {{base_url}}/api/sessions?page=1&pageSize=20`  
   Expect **200** and array of sessions.

4. **Update session**  
   `PUT {{base_url}}/api/sessions/{{session_id}}`  
   Body (JSON):
   ```json
   {
     "topic": "Updated topic",
     "endTime": "2025-04-11T09:05:00Z",
     "mode": 2
   }
   ```
   Expect **204**.

5. **Delete session**  
   `DELETE {{base_url}}/api/sessions/{{session_id}}`  
   Expect **204**. Then GET same id → **404**.

### U4 – Session sharing

**Prerequisites**

- **Two Keycloak users** (e.g. `testuser` and `otheruser`). Create the second in Keycloak: **Users** → **Add user** (e.g. username `otheruser`) → Save → **Credentials** → Set password (e.g. `otheruser`).
- **Other user’s ID (subject):** In Keycloak Admin → **Users** → click the second user → the **ID** shown at the top (UUID) is the `userIds` / `shared_with_user_id` value. Copy it into a Postman variable (e.g. `other_user_id`).

**Step-by-step (use Postman collection “Sharing (U4)” or the requests below)**

1. **Get token as owner**  
   Use “Get Token” with your **owner** user (e.g. `testuser`). Set `access_token` and ensure `session_id` is set (create a session first with “Create Session” from U2 if needed).

2. **Share session**  
   `POST {{base_url}}/api/sessions/{{session_id}}/share`  
   **Header:** `Authorization: Bearer {{access_token}}`.  
   **Body (JSON):** `{ "userIds": ["<paste-other-user-id-here>"] }`  
   → Expect **204**.

3. **Create public link**  
   `POST {{base_url}}/api/sessions/{{session_id}}/public-link`  
   **Header:** `Authorization: Bearer {{access_token}}`.  
   → Expect **200** and body like `{ "url": "http://.../api/sessions/public/abc123..." }`.  
   Copy the **token** from the URL (the part after `/public/`, e.g. `abc123...`) into a variable (e.g. `public_link_token`).

4. **Get by public link (no auth)**  
   `GET {{base_url}}/api/sessions/public/{{public_link_token}}`  
   Do **not** send Authorization.  
   → Expect **200** and the session JSON.

5. **Revoke public link**  
   `POST {{base_url}}/api/sessions/{{session_id}}/public-link/revoke`  
   **Header:** `Authorization: Bearer {{access_token}}`.  
   → Expect **204**.

6. **Get by public link again**  
   `GET {{base_url}}/api/sessions/public/{{public_link_token}}`  
   → Expect **410 Gone** (link revoked).

7. **Unshare**  
   `DELETE {{base_url}}/api/sessions/{{session_id}}/share/{{other_user_id}}`  
   **Header:** `Authorization: Bearer {{access_token}}`.  
   Use the same other-user UUID as in step 2.  
   → Expect **204**.

**Quick reference**

| Step | Method | URL | Auth | Body / notes |
|------|--------|-----|------|----------------|
| Share | POST | `.../api/sessions/{{session_id}}/share` | Bearer | `{ "userIds": ["<other-user-uuid>"] }` |
| Create public link | POST | `.../api/sessions/{{session_id}}/public-link` | Bearer | — |
| Get by link | GET | `.../api/sessions/public/{{token}}` | None | Copy token from Create response URL |
| Revoke link | POST | `.../api/sessions/{{session_id}}/public-link/revoke` | Bearer | — |
| Unshare | DELETE | `.../api/sessions/{{session_id}}/share/{{other_user_id}}` | Bearer | — |

**How the other user sees the shared session (online vs offline)**

The **Session API** already includes shared sessions in the list: when **otheruser** calls `GET /api/sessions` with their token, they get sessions they own **and** sessions shared with them. So **otheruser always “sees” the session in the list** once it’s shared, regardless of online/offline. The **Notification API** only controls **how** they are notified (real-time vs email).

- **1. Otheruser sees the session (both cases)**  
  Get a token as **otheruser** (Keycloak token with username/password of the second user). Then:
  - `GET {{base_url}}/api/sessions?page=1&pageSize=20`  
  - **Header:** `Authorization: Bearer <otheruser_access_token>`  
  - The shared session must appear in the response. You can also `GET {{base_url}}/api/sessions/{{session_id}}` as otheruser → **200** (they have access because it’s shared).

- **2. Otheruser online (real-time notification)**  
  The Notification API sends a **SignalR** message to connected clients when a session is shared. To test:
  1. Start **Notification API** (e.g. from VS, http profile → `http://localhost:5101`).
  2. Connect a SignalR client as **otheruser** so they are “online”:
     - Open `docs/signalr-test-notifications.html` in a browser. If you open it as a file (`file://...`) and get CORS or connection errors, serve the folder instead (e.g. run `npx serve docs` and open `http://localhost:3000/signalr-test-notifications.html`). Enter the Notification API URL (e.g. `http://localhost:5101`) and the **other user ID** (Keycloak UUID), then click **Connect**.
     - Or in browser console on any page that loads the SignalR script, connect to `http://localhost:5101/hubs/notifications?userId=<other_user_id>` and listen for `SessionShared`.
  3. As **owner**, share the session: `POST {{base_url}}/api/sessions/{{session_id}}/share` with `{ "userIds": ["<other_user_id>"] }`.
  4. The otheruser client should receive the **SessionShared** event (e.g. payload `{ sessionId, ownerUserId, sharedAt }`).  
  - In **Notification API** console you should see: `Session shared (SignalR): SessionId=..., Recipient=<other_user_id>`.

- **3. Otheruser offline (email fallback)**  
  When otheruser is **not** connected to SignalR, the Notification API sends an email (or logs if SMTP is not configured).
  1. **Do not** open the SignalR test page (or disconnect it) so otheruser has no active connection.
  2. As **owner**, share the session again (same request as above).
  3. If SMTP is configured (see below): the otheruser receives an email. Otherwise the Notification API logs:  
     `Email (stub): session shared with user ... Set Email:SmtpHost and Email:UserIdToEmail for real email.`  
  - Otheruser can still see the session by calling `GET /api/sessions` with their token (step 1).

**SMTP setup (so offline otheruser gets a real email)**

In **Notification API** `appsettings.Development.json` (or User Secrets), set:

- **Email:SmtpHost** – e.g. `smtp.gmail.com`, `smtp.office365.com`, or your SMTP server.
- **Email:SmtpPort** – e.g. `587` (TLS), `465` (SSL).
- **Email:SmtpUserName** / **Email:SmtpPassword** – SMTP credentials. For Gmail use an [App Password](https://support.google.com/accounts/answer/185833), not your normal password.
- **Email:FromAddress** / **Email:FromName** – sender shown on the email.
- **Email:UserIdToEmail** – map Keycloak user ID → email so the Notification API knows where to send:
  - `"Email:UserIdToEmail:<other_user_keycloak_id>"`: `"otheruser@example.com"`  
  - Or in JSON: `"UserIdToEmail": { "<other_user_keycloak_id>": "otheruser@example.com" }`.

Restart the Notification API after changing config. When the otheruser is offline and you share a session, they receive the “Session shared with you” email.

**Summary**

| Scenario   | How otheruser sees the session        | How they are notified                          |
|-----------|----------------------------------------|------------------------------------------------|
| Any       | `GET /api/sessions` with otheruser token | —                                              |
| Online    | Same                                   | SignalR event **SessionShared** (real-time)    |
| Offline   | Same                                   | Email (if SMTP + UserIdToEmail configured); else stub log |

### U5 – Logout

`POST {{base_url}}/api/auth/logout`  
With **Authorization: Bearer {{access_token}}** (optional; clears server-side session if any).  
Expect **204**. Then any protected call with the same token may still work until token expiry (logout revokes refresh token and clears cookie; with Bearer, Postman keeps the token until you remove it).

### A1 – Admin session filtering

Use a user with **Admin** role; **Authorization: Bearer {{access_token}}**.

`GET {{base_url}}/admin/sessions?page=1&pageSize=10`  
Optional: `userId`, `mode`, `startDateFrom`, `startDateTo`, `endDateFrom`, `endDateTo`, `minDuration`, `maxDuration`, `orderBy`, `direction`.  
Expect **200** and list; check **X-Total-Count** header.

### A2 – Monthly focus statistics

`GET {{base_url}}/admin/statistics/monthly-focus?page=1&pageSize=10`  
Optional: `orderBy=UserId` or `TotalDurationMin`, `direction=asc` or `desc`.  
Expect **200** and items `{ userId, year, month, totalDurationMin }`; check **X-Total-Count**.

### A3 – User management

1. **Set user status**  
   `PATCH {{base_url}}/admin/users/{{user_id}}/status`  
   Body (JSON): `{ "status": "Suspended" }`  
   Allowed: `Active`, `Suspended`, `Deactivated`.  
   Expect **204**.

2. **Verify blocked login (A3)**  
   Set status to **Suspended** or **Deactivated** for a user. In a browser, log in as that user via the Gateway; you should see the “account disabled” page, not the app. (In Postman you only test the admin PATCH; the block is enforced at login in the browser.)

### U3 – Daily focus reward (120 min badge)

- The **Reward Worker** consumes session events and sets `IsDailyGoalAchieved` when daily focus ≥ 120 min.
- **In Postman:** Create/update sessions so a user’s total for the day reaches ≥ 120 minutes (e.g. two sessions of 60 min each). The worker runs asynchronously; after a short delay, GET the session that crossed 120 min and check `isDailyGoalAchieved === true`.
- **Internal API (if you call Session API directly):**  
  `PATCH http://localhost:5001/api/sessions/{{session_id}}/daily-goal-achieved`  
  Header: `X-Internal-Api-Key: <Session API internal key>` (only for testing worker behavior; normally the worker calls this).

---

## 6. Validation and errors

- **400** – Validation (e.g. invalid body). Response body should be machine-readable (e.g. `errors` with field names).
- **401** – No token or invalid/expired token.
- **403** – Not allowed (e.g. non-admin on admin endpoints).
- **404** – Resource not found (e.g. wrong session id or not owner/shared).
- **429** – Login rate limit (5/min per IP on `/signin-oidc`).

---

## 7. Quick checklist

| Area        | What to test |
|------------|--------------|
| Health     | GET /healthz, /readyz |
| U2 Sessions| Create, Get, List, Update, Delete |
| U4 Sharing | Share, public link, get by token, revoke, unshare |
| U5 Logout  | POST /api/auth/logout |
| A1 Filter  | GET /admin/sessions with query params + X-Total-Count |
| A2 Stats   | GET /admin/statistics/monthly-focus + X-Total-Count |
| A3 Status  | PATCH /admin/users/{id}/status (Active/Suspended/Deactivated) |
| U3 Reward  | Sessions totalling 120 min → session has isDailyGoalAchieved |

Use **User** token for session and sharing; **Admin** token for A1, A2, A3.
