using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Boards;

[ApiController]
[Route("api")]
public class BoardColumnsController(BoardService boards) : ControllerBase
{
    [HttpPost("boards/{boardId:int}/columns")]
    public async Task<ActionResult<BoardColumnDto>> Add(int boardId, SaveColumnRequest request)
    {
        var column = await boards.AddColumnAsync(boardId, request);
        return Created($"/api/columns/{column.Id}", column);
    }

    [HttpPut("boards/{boardId:int}/columns/order")]
    public Task<List<BoardColumnDto>> Reorder(int boardId, ReorderColumnsRequest request) =>
        boards.ReorderColumnsAsync(boardId, request);

    [HttpPut("columns/{id:int}")]
    public Task<BoardColumnDto> Update(int id, SaveColumnRequest request) => boards.UpdateColumnAsync(id, request);

    [HttpDelete("columns/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await boards.DeleteColumnAsync(id);
        return NoContent();
    }
}
