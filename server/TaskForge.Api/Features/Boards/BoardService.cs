using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Features.Boards;

public class BoardService(AppDbContext db, AccessService access)
{
    public async Task<List<BoardSummaryDto>> GetForProjectAsync(int projectId)
    {
        await access.RequireProjectRoleAsync(projectId);

        return await db.Boards
            .Where(b => b.ProjectId == projectId)
            .OrderBy(b => b.Id)
            .Select(b => new BoardSummaryDto(b.Id, b.Name))
            .ToListAsync();
    }

    public async Task<BoardDto> GetAsync(int boardId)
    {
        var (_, myRole) = await RequireBoardAccessAsync(boardId);

        return await db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => new BoardDto(
                b.Id,
                b.ProjectId,
                b.Project.Key,
                b.Project.Name,
                b.Name,
                myRole,
                b.Columns
                    .OrderBy(c => c.Position)
                    .Select(c => new BoardColumnDto(c.Id, c.Name, c.Position, c.Category))
                    .ToList()))
            .SingleAsync();
    }

    public async Task<BoardDto> CreateAsync(int projectId, SaveBoardRequest request)
    {
        await access.RequireProjectRoleAsync(projectId, ProjectRole.Manager);

        var board = Board.CreateWithDefaultColumns(request.Name.Trim());
        board.ProjectId = projectId;
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        return await GetAsync(board.Id);
    }

    public async Task<BoardDto> UpdateAsync(int boardId, SaveBoardRequest request)
    {
        await RequireBoardAccessAsync(boardId, ProjectRole.Manager);

        var board = await db.Boards.SingleAsync(b => b.Id == boardId);
        board.Name = request.Name.Trim();
        await db.SaveChangesAsync();

        return await GetAsync(boardId);
    }

    public async Task DeleteAsync(int boardId)
    {
        var (projectId, _) = await RequireBoardAccessAsync(boardId, ProjectRole.Manager);

        if (await db.Boards.CountAsync(b => b.ProjectId == projectId) == 1)
            throw new ConflictException("A project needs at least one board.");

        if (await db.Tasks.AnyAsync(t => t.Column.BoardId == boardId))
            throw new ConflictException("Move or delete the tasks on this board before deleting it.");

        await db.Boards.Where(b => b.Id == boardId).ExecuteDeleteAsync();
    }

    public async Task<BoardColumnDto> AddColumnAsync(int boardId, SaveColumnRequest request)
    {
        await RequireBoardAccessAsync(boardId, ProjectRole.Manager);

        var lastPosition = await db.BoardColumns
            .Where(c => c.BoardId == boardId)
            .MaxAsync(c => (int?)c.Position) ?? 0;

        var column = new BoardColumn
        {
            BoardId = boardId,
            Name = request.Name.Trim(),
            Category = request.Category,
            Position = lastPosition + 1
        };

        db.BoardColumns.Add(column);
        await db.SaveChangesAsync();

        return ToDto(column);
    }

    public async Task<BoardColumnDto> UpdateColumnAsync(int columnId, SaveColumnRequest request)
    {
        var column = await FindColumnAsync(columnId, ProjectRole.Manager);

        column.Name = request.Name.Trim();
        column.Category = request.Category;
        await db.SaveChangesAsync();

        return ToDto(column);
    }

    // The client sends the full order rather than "moved column X to index 3", which keeps
    // the result predictable when two people reorder at the same time: the last save wins.
    public async Task<List<BoardColumnDto>> ReorderColumnsAsync(int boardId, ReorderColumnsRequest request)
    {
        await RequireBoardAccessAsync(boardId, ProjectRole.Manager);

        var columns = await db.BoardColumns.Where(c => c.BoardId == boardId).ToListAsync();
        var requestedIds = request.ColumnIds;

        if (requestedIds.Count != columns.Count || requestedIds.Distinct().Count() != requestedIds.Count
            || requestedIds.Any(id => columns.All(c => c.Id != id)))
        {
            throw new BadRequestException("Send every column of this board exactly once.");
        }

        for (var index = 0; index < requestedIds.Count; index++)
        {
            columns.Single(c => c.Id == requestedIds[index]).Position = index + 1;
        }

        await db.SaveChangesAsync();

        return columns.OrderBy(c => c.Position).Select(ToDto).ToList();
    }

    public async Task DeleteColumnAsync(int columnId)
    {
        var column = await FindColumnAsync(columnId, ProjectRole.Manager);

        if (await db.BoardColumns.CountAsync(c => c.BoardId == column.BoardId) == 1)
            throw new ConflictException("A board needs at least one column.");

        if (await db.Tasks.AnyAsync(t => t.ColumnId == columnId))
            throw new ConflictException("Move the tasks out of this column before deleting it.");

        db.BoardColumns.Remove(column);
        await db.SaveChangesAsync();
    }

    private async Task<(int ProjectId, ProjectRole Role)> RequireBoardAccessAsync(int boardId, ProjectRole minimum = ProjectRole.Viewer)
    {
        var projectId = await db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => (int?)b.ProjectId)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Board not found.");

        var role = await access.RequireProjectRoleAsync(projectId, minimum);
        return (projectId, role);
    }

    private async Task<BoardColumn> FindColumnAsync(int columnId, ProjectRole minimum)
    {
        var column = await db.BoardColumns.SingleOrDefaultAsync(c => c.Id == columnId)
            ?? throw new NotFoundException("Column not found.");

        await RequireBoardAccessAsync(column.BoardId, minimum);
        return column;
    }

    private static BoardColumnDto ToDto(BoardColumn column) =>
        new(column.Id, column.Name, column.Position, column.Category);
}
