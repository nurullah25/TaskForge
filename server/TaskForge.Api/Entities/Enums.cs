namespace TaskForge.Api.Entities;

// Enums are stored as ints. Values are explicit so reordering members never changes stored data.
// Role values are ordered by power, which lets permission checks use >= comparisons.

public enum OrganizationRole
{
    Member = 1,
    Admin = 2,
    Owner = 3
}

public enum ProjectRole
{
    Viewer = 1,
    Contributor = 2,
    Manager = 3
}

public enum ProjectStatus
{
    Active = 1,
    OnHold = 2,
    Completed = 3,
    Archived = 4
}

public enum ColumnCategory
{
    ToDo = 1,
    InProgress = 2,
    Done = 3
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum ActivityType
{
    TaskCreated = 1,
    TaskAssigned = 2,
    StatusChanged = 3,
    PriorityChanged = 4,
    DueDateChanged = 5,
    TitleChanged = 6,
    CommentAdded = 7,
    AttachmentAdded = 8,
    TaskDeleted = 9
}

public enum NotificationType
{
    TaskAssigned = 1,
    CommentAdded = 2,
    AddedToProject = 3
}
