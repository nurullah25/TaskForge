export type ProjectRole = 'Manager' | 'Contributor' | 'Viewer';
export type ProjectStatus = 'Active' | 'OnHold' | 'Completed' | 'Archived';

export const PROJECT_ROLES: ProjectRole[] = ['Manager', 'Contributor', 'Viewer'];

export const PROJECT_STATUSES: { value: ProjectStatus; label: string }[] = [
  { value: 'Active', label: 'Active' },
  { value: 'OnHold', label: 'On hold' },
  { value: 'Completed', label: 'Completed' },
  { value: 'Archived', label: 'Archived' },
];

export interface Project {
  id: number;
  organizationId: number;
  key: string;
  name: string;
  description: string | null;
  status: ProjectStatus;
  myRole: ProjectRole;
  memberCount: number;
  openTaskCount: number;
  createdAt: string;
}

export interface ProjectBoard {
  id: number;
  name: string;
}

export interface ProjectDetails {
  id: number;
  organizationId: number;
  organizationName: string;
  key: string;
  name: string;
  description: string | null;
  status: ProjectStatus;
  myRole: ProjectRole;
  canDelete: boolean;
  createdAt: string;
  boards: ProjectBoard[];
}

export interface ProjectMember {
  userId: number;
  fullName: string;
  email: string;
  role: ProjectRole;
  addedAt: string;
}

export interface CreateProjectRequest {
  name: string;
  key: string;
  description: string | null;
}

export interface UpdateProjectRequest {
  name: string;
  description: string | null;
  status: ProjectStatus;
}
