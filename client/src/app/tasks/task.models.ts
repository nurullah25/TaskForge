import { Label } from '../labels/label.models';
import { ProjectRole } from '../projects/project.models';

export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export const TASK_PRIORITIES: TaskPriority[] = ['Low', 'Medium', 'High', 'Urgent'];

export interface TaskMember {
  id: number;
  fullName: string;
  email: string;
}

export interface TaskCard {
  id: number;
  number: number;
  title: string;
  priority: TaskPriority;
  dueDate: string | null;
  columnId: number;
  position: number;
  assignee: TaskMember | null;
  labels: Label[];
  hasDescription: boolean;
  commentCount: number;
}

export interface TaskDetails {
  id: number;
  number: number;
  projectId: number;
  projectKey: string;
  boardId: number;
  columnId: number;
  columnName: string;
  position: number;
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueDate: string | null;
  assignee: TaskMember | null;
  reporter: TaskMember;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
  labels: Label[];
  myRole: ProjectRole;
  rowVersion: string;
}

export interface CreateTaskRequest {
  columnId: number;
  title: string;
  description: string | null;
  priority: TaskPriority;
  assigneeId: number | null;
  dueDate: string | null;
}

export interface UpdateTaskRequest {
  title: string;
  description: string | null;
  priority: TaskPriority;
  assigneeId: number | null;
  dueDate: string | null;
  rowVersion: string;
}

export interface MoveTaskRequest {
  columnId: number;
  aboveTaskId: number | null;
  belowTaskId: number | null;
}

export function taskKey(projectKey: string, number: number): string {
  return `${projectKey}-${number}`;
}

export function isOverdue(dueDate: string | null, completed = false): boolean {
  if (!dueDate || completed) {
    return false;
  }

  const today = new Date().toISOString().slice(0, 10);
  return dueDate < today;
}
