using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    
    // In-memory storage for demonstration purposes
    // BUG FIX: Added lock object for thread-safe operations
    private static readonly object _lock = new object();
    private static readonly List<User> _users = new()
    {
        new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", PhoneNumber = "123-456-7890", CreatedAt = DateTime.UtcNow },
        new User { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", PhoneNumber = "098-765-4321", CreatedAt = DateTime.UtcNow }
    };
    private static int _nextId = 3;

    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }

    // GET: api/user
    /// <summary>
    /// Retrieves all users
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<User>> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Retrieving all users at {Time}", DateTime.UtcNow);
            
            // BUG FIX: Performance optimization - return materialized list to avoid enumeration issues
            lock (_lock)
            {
                var userList = _users.ToList();
                _logger.LogInformation("Successfully retrieved {Count} users", userList.Count);
                return Ok(userList);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all users");
            return StatusCode(500, new { message = "An error occurred while retrieving users", details = ex.Message });
        }
    }

    // GET: api/user/{id}
    /// <summary>
    /// Retrieves a specific user by ID
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<User> GetUserById(int id)
    {
        try
        {
            // BUG FIX: Validate input ID
            if (id <= 0)
            {
                _logger.LogWarning("Invalid user ID requested: {Id}", id);
                return BadRequest(new { message = "User ID must be a positive integer" });
            }

            _logger.LogInformation("Retrieving user with ID: {Id}", id);
            
            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                
                if (user == null)
                {
                    _logger.LogWarning("User with ID {Id} not found", id);
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                _logger.LogInformation("Successfully retrieved user with ID: {Id}", id);
                return Ok(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving user with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the user", details = ex.Message });
        }
    }

    // POST: api/user
    /// <summary>
    /// Creates a new user
    /// </summary>
    [HttpPost]
    public ActionResult<User> CreateUser([FromBody] CreateUserDto userDto)
    {
        try
        {
            // BUG FIX: Enhanced validation
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for user creation: {Errors}", ModelState.Values);
                return BadRequest(ModelState);
            }

            // BUG FIX: Check for duplicate email
            lock (_lock)
            {
                if (_users.Any(u => u.Email.Equals(userDto.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Attempt to create user with duplicate email: {Email}", userDto.Email);
                    return Conflict(new { message = "A user with this email already exists" });
                }

                var newUser = new User
                {
                    Id = _nextId++,
                    FirstName = userDto.FirstName.Trim(),
                    LastName = userDto.LastName.Trim(),
                    Email = userDto.Email.Trim().ToLowerInvariant(),
                    PhoneNumber = userDto.PhoneNumber?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _users.Add(newUser);
                _logger.LogInformation("Successfully created user with ID: {Id}, Email: {Email}", newUser.Id, newUser.Email);

                return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating user");
            return StatusCode(500, new { message = "An error occurred while creating the user", details = ex.Message });
        }
    }

    // PUT: api/user/{id}
    /// <summary>
    /// Updates an existing user's details
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<User> UpdateUser(int id, [FromBody] UpdateUserDto userDto)
    {
        try
        {
            // BUG FIX: Validate input ID
            if (id <= 0)
            {
                _logger.LogWarning("Invalid user ID for update: {Id}", id);
                return BadRequest(new { message = "User ID must be a positive integer" });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for user update: {Errors}", ModelState.Values);
                return BadRequest(ModelState);
            }

            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                
                if (user == null)
                {
                    _logger.LogWarning("User with ID {Id} not found for update", id);
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                // BUG FIX: Check for duplicate email (excluding current user)
                if (_users.Any(u => u.Id != id && u.Email.Equals(userDto.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Attempt to update user {Id} with duplicate email: {Email}", id, userDto.Email);
                    return Conflict(new { message = "Another user with this email already exists" });
                }

                user.FirstName = userDto.FirstName.Trim();
                user.LastName = userDto.LastName.Trim();
                user.Email = userDto.Email.Trim().ToLowerInvariant();
                user.PhoneNumber = userDto.PhoneNumber?.Trim();
                user.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Successfully updated user with ID: {Id}", id);
                return Ok(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating user with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the user", details = ex.Message });
        }
    }

    // DELETE: api/user/{id}
    /// <summary>
    /// Deletes a user by ID
    /// </summary>
    [HttpDelete("{id}")]
    public ActionResult DeleteUser(int id)
    {
        try
        {
            // BUG FIX: Validate input ID
            if (id <= 0)
            {
                _logger.LogWarning("Invalid user ID for deletion: {Id}", id);
                return BadRequest(new { message = "User ID must be a positive integer" });
            }

            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                
                if (user == null)
                {
                    _logger.LogWarning("User with ID {Id} not found for deletion", id);
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                _users.Remove(user);
                _logger.LogInformation("Successfully deleted user with ID: {Id}, Email: {Email}", id, user.Email);

                return Ok(new { message = $"User with ID {id} has been successfully deleted" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting user with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the user", details = ex.Message });
        }
    }
}

// DTOs (Data Transfer Objects)
// BUG FIX: Added comprehensive validation attributes
public class CreateUserDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
    public required string FirstName { get; set; }
    
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
    public required string LastName { get; set; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string Email { get; set; }
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }
}

public class UpdateUserDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
    public required string FirstName { get; set; }
    
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
    public required string LastName { get; set; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string Email { get; set; }
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string? PhoneNumber { get; set; }
}
