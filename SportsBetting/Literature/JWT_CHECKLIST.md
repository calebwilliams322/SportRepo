# JWT Implementation Checklist

Quick reference for completing JWT authentication implementation.

## ☐ Phase 1: Configuration & Setup

- [ ] **Step 1.1**: Add JWT configuration to `appsettings.json`
  - Add Jwt section with Secret, Issuer, Audience
  - Set up user secrets for development: `dotnet user-secrets set "Jwt:Secret" "your-secret"`

## ☐ Phase 2: Create DTOs

- [ ] **Step 2.1**: Create `SportsBetting.API/Models/Auth/` folder
- [ ] **Step 2.2**: Create `LoginRequest.cs`
- [ ] **Step 2.3**: Create `RegisterRequest.cs`
- [ ] **Step 2.4**: Create `AuthResponse.cs` and `UserInfo.cs`
- [ ] **Step 2.5**: Create `RefreshTokenRequest.cs`

## ☐ Phase 3: Create Services

- [ ] **Step 3.1**: Create `IAuthService.cs` interface
- [ ] **Step 3.2**: Create `AuthService.cs` implementation
  - Implement RegisterAsync
  - Implement LoginAsync
  - Implement RefreshTokenAsync
  - Implement RevokeTokenAsync
  - Implement GenerateAuthResponseAsync helper

## ☐ Phase 4: Create Controller

- [ ] **Step 4.1**: Create `AuthController.cs`
  - Add Register endpoint (POST /api/auth/register)
  - Add Login endpoint (POST /api/auth/login)
  - Add Refresh endpoint (POST /api/auth/refresh)
  - Add Logout endpoint (POST /api/auth/logout)
  - Add GetCurrentUser endpoint (GET /api/auth/me)

## ☐ Phase 5: Configure Middleware

- [ ] **Step 5.1**: Update `Program.cs`
  - Add JWT authentication configuration
  - Add authorization configuration
  - Register JwtService and IAuthService
  - Update Swagger to support JWT
  - Ensure UseAuthentication() comes before UseAuthorization()

## ☐ Phase 6: Database

- [ ] **Step 6.1**: Create migration: `dotnet ef migrations add AddRefreshTokens --project ../SportsBetting.Data --startup-project .`
- [ ] **Step 6.2**: Update database: `dotnet ef database update --project ../SportsBetting.Data --startup-project .`
- [ ] **Step 6.3**: Verify RefreshTokens table exists in database

## ☐ Phase 7: Protect Endpoints

- [ ] **Step 7.1**: Add `[Authorize]` to `BetsController`
- [ ] **Step 7.2**: Add `[Authorize]` to `WalletsController`
- [ ] **Step 7.3**: Add `[Authorize]` to `EventsController` (if needed)
- [ ] **Step 7.4**: Add helper methods to get current user ID from JWT claims

## ☐ Phase 8: Testing

- [ ] **Step 8.1**: Test user registration
- [ ] **Step 8.2**: Test user login
- [ ] **Step 8.3**: Test accessing protected endpoint with JWT
- [ ] **Step 8.4**: Test token refresh
- [ ] **Step 8.5**: Test logout
- [ ] **Step 8.6**: Test accessing protected endpoint after logout (should fail)
- [ ] **Step 8.7**: Test invalid credentials (should fail)
- [ ] **Step 8.8**: Test expired token (should fail)

## ☐ Phase 9: Build & Run

- [ ] **Step 9.1**: Build solution: `dotnet build`
- [ ] **Step 9.2**: Run API: `dotnet run --project SportsBetting.API`
- [ ] **Step 9.3**: Test endpoints via Swagger UI at http://localhost:5000/swagger
- [ ] **Step 9.4**: Verify all authentication flows work correctly

## ☐ Phase 10: Security Review (Optional but Recommended)

- [ ] Review password requirements
- [ ] Consider adding email verification
- [ ] Consider adding password reset
- [ ] Consider adding rate limiting
- [ ] Consider adding account lockout
- [ ] Review CORS settings for production
- [ ] Ensure HTTPS is enforced in production
- [ ] Review token expiration times
- [ ] Consider adding 2FA support

---

## Quick Command Reference

```bash
# Set JWT secret (development)
dotnet user-secrets init --project SportsBetting.API
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 32)" --project SportsBetting.API

# Create migration
cd SportsBetting.API
dotnet ef migrations add AddRefreshTokens --project ../SportsBetting.Data --startup-project .

# Update database
dotnet ef database update --project ../SportsBetting.Data --startup-project .

# Build solution
dotnet build

# Run API
dotnet run --project SportsBetting.API

# Run tests
dotnet test
```

---

## Files to Create/Modify

### New Files:
- ✅ `SportsBetting.API/Models/Auth/LoginRequest.cs`
- ✅ `SportsBetting.API/Models/Auth/RegisterRequest.cs`
- ✅ `SportsBetting.API/Models/Auth/AuthResponse.cs`
- ✅ `SportsBetting.API/Models/Auth/RefreshTokenRequest.cs`
- ✅ `SportsBetting.API/Services/IAuthService.cs`
- ✅ `SportsBetting.API/Services/AuthService.cs`
- ✅ `SportsBetting.API/Controllers/AuthController.cs`

### Modified Files:
- ✅ `SportsBetting.API/appsettings.json`
- ✅ `SportsBetting.API/Program.cs`
- ✅ `SportsBetting.API/Controllers/BetsController.cs`
- ✅ `SportsBetting.API/Controllers/WalletsController.cs`
- ✅ `SportsBetting.API/Controllers/EventsController.cs`

---

## Estimated Time: 2-3 hours

- Phase 1-2: 15 minutes
- Phase 3-4: 45 minutes
- Phase 5-6: 30 minutes
- Phase 7: 15 minutes
- Phase 8-9: 45 minutes
- Phase 10: 30 minutes (optional)
