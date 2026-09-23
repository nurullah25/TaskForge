using System.ComponentModel.DataAnnotations;
using TaskForge.Api.Common;

namespace TaskForge.Api.Features.Comments;

public record CommentDto(
    int Id,
    MemberSummaryDto Author,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool CanEdit,
    bool CanDelete);

public class SaveCommentRequest
{
    [Required, MaxLength(4000)]
    public string Body { get; set; } = "";
}
