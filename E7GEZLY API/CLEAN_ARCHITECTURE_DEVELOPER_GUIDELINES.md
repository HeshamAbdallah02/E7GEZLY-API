# E7GEZLY Clean Architecture Developer Guidelines

## Overview

This document provides comprehensive guidelines for developing new features and maintaining the E7GEZLY API using Clean Architecture principles. All development must follow these patterns to ensure consistency, maintainability, and adherence to architectural boundaries.

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Development Patterns](#development-patterns)
3. [Feature Development Workflow](#feature-development-workflow)
4. [Code Examples](#code-examples)
5. [Testing Guidelines](#testing-guidelines)
6. [Performance Considerations](#performance-considerations)
7. [Security Guidelines](#security-guidelines)
8. [Error Handling](#error-handling)
9. [Best Practices](#best-practices)
10. [Common Pitfalls](#common-pitfalls)

## Architecture Overview

### Layer Dependencies (CRITICAL)
```
API Layer (Controllers/Middleware)
    ↓ depends on
Application Layer (Use Cases/Handlers)
    ↓ depends on  
Domain Layer (Business Logic/Entities)

Infrastructure Layer (Data Access/External Services)
    ↓ implements interfaces from
Application Layer + Domain Layer
```

**RULE**: Dependencies MUST flow inward. Outer layers can depend on inner layers, but inner layers CANNOT depend on outer layers.

### Project Structure
```
E7GEZLY API/
├── Domain/                    # Core business logic (NO dependencies)
│   ├── Common/               # Base classes, interfaces
│   ├── Entities/             # Business entities
│   ├── ValueObjects/         # Immutable value objects
│   ├── Enums/               # Domain enumerations
│   ├── Events/              # Domain events
│   ├── Repositories/        # Repository interfaces
│   └── Services/            # Domain services
├── Application/              # Application logic (depends on Domain only)
│   ├── Common/              # Shared application concerns
│   │   ├── Behaviors/       # MediatR pipeline behaviors
│   │   ├── Interfaces/      # Application interfaces
│   │   ├── Models/          # Application models (Result pattern)
│   │   └── Services/        # Application services
│   └── Features/            # Feature-based organization
│       └── [FeatureName]/
│           ├── Commands/    # Write operations (CQRS)
│           └── Queries/     # Read operations (CQRS)
├── Infrastructure/           # External concerns (implements Application/Domain interfaces)
│   ├── Persistence/         # Database implementations
│   └── Repositories/        # Repository implementations
└── Controllers/             # API endpoints (uses Application layer via MediatR)
```

## Development Patterns

### 1. CQRS Pattern (Command Query Responsibility Segregation)

**Commands**: Operations that modify state
**Queries**: Operations that read state

### 2. MediatR Pattern
All business logic flows through MediatR handlers, ensuring loose coupling and testability.

### 3. Repository Pattern
Data access is abstracted through repository interfaces defined in Domain layer.

### 4. Result Pattern
Instead of throwing exceptions for business logic failures, use `ApplicationResult<T>`.

### 5. Domain Events
For eventual consistency and decoupled business logic.

## Feature Development Workflow

### Step 1: Define Domain Concepts

1. **Create/Update Domain Entity** (if needed)
```csharp
// Domain/Entities/NewEntity.cs
public class NewEntity : BaseEntity
{
    private NewEntity() { } // Private constructor for EF Core
    
    // Static factory method for creation
    public static NewEntity Create(string name, int value)
    {
        var entity = new NewEntity();
        entity.SetName(name);
        entity.SetValue(value);
        entity.RaiseDomainEvent(new NewEntityCreatedEvent(entity.Id));
        return entity;
    }
    
    public string Name { get; private set; } = string.Empty;
    public int Value { get; private set; }
    
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty");
        Name = name;
        MarkAsModified();
    }
}
```

2. **Create Value Objects** (if needed)
```csharp
// Domain/ValueObjects/NewValueObject.cs
public class NewValueObject : ValueObject
{
    public string Value { get; }
    
    public NewValueObject(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Value cannot be empty");
        Value = value;
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}
```

3. **Define Repository Interface**
```csharp
// Domain/Repositories/INewEntityRepository.cs
public interface INewEntityRepository
{
    Task<NewEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<NewEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<NewEntity> AddAsync(NewEntity entity, CancellationToken cancellationToken = default);
    Task<NewEntity> UpdateAsync(NewEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### Step 2: Implement Application Layer

1. **Create Command/Query**
```csharp
// Application/Features/NewFeature/Commands/CreateNewEntity/CreateNewEntityCommand.cs
public record CreateNewEntityCommand(
    string Name,
    int Value
) : IRequest<ApplicationResult<Guid>>;
```

2. **Create Validator**
```csharp
// Application/Features/NewFeature/Commands/CreateNewEntity/CreateNewEntityValidator.cs
public class CreateNewEntityValidator : AbstractValidator<CreateNewEntityCommand>
{
    public CreateNewEntityValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
            
        RuleFor(x => x.Value)
            .GreaterThan(0);
    }
}
```

3. **Create Handler**
```csharp
// Application/Features/NewFeature/Commands/CreateNewEntity/CreateNewEntityHandler.cs
public class CreateNewEntityHandler : IRequestHandler<CreateNewEntityCommand, ApplicationResult<Guid>>
{
    private readonly INewEntityRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateNewEntityHandler> _logger;

    public CreateNewEntityHandler(
        INewEntityRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateNewEntityHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ApplicationResult<Guid>> Handle(
        CreateNewEntityCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating new entity with name {Name}", request.Name);

            var entity = NewEntity.Create(request.Name, request.Value);
            
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created entity with ID {EntityId}", entity.Id);

            return ApplicationResult<Guid>.Success(entity.Id);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain validation failed: {Error}", ex.Message);
            return ApplicationResult<Guid>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new entity");
            return ApplicationResult<Guid>.Failure("Failed to create entity");
        }
    }
}
```

### Step 3: Implement Infrastructure Layer

1. **Implement Repository**
```csharp
// Infrastructure/Repositories/NewEntityRepository.cs
public class NewEntityRepository : INewEntityRepository
{
    private readonly AppDbContext _context;

    public NewEntityRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NewEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NewEntities
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<NewEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NewEntities
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<NewEntity> AddAsync(NewEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.NewEntities.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    public async Task<NewEntity> UpdateAsync(NewEntity entity, CancellationToken cancellationToken = default)
    {
        _context.NewEntities.Update(entity);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            _context.NewEntities.Remove(entity);
        }
    }
}
```

2. **Register in DependencyInjection**
```csharp
// Infrastructure/DependencyInjection.cs
services.AddScoped<INewEntityRepository, NewEntityRepository>();
```

### Step 4: Create API Controller

```csharp
// Controllers/NewFeatureController.cs
[ApiController]
[Route("api/new-feature")]
public class NewFeatureController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NewFeatureController> _logger;

    public NewFeatureController(IMediator mediator, ILogger<NewFeatureController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateNewEntityCommand command)
    {
        _logger.LogInformation("Received request to create new entity");

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(ApiResponseDto<Guid>.Success(result.Data, "Entity created successfully"));
        }

        return BadRequest(ApiResponseDto<object>.Error(result.Error));
    }
}
```

## Code Examples

### Query Example
```csharp
// Query
public record GetNewEntitiesQuery(int? Skip = null, int? Take = null) 
    : IRequest<ApplicationResult<IEnumerable<NewEntityDto>>>;

// Handler
public class GetNewEntitiesHandler : IRequestHandler<GetNewEntitiesQuery, ApplicationResult<IEnumerable<NewEntityDto>>>
{
    private readonly INewEntityRepository _repository;
    private readonly IMapper _mapper;

    public GetNewEntitiesHandler(INewEntityRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ApplicationResult<IEnumerable<NewEntityDto>>> Handle(
        GetNewEntitiesQuery request, 
        CancellationToken cancellationToken)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        
        if (request.Skip.HasValue)
            entities = entities.Skip(request.Skip.Value);
            
        if (request.Take.HasValue)
            entities = entities.Take(request.Take.Value);

        var dtos = _mapper.Map<IEnumerable<NewEntityDto>>(entities);
        
        return ApplicationResult<IEnumerable<NewEntityDto>>.Success(dtos);
    }
}
```

### Update Command Example
```csharp
// Command
public record UpdateNewEntityCommand(
    Guid Id,
    string Name,
    int Value
) : IRequest<ApplicationResult<Unit>>;

// Handler
public class UpdateNewEntityHandler : IRequestHandler<UpdateNewEntityCommand, ApplicationResult<Unit>>
{
    private readonly INewEntityRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNewEntityHandler(INewEntityRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApplicationResult<Unit>> Handle(
        UpdateNewEntityCommand request, 
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (entity == null)
            return ApplicationResult<Unit>.Failure("Entity not found");

        try
        {
            entity.SetName(request.Name);
            entity.SetValue(request.Value);

            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Unit>.Success(Unit.Value);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<Unit>.Failure(ex.Message);
        }
    }
}
```

## Testing Guidelines

### 1. Domain Unit Tests
```csharp
[Test]
public void Create_ValidData_ShouldCreateEntity()
{
    // Arrange
    var name = "Test Entity";
    var value = 100;

    // Act
    var entity = NewEntity.Create(name, value);

    // Assert
    entity.Name.Should().Be(name);
    entity.Value.Should().Be(value);
    entity.Id.Should().NotBe(Guid.Empty);
}

[Test]
public void SetName_EmptyString_ShouldThrowDomainException()
{
    // Arrange
    var entity = NewEntity.Create("Valid Name", 100);

    // Act & Assert
    Assert.Throws<DomainException>(() => entity.SetName(""));
}
```

### 2. Application Handler Tests
```csharp
[Test]
public async Task Handle_ValidCommand_ShouldCreateEntity()
{
    // Arrange
    var command = new CreateNewEntityCommand("Test", 100);
    var mockRepository = new Mock<INewEntityRepository>();
    var mockUnitOfWork = new Mock<IUnitOfWork>();
    var mockLogger = new Mock<ILogger<CreateNewEntityHandler>>();

    var handler = new CreateNewEntityHandler(
        mockRepository.Object, 
        mockUnitOfWork.Object, 
        mockLogger.Object);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    mockRepository.Verify(x => x.AddAsync(It.IsAny<NewEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

### 3. Integration Tests
```csharp
[Test]
public async Task CreateEntity_ValidData_ShouldReturnCreatedEntity()
{
    // Arrange
    var client = _factory.CreateClient();
    var command = new CreateNewEntityCommand("Test Entity", 100);

    // Act
    var response = await client.PostAsJsonAsync("/api/new-feature", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadFromJsonAsync<ApiResponseDto<Guid>>();
    content.IsSuccess.Should().BeTrue();
    content.Data.Should().NotBe(Guid.Empty);
}
```

## Performance Considerations

### 1. Database Queries
- Use async/await consistently
- Include related data efficiently with `Include()`
- Avoid N+1 queries
- Use pagination for large datasets

### 2. Caching
- Cache frequently accessed read-only data
- Use distributed caching for scalability
- Implement cache invalidation strategies

### 3. Response Times
- Target sub-200ms response times
- Use profiling to identify bottlenecks
- Optimize database queries first

## Security Guidelines

### 1. Authentication & Authorization
```csharp
[HttpPost]
[Authorize(Policy = "VenueOnly")]
[RequireVenuePermission(VenuePermissions.CreateBookings)]
public async Task<IActionResult> CreateBooking(CreateBookingCommand command)
{
    // Implementation
}
```

### 2. Input Validation
- Always validate input at the Application layer
- Use FluentValidation for complex validation rules
- Sanitize user input appropriately

### 3. Data Protection
- Use value objects for sensitive data
- Implement proper encryption for stored sensitive data
- Follow OWASP guidelines

## Error Handling

### 1. Domain Exceptions
```csharp
public class VenueBookingException : DomainException
{
    public VenueBookingException(string message) : base(message) { }
}
```

### 2. Application Result Pattern
```csharp
// Success
return ApplicationResult<T>.Success(data);

// Failure
return ApplicationResult<T>.Failure("Error message");

// With validation errors
return ApplicationResult<T>.Failure("Validation failed", validationErrors);
```

### 3. Global Exception Handling
- Use `GlobalExceptionMiddleware` for unhandled exceptions
- Log all errors appropriately
- Return consistent error responses

## Best Practices

### 1. Naming Conventions
- **Commands**: `CreateVenueCommand`, `UpdateVenueProfileCommand`
- **Queries**: `GetVenuesQuery`, `GetVenueByIdQuery`
- **Handlers**: `CreateVenueHandler`, `GetVenuesHandler`
- **Validators**: `CreateVenueValidator`, `UpdateVenueValidator`

### 2. File Organization
- One class per file
- Group related files in feature folders
- Follow established folder structure

### 3. Dependency Injection
- Register services in appropriate DI containers
- Use appropriate lifetimes (Transient, Scoped, Singleton)
- Follow dependency flow rules

### 4. Documentation
- Document public APIs with XML comments
- Include examples in complex methods
- Maintain architectural decision records

## Common Pitfalls

### 1. ❌ Don't Access Database Directly in Controllers
```csharp
// WRONG
public async Task<IActionResult> GetVenues()
{
    var venues = await _context.Venues.ToListAsync(); // Direct DB access
    return Ok(venues);
}

// CORRECT
public async Task<IActionResult> GetVenues()
{
    var result = await _mediator.Send(new GetVenuesQuery());
    return Ok(result);
}
```

### 2. ❌ Don't Reference Infrastructure in Domain
```csharp
// WRONG - Domain depending on Infrastructure
public class Venue : BaseEntity
{
    public void SaveToDatabase(AppDbContext context) // Infrastructure dependency
    {
        context.Venues.Add(this);
    }
}

// CORRECT - Use repository pattern
public interface IVenueRepository
{
    Task<Venue> AddAsync(Venue venue);
}
```

### 3. ❌ Don't Use Exceptions for Business Logic Flow
```csharp
// WRONG
public async Task<VenueDto> GetVenue(Guid id)
{
    var venue = await _repository.GetByIdAsync(id);
    if (venue == null)
        throw new NotFoundException("Venue not found"); // Exception for control flow
    return _mapper.Map<VenueDto>(venue);
}

// CORRECT
public async Task<ApplicationResult<VenueDto>> GetVenue(Guid id)
{
    var venue = await _repository.GetByIdAsync(id);
    if (venue == null)
        return ApplicationResult<VenueDto>.Failure("Venue not found");
    
    var dto = _mapper.Map<VenueDto>(venue);
    return ApplicationResult<VenueDto>.Success(dto);
}
```

### 4. ❌ Don't Skip Validation
```csharp
// WRONG - No validation
public record CreateVenueCommand(string Name) : IRequest<ApplicationResult<Guid>>;

// CORRECT - With validation
public record CreateVenueCommand(string Name) : IRequest<ApplicationResult<Guid>>;

public class CreateVenueValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

## Migration from Legacy Code

When migrating existing controllers or services:

1. **Identify the business operation**
2. **Create appropriate Command/Query**
3. **Implement validator if needed**
4. **Create handler with business logic**
5. **Update controller to use MediatR**
6. **Add tests**
7. **Update dependency injection**

## Conclusion

Following these guidelines ensures:
- ✅ Consistent code structure
- ✅ Maintainable and testable code
- ✅ Proper separation of concerns
- ✅ Adherence to Clean Architecture principles
- ✅ Scalable and performance-optimized solutions

For questions or clarifications, consult the E7GEZLY architecture team or refer to the Clean Architecture documentation.

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-08  
**Next Review**: Monthly  
**Maintained By**: E7GEZLY Architecture Team