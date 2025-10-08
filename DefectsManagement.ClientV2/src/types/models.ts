// src/types/models.ts
export type Guid = string;

export type GlobalRole = 'Engineer' | 'Manager' | 'Observer' | 'Admin';

export interface CurrentUserDto {
  id: Guid;
  username: string;
  name: string;
  role: GlobalRole; // глобальная роль остается
}

export type DefectStatus = 'New' | 'InProgress' | 'InReview' | 'Closed' | 'Canceled';
export type DefectPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface DefectDto {
  id: Guid;
  projectId: Guid;
  title: string;
  description: string;
  status: DefectStatus;
  priority: DefectPriority;
  createdById: Guid;
  createdBy?: Pick<UserDto, 'id'|'username'|'name'>;
  assignedId?: Guid | null;
  assignedTo?: Pick<UserDto, 'id'|'username'|'name'> | null;
  dueDate?: string | null; // ISO (DateOnly → передаем как 'YYYY-MM-DD')
  createdAt: string; // ISO
  updatedAt: string; // ISO
  closedAt?: string | null;
  isDeleted: boolean;
  tags: string[]; // упрощенно; бэк хранит как DefectTag[], фронт может маппить в string[]
}

export interface CreateDefectDto {
  projectId: Guid;
  title: string;
  description: string;
  priority: DefectPriority;
  assignedId?: Guid | null;
  dueDate?: string | null;
  tags?: string[];
}

export interface UpdateDefectDto {
  title?: string;
  description?: string;
  priority?: DefectPriority;
  status?: DefectStatus;
  assignedId?: Guid | null;
  dueDate?: string | null;
  tags?: string[];
}

export interface DefectAttachmentDto {
  id: Guid;
  defectId: Guid;
  uploadedById: Guid;
  uploadedBy?: Pick<UserDto, 'id'|'username'|'name'>;
  fileName: string;
  contentType: string;
  size: number;
  storageKey: string;
  uploadedAt: string;
}

export interface DefectCommentDto {
  id: Guid;
  defectId: Guid;
  authorId: Guid;
  author?: Pick<UserDto, 'id'|'username'|'name'>;
  text: string;
  createdAt: string;
  isEdited: boolean;
  editedAt?: string | null;
}

export interface DefectHistoryRecordDto {
  id: Guid;
  defectId: Guid;
  type: string;
  actorId: Guid;
  actor?: Pick<UserDto,'id'|'username'|'name'>;
  occurredAt: string;
  payload: string; // JSON
}

export type ProjectRole = 'Engineer' | 'Manager' | 'Observer';

export interface ProjectDto {
  id: Guid;
  key: string;
  name: string;
  description?: string | null;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
}

export interface ProjectMemberDto {
  projectId: Guid;
  userId: Guid;
  role: ProjectRole;
  user?: UserDto;
}

export interface UserDto {
  id: Guid;
  name: string;
  username: string;
  role: GlobalRole;
}

export interface CreateUserModel {
  name: string;
  username: string;
  password: string;
  role: GlobalRole | string; // бэк принимает string
}
