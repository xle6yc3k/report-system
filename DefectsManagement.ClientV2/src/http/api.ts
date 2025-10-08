// src/http/api.ts
import axios from 'axios';
import type {
  CurrentUserDto, CreateDefectDto, DefectAttachmentDto, DefectCommentDto, DefectDto,
  DefectHistoryRecordDto, UpdateDefectDto, ProjectDto, ProjectMemberDto, CreateUserModel, Guid,
  DefectPriority, DefectStatus
} from '@/types/models';

let isLoggingOut = false;

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' }
});

api.interceptors.response.use(
  (r) => r,
  async (error) => {
    const url = error.config?.url || '';
    const status = error.response?.status;

    if (status === 401 && !url.endsWith('/Auth/login') && !url.endsWith('/Auth/logout')) {
      if (!isLoggingOut) {
        isLoggingOut = true;
        const { useAuthStore } = await import('@/stores/authStore.ts');
        const store = useAuthStore();
        store.forceLogout('expired');
        setTimeout(() => { isLoggingOut = false; }, 300);
      }
      return Promise.reject({ message: 'SESSION_EXPIRED', __silenced401: true });
    }
    return Promise.reject(error);
  }
);

// ===== Auth =====
export const AuthApi = {
  login: (payload: { username: string; password: string; }) =>
    api.post('/Auth/login', payload),
  logout: () => api.post('/Auth/logout'),
  // password-hash — вспомогательный, можно не прокидывать в UI
};

// ===== User =====
export const UserApi = {
  me: () => api.get<CurrentUserDto>('/User/me'),
  changePassword: (payload: { oldPassword: string; newPassword: string; confirmPassword: string; }) =>
    api.put('/User/me/password', payload),
  create: (payload: CreateUserModel) => api.post('/User', payload),
};

// ===== Defect (CRUD + partial updates) =====
export const DefectApi = {
  create: (payload: CreateDefectDto) => api.post<DefectDto>('/Defect', payload),
  list: () => api.get<DefectDto[]>('/Defect'),
  get: (id: Guid) => api.get<DefectDto>(`/Defect/${id}`),
  update: (id: Guid, payload: UpdateDefectDto) => api.put<DefectDto>(`/Defect/${id}`, payload),
  delete: (id: Guid) => api.delete<void>(`/Defect/${id}`),

  assign: (id: Guid, assignedId: Guid | null) =>
    api.patch<DefectDto>(`/Defect/${id}/assign`, { assignedId }),

  setStatus: (id: Guid, status: DefectStatus) =>
    api.patch<DefectDto>(`/Defect/${id}/status`, { status }),

  setPriority: (id: Guid, priority: DefectPriority) =>
    api.patch<DefectDto>(`/Defect/${id}/priority`, { priority }),

  setDueDate: (id: Guid, dueDate: string | null) =>
    api.patch<DefectDto>(`/Defect/${id}/due-date`, { dueDate }),

  setTags: (id: Guid, tags: string[]) =>
    api.put<DefectDto>(`/Defect/${id}/tags`, { tags }),
};

// ===== Attachments =====
export const AttachmentApi = {
  list: (defectId: Guid) =>
    api.get<DefectAttachmentDto[]>(`/Defect/${defectId}/attachments`),

  upload: (defectId: Guid, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return api.post<DefectAttachmentDto>(`/Defect/${defectId}/attachments`, form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
  },

  get: (defectId: Guid, fileId: Guid) =>
    api.get<Blob>(`/Defect/${defectId}/attachments/${fileId}`, { responseType: 'blob' }),

  delete: (defectId: Guid, fileId: Guid) =>
    api.delete<void>(`/Defect/${defectId}/attachments/${fileId}`),
};

// ===== Comments =====
export const CommentApi = {
  create: (defectId: Guid, text: string) =>
    api.post<DefectCommentDto>(`/Defect/${defectId}/comments`, { text }),

  list: (defectId: Guid) =>
    api.get<DefectCommentDto[]>(`/Defect/${defectId}/comments`),

  update: (defectId: Guid, commentId: Guid, text: string) =>
    api.put<DefectCommentDto>(`/Defect/${defectId}/comments/${commentId}`, { text }),

  delete: (defectId: Guid, commentId: Guid) =>
    api.delete<void>(`/Defect/${defectId}/comments/${commentId}`),
};

// ===== History =====
export const HistoryApi = {
  list: (defectId: Guid) =>
    api.get<DefectHistoryRecordDto[]>(`/Defect/${defectId}/history`),
};

// ===== Projects =====
export const ProjectApi = {
  list: () => api.get<ProjectDto[]>('/Project'),
  create: (payload: { key: string; name: string; description?: string | null }) =>
    api.post<ProjectDto>('/Project', payload),
  get: (id: Guid) => api.get<ProjectDto>(`/Project/${id}`),
  update: (id: Guid, payload: { key?: string; name?: string; description?: string | null }) =>
    api.put<ProjectDto>(`/Project/${id}`, payload),
  addMembers: (id: Guid, payload: { userId: Guid; role: 'Engineer'|'Manager'|'Observer' }[]) =>
    api.post<ProjectMemberDto[]>(`/Project/${id}/members`, payload),
  removeMember: (id: Guid, userId: Guid) =>
    api.delete<void>(`/Project/${id}/members/${userId}`),
};

export default api;
