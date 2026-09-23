export type OrganizationRole = 'Owner' | 'Admin' | 'Member';

export const ORGANIZATION_ROLES: OrganizationRole[] = ['Owner', 'Admin', 'Member'];

export interface Organization {
  id: number;
  name: string;
  myRole: OrganizationRole;
  memberCount: number;
  projectCount: number;
}

export interface OrganizationMember {
  userId: number;
  fullName: string;
  email: string;
  role: OrganizationRole;
  joinedAt: string;
}

export function canManageOrganization(role: OrganizationRole | undefined): boolean {
  return role === 'Owner' || role === 'Admin';
}
