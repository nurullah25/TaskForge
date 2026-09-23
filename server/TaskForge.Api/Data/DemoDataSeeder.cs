using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskForge.Api.Entities;

namespace TaskForge.Api.Data;

// Fills an empty development database with a small, realistic workspace so the UI
// has something to show right after the first run. All demo users share one password.
public static class DemoDataSeeder
{
    public const string DemoPassword = "Demo@1234";

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Email == "sarah@example.com"))
            return;

        var hasher = new PasswordHasher<User>();
        User CreateUser(string email, string fullName)
        {
            var user = new User { Email = email, FullName = fullName };
            user.PasswordHash = hasher.HashPassword(user, DemoPassword);
            return user;
        }

        var sarah = CreateUser("sarah@example.com", "Sarah Khan");
        var daniel = CreateUser("daniel@example.com", "Daniel Reyes");
        var priya = CreateUser("priya@example.com", "Priya Nair");
        var tom = CreateUser("tom@example.com", "Tom Walsh");

        var organization = new Organization
        {
            Name = "Brightline Software",
            Members =
            [
                new() { User = sarah, Role = OrganizationRole.Owner },
                new() { User = daniel, Role = OrganizationRole.Admin },
                new() { User = priya, Role = OrganizationRole.Member },
                new() { User = tom, Role = OrganizationRole.Member }
            ]
        };

        var frontend = new Label { Name = "frontend", Color = "#3b82f6" };
        var backend = new Label { Name = "backend", Color = "#10b981" };
        var bug = new Label { Name = "bug", Color = "#ef4444" };

        var portal = new Project
        {
            Organization = organization,
            Key = "CP",
            Name = "Customer Portal",
            Description = "Self-service portal where customers track orders and manage their account.",
            Labels = [frontend, backend, bug],
            Members =
            [
                new() { User = sarah, Role = ProjectRole.Manager },
                new() { User = daniel, Role = ProjectRole.Manager },
                new() { User = priya, Role = ProjectRole.Contributor },
                new() { User = tom, Role = ProjectRole.Viewer }
            ]
        };

        var mobile = new Project
        {
            Organization = organization,
            Key = "MOB",
            Name = "Mobile App",
            Description = "iOS and Android companion app for the customer portal.",
            Members =
            [
                new() { User = daniel, Role = ProjectRole.Manager },
                new() { User = priya, Role = ProjectRole.Contributor },
                new() { User = tom, Role = ProjectRole.Contributor }
            ]
        };

        var portalBoard = Board.CreateWithDefaultColumns("Development");
        var mobileBoard = Board.CreateWithDefaultColumns("Sprint board");
        portal.Boards.Add(portalBoard);
        mobile.Boards.Add(mobileBoard);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tasks = new List<TaskItem>();

        TaskItem AddTask(Project project, BoardColumn column, string title, TaskPriority priority,
            User reporter, User? assignee, int? dueInDays, params Label[] labels)
        {
            var createdAt = DateTime.UtcNow.AddDays(-14 + project.TaskCounter);
            var task = new TaskItem
            {
                Project = project,
                Number = ++project.TaskCounter,
                Title = title,
                Priority = priority,
                Reporter = reporter,
                Assignee = assignee,
                DueDate = dueInDays.HasValue ? today.AddDays(dueInDays.Value) : null,
                Position = (column.Tasks.Count + 1) * 1000,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                CompletedAt = column.Category == ColumnCategory.Done ? createdAt.AddDays(3) : null,
                Labels = labels.Select(label => new TaskLabel { Label = label }).ToList()
            };
            column.Tasks.Add(task);
            tasks.Add(task);
            return task;
        }

        var (todo, inProgress, testing, done) =
            (portalBoard.Columns[0], portalBoard.Columns[1], portalBoard.Columns[2], portalBoard.Columns[3]);

        var loginPage = AddTask(portal, todo, "Build login page", TaskPriority.High, sarah, priya, 5, frontend);
        loginPage.Description = "Email and password form with validation, \"remember me\" and a link to password reset.";
        AddTask(portal, todo, "Create reports", TaskPriority.Medium, sarah, sarah, 14, backend);
        AddTask(portal, todo, "Add password reset email", TaskPriority.Low, daniel, sarah, 2, backend);
        var payments = AddTask(portal, inProgress, "Payment integration", TaskPriority.Urgent, sarah, daniel, -2, backend);
        payments.Description = "Card payments through the payment provider's hosted checkout. Needs webhook handling for refunds.";
        AddTask(portal, inProgress, "Order history page", TaskPriority.Medium, daniel, priya, 3, frontend);
        var orderApi = AddTask(portal, testing, "Order API", TaskPriority.High, sarah, daniel, 1, backend);
        AddTask(portal, testing, "Fix date format on invoices", TaskPriority.Low, tom, priya, null, bug, frontend);
        AddTask(portal, done, "User registration", TaskPriority.High, sarah, priya, -6, frontend, backend);
        AddTask(portal, done, "Set up CI pipeline", TaskPriority.Medium, daniel, daniel, null);

        var (mobileTodo, mobileInProgress, _, mobileDone) =
            (mobileBoard.Columns[0], mobileBoard.Columns[1], mobileBoard.Columns[2], mobileBoard.Columns[3]);

        AddTask(mobile, mobileTodo, "Push notifications for order updates", TaskPriority.Medium, daniel, daniel, 10);
        AddTask(mobile, mobileInProgress, "Offline cache for recent orders", TaskPriority.High, daniel, priya, 4);
        AddTask(mobile, mobileDone, "App skeleton and navigation", TaskPriority.Medium, daniel, tom, null);

        db.AddRange(portal, mobile);
        await db.SaveChangesAsync();

        var comments = new List<Comment>
        {
            new() { Task = payments, Author = daniel, Body = "Sandbox keys are in the team vault. Webhooks still need a public URL for local testing.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Task = payments, Author = priya, Body = "I can set up a tunnel for the webhook endpoint tomorrow morning.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Task = orderApi, Author = tom, Body = "Paging skips an order when two orders have the same timestamp.", CreatedAt = DateTime.UtcNow.AddHours(-20) },
            new() { Task = orderApi, Author = daniel, Body = "Good catch. Adding Id as a secondary sort key.", CreatedAt = DateTime.UtcNow.AddHours(-18) }
        };
        db.Comments.AddRange(comments);

        foreach (var task in tasks)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                ProjectId = task.ProjectId, Task = task, UserId = task.ReporterId,
                Type = ActivityType.TaskCreated, NewValue = task.Title, CreatedAt = task.CreatedAt
            });

            if (task.Assignee != null)
            {
                db.ActivityLogs.Add(new ActivityLog
                {
                    ProjectId = task.ProjectId, Task = task, UserId = task.ReporterId,
                    Type = ActivityType.TaskAssigned, NewValue = task.Assignee.FullName, CreatedAt = task.CreatedAt.AddMinutes(5)
                });
            }

            if (task.CompletedAt != null)
            {
                db.ActivityLogs.Add(new ActivityLog
                {
                    ProjectId = task.ProjectId, Task = task, UserId = task.AssigneeId ?? task.ReporterId,
                    Type = ActivityType.StatusChanged, OldValue = "Testing", NewValue = "Done", CreatedAt = task.CompletedAt.Value
                });
            }
        }

        foreach (var comment in comments)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                ProjectId = comment.Task.ProjectId, Task = comment.Task, User = comment.Author,
                Type = ActivityType.CommentAdded, CreatedAt = comment.CreatedAt
            });
        }

        db.Notifications.AddRange(
            new Notification
            {
                User = priya, Task = loginPage, Type = NotificationType.TaskAssigned,
                Message = $"Sarah Khan assigned you CP-{loginPage.Number}: {loginPage.Title}", CreatedAt = loginPage.CreatedAt
            },
            new Notification
            {
                User = daniel, Task = payments, Type = NotificationType.CommentAdded,
                Message = $"Priya Nair commented on CP-{payments.Number}: {payments.Title}", CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Notification
            {
                User = daniel, Task = orderApi, Type = NotificationType.CommentAdded, IsRead = true,
                Message = $"Tom Walsh commented on CP-{orderApi.Number}: {orderApi.Title}", CreatedAt = DateTime.UtcNow.AddHours(-20)
            });

        await db.SaveChangesAsync();
    }
}
