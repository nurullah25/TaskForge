using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TaskForge.Api.Common;

public static class DbUpdateExceptionExtensions
{
    // 2601 and 2627 are SQL Server's duplicate key errors (unique index and unique constraint).
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
