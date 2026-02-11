# Keycloak: Admin and User roles

The Gateway expects two realm roles so that admin endpoints can require **Admin** and regular users use **User**.

## 1. Create realm roles

In Keycloak Admin Console (e.g. http://localhost:9080):

1. Select realm **focus-track** (or create it).
2. Go to **Realm roles** → **Create role**.
3. Create:
   - **Admin**
   - **User**

## 2. Assign roles to users

1. Go to **Users** → select a user.
2. Open the **Role mapping** tab.
3. Click **Assign role** → choose **Filter by realm roles**.
4. Assign **Admin** to admin users and **User** to normal users (assign at least **User** to everyone who can sign in).

## 3. Roles in the token

Keycloak includes realm roles in the **access token** as:

```json
"realm_access": {
  "roles": ["User", "Admin"]
}
```

The Gateway’s **KeycloakRolesClaimsTransformation** reads this (from the principal or by decoding the access token) and adds `ClaimTypes.Role` claims. Then `[Authorize(Roles = "Admin")]` and `User.IsInRole("Admin")` work.

## 4. Optional: client scope for roles

If roles do not appear in the access token:

1. Go to **Clients** → **focus-track-client** (or your client).
2. **Client scopes** → ensure the client has a scope that includes **realm roles** in the token (e.g. add the built-in **roles** scope or a mapper that adds `realm_access.roles` to the access token).

No code changes are needed in the app beyond the existing claims transformation; only Keycloak configuration.
