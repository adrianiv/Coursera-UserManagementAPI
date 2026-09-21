using System.Collections.Concurrent;
using UserManagementAPI.Middleware;
using UserManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// In-memory "database"
var users = new ConcurrentDictionary<int, User>();
users[1] = new User { Id = 1, Name = "Alice" };
users[2] = new User { Id = 2, Name = "Bob" };
var nextId = 3;

// Middleware pipeline order:
//   1. Error handling — wraps everything after it, so a failure anywhere
//      below (including in auth or logging) still gets a clean JSON 500
//      instead of a crash.
//   2. Authentication — rejects invalid/missing tokens before any
//      business logic runs.
//   3. Logging — records method, path, and status code for requests that
//      make it past authentication.
app.UseErrorHandling();
app.UseTokenAuthentication();
app.UseRequestResponseLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var usersGroup = app.MapGroup("/users");

// GET /users -> list of all users
usersGroup.MapGet("/", () => Results.Ok(users.Values.ToList()));

// GET /users/{id} -> a specific user, or 404
usersGroup.MapGet("/{id:int}", (int id) =>
{
    return users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound(new { message = $"User with Id {id} not found." });
});

// POST /users -> create a new user
usersGroup.MapPost("/", (User newUser) =>
{
    var validationError = ValidateUser(newUser);
    if (validationError is not null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var id = Interlocked.Increment(ref nextId) - 1;
    var user = new User { Id = id, Name = newUser.Name.Trim() };
    users[id] = user;
    return Results.Created($"/users/{id}", user);
});

// PUT /users/{id} -> update an existing user's name
usersGroup.MapPut("/{id:int}", (int id, User updatedUser) =>
{
    if (!users.ContainsKey(id))
    {
        return Results.NotFound(new { message = $"User with Id {id} not found." });
    }

    var validationError = ValidateUser(updatedUser);
    if (validationError is not null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var updated = new User { Id = id, Name = updatedUser.Name.Trim() };
    users[id] = updated;
    return Results.Ok(updated);
});

// DELETE /users/{id} -> remove a user
usersGroup.MapDelete("/{id:int}", (int id) =>
{
    return users.TryRemove(id, out _)
        ? Results.NoContent()
        : Results.NotFound(new { message = $"User with Id {id} not found." });
});

app.Run();

// Shared validation for create/update.
static string? ValidateUser(User user)
{
    if (user is null)
    {
        return "Request body is required.";
    }

    if (string.IsNullOrWhiteSpace(user.Name))
    {
        return "Name is required and cannot be empty or whitespace.";
    }

    var trimmed = user.Name.Trim();

    if (trimmed.Length > 100)
    {
        return "Name cannot exceed 100 characters.";
    }

    // Blocks names that are only digits/punctuation, e.g. "12345".
    if (!trimmed.Any(char.IsLetter))
    {
        return "Name must contain at least one letter.";
    }

    return null;
}
