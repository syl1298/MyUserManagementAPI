# User Management API - Testing & Improvement Guide

## 🚀 How to Run the API

1. **Build and Run the Project**
   ```bash
   dotnet build
   dotnet run
   ```

2. **Note the API URL** - The API will typically run on:
   - HTTPS: `https://localhost:7000`
   - HTTP: `http://localhost:5000`
   
   Check your console output for the exact port numbers.

---

## 🧪 Testing with Postman

### Step 1: Import the Collection
1. Open Postman
2. Click **Import** button
3. Select the file: `PostmanCollection/UserManagementAPI.postman_collection.json`
4. The collection will include 10 pre-configured test requests

### Step 2: Configure Environment Variable
1. Before running tests, verify the `baseUrl` variable in the collection
2. Update it to match your API's actual URL (e.g., `https://localhost:7000`)

### Step 3: Run the Tests
**Run tests in this order:**
1. ✅ **GET All Users** - Verify initial seed data
2. ✅ **GET User by ID (Valid)** - Retrieve user with ID 1
3. ✅ **GET User by ID (Invalid)** - Test 404 error handling
4. ✅ **POST Create New User** - Add a new user (saves ID for later tests)
5. ✅ **POST Create User (Invalid)** - Test validation
6. ✅ **PUT Update Existing User** - Update the created user
7. ✅ **PUT Update User (Invalid)** - Test 404 on update
8. ✅ **DELETE User by ID** - Delete the created user
9. ✅ **DELETE User (Invalid)** - Test 404 on delete
10. ✅ **Verify User Deleted** - Confirm deletion

### Step 4: Run Collection with Test Runner
- Click the **Run** button on the collection
- Select all requests
- Click **Run User Management API - CRUD Tests**
- View automated test results

---

## 📊 Expected Test Results

All requests include automated tests that verify:
- ✅ Correct HTTP status codes
- ✅ Response data structure
- ✅ Required fields are present
- ✅ Data integrity after operations
- ✅ Proper error messages

---

## 🔧 API Improvement Recommendations

### 1. **Data Persistence** ⭐ HIGH PRIORITY
**Current Issue:** Uses in-memory storage - data is lost when the API restarts.

**Solutions:**
- **SQL Server with Entity Framework Core:**
  ```bash
  dotnet add package Microsoft.EntityFrameworkCore.SqlServer
  dotnet add package Microsoft.EntityFrameworkCore.Tools
  ```
  
- **SQLite (Simpler for development):**
  ```bash
  dotnet add package Microsoft.EntityFrameworkCore.Sqlite
  ```

- **Create DbContext:**
  ```csharp
  public class UserDbContext : DbContext
  {
      public DbSet<User> Users { get; set; }
      
      public UserDbContext(DbContextOptions<UserDbContext> options) 
          : base(options) { }
  }
  ```

---

### 2. **Input Validation** ⭐ HIGH PRIORITY
**Add Data Annotations & FluentValidation:**

```bash
dotnet add package FluentValidation.AspNetCore
```

**Enhance DTOs:**
```csharp
public class CreateUserDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2)]
    public required string FirstName { get; set; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; }
    
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber { get; set; }
}
```

---

### 3. **API Documentation with Swagger** ⭐ MEDIUM PRIORITY

```bash
dotnet add package Swashbuckle.AspNetCore
```

**Update Program.cs:**
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "User Management API", 
        Version = "v1",
        Description = "API for managing users with full CRUD operations"
    });
});

// In app configuration:
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

---

### 4. **Error Handling Middleware** ⭐ MEDIUM PRIORITY

**Create Global Exception Handler:**
```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        
        return context.Response.WriteAsJsonAsync(new 
        { 
            message = "An error occurred processing your request",
            details = ex.Message 
        });
    }
}
```

---

### 5. **Logging** ⭐ MEDIUM PRIORITY

**Use ILogger in Controller:**
```csharp
private readonly ILogger<UserController> _logger;

public UserController(ILogger<UserController> logger)
{
    _logger = logger;
}

[HttpGet]
public ActionResult<IEnumerable<User>> GetAllUsers()
{
    _logger.LogInformation("Retrieving all users at {Time}", DateTime.UtcNow);
    return Ok(_users);
}
```

**Add Serilog for advanced logging:**
```bash
dotnet add package Serilog.AspNetCore
```

---

### 6. **Authentication & Authorization** ⭐ HIGH PRIORITY (Production)

**Add JWT Authentication:**
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

**Protect endpoints:**
```csharp
[Authorize]
[HttpDelete("{id}")]
public ActionResult DeleteUser(int id)
{
    // Only authenticated users can delete
}
```

---

### 7. **Pagination & Filtering** ⭐ MEDIUM PRIORITY

**Improve GET All Users:**
```csharp
[HttpGet]
public ActionResult<IEnumerable<User>> GetAllUsers(
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null)
{
    var query = _users.AsQueryable();
    
    if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(u => 
            u.FirstName.Contains(search) || 
            u.LastName.Contains(search) ||
            u.Email.Contains(search));
    }
    
    var total = query.Count();
    var users = query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();
    
    return Ok(new { total, page, pageSize, data = users });
}
```

---

### 8. **Rate Limiting** ⭐ LOW PRIORITY

**Prevent API abuse:**
```bash
dotnet add package AspNetCoreRateLimit
```

---

### 9. **Response Caching** ⭐ LOW PRIORITY

**Cache GET requests:**
```csharp
[HttpGet]
[ResponseCache(Duration = 60)]
public ActionResult<IEnumerable<User>> GetAllUsers()
{
    return Ok(_users);
}
```

---

### 10. **API Versioning** ⭐ LOW PRIORITY

**Support multiple API versions:**
```bash
dotnet add package Microsoft.AspNetCore.Mvc.Versioning
```

---

## 📈 Priority Implementation Order

1. **Phase 1 - Core Improvements** (Week 1)
   - ✅ Add database persistence (Entity Framework)
   - ✅ Implement input validation
   - ✅ Add Swagger documentation

2. **Phase 2 - Security & Reliability** (Week 2)
   - ✅ Global error handling
   - ✅ Logging with Serilog
   - ✅ Authentication with JWT

3. **Phase 3 - Performance & UX** (Week 3)
   - ✅ Pagination and filtering
   - ✅ Response caching
   - ✅ Rate limiting

4. **Phase 4 - Scalability** (Future)
   - ✅ API versioning
   - ✅ Distributed caching (Redis)
   - ✅ Load balancing support

---

## 🎯 Quick Wins (Implement Today)

1. Add Swagger UI for interactive testing
2. Implement basic input validation with Data Annotations
3. Add logging to track API usage
4. Create a global exception handler

---

## 📝 Additional Resources

- **ASP.NET Core Documentation:** https://docs.microsoft.com/aspnet/core
- **Entity Framework Core:** https://docs.microsoft.com/ef/core
- **Postman Learning Center:** https://learning.postman.com
- **RESTful API Best Practices:** https://restfulapi.net

---

## ✅ Testing Checklist

- [ ] All Postman tests pass successfully
- [ ] GET endpoints return correct data
- [ ] POST creates users with auto-incremented IDs
- [ ] PUT updates user data correctly
- [ ] DELETE removes users and returns proper status
- [ ] 404 errors returned for non-existent users
- [ ] 400 errors returned for invalid input
- [ ] CORS is configured for frontend integration

---

**Created:** December 28, 2025  
**API Version:** 1.0  
**Framework:** .NET 10.0
