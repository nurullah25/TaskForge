using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskForge.Api.Common;
using TaskForge.Api.Data;
using TaskForge.Api.Features.Auth;
using TaskForge.Api.Features.Boards;
using TaskForge.Api.Features.Organizations;
using TaskForge.Api.Features.Projects;
using TaskForge.Api.Features.Activity;
using TaskForge.Api.Features.Attachments;
using TaskForge.Api.Features.Comments;
using TaskForge.Api.Features.Labels;
using TaskForge.Api.Features.Tasks;
using TaskForge.Api.Features.Notifications;
using TaskForge.Api.Realtime;
using TaskForge.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddScoped<AccessService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<BoardService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<LabelService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<BoardNotifier>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Every error response carries the request's trace id, which also appears in the logs.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier));
builder.Services.AddExceptionHandler<ErrorHandler>();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();
app.MapHub<AppHub>("/hubs/app");

await app.PrepareDatabaseAsync();

app.Run();

// Makes Program visible to WebApplicationFactory in the test project.
public partial class Program { }
