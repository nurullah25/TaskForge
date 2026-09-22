namespace TaskForge.Api.Common;

// Thrown from services and turned into the matching HTTP status by ErrorHandler.
// This keeps controllers free of repetitive "if null return NotFound()" code.

public class BadRequestException(string message) : Exception(message);

public class ForbiddenException(string message = "You don't have permission to do this.") : Exception(message);

public class NotFoundException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);
