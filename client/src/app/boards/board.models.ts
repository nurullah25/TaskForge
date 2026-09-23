import { ProjectRole } from '../projects/project.models';
import { TaskCard } from '../tasks/task.models';

export type ColumnCategory = 'ToDo' | 'InProgress' | 'Done';

export const COLUMN_CATEGORIES: { value: ColumnCategory; label: string; hint: string }[] = [
  { value: 'ToDo', label: 'To do', hint: 'Work that has not started' },
  { value: 'InProgress', label: 'In progress', hint: 'Work being done right now' },
  { value: 'Done', label: 'Done', hint: 'Finished work, counted as completed' },
];

export interface BoardColumn {
  id: number;
  name: string;
  position: number;
  category: ColumnCategory;
  tasks: TaskCard[];
}

export interface Board {
  id: number;
  projectId: number;
  projectKey: string;
  projectName: string;
  name: string;
  myRole: ProjectRole;
  columns: BoardColumn[];
}

export interface BoardSummary {
  id: number;
  name: string;
}

export interface SaveColumnRequest {
  name: string;
  category: ColumnCategory;
}
