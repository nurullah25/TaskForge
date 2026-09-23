namespace TaskForge.Api.Common;

// Minimal information about a person, used wherever a task, comment or member is shown.
public record MemberSummaryDto(int Id, string FullName, string Email);
