import { adminApi, adminPath } from './_base';
import type { PagedResult } from '../../types/admin/AdminResponse';
import type { AdminUserDetail, AdminUserListItem } from '../../types/admin/User';

export async function listUsers(params: {
  email?: string;
  role?: string;
  isActive?: boolean;
  skip?: number;
  take?: number;
}): Promise<PagedResult<AdminUserListItem>> {
  const response = await adminApi.get<PagedResult<AdminUserListItem>>(adminPath.users, { params });
  return response.data;
}

export async function getUserDetail(userId: string): Promise<AdminUserDetail> {
  const response = await adminApi.get<AdminUserDetail>(`${adminPath.users}/${userId}`);
  return response.data;
}

export async function deactivateUser(userId: string, reason?: string): Promise<{ userId: string; isActive: boolean }> {
  const response = await adminApi.patch(`${adminPath.users}/${userId}/deactivate`, { reason });
  return response.data;
}

export async function reactivateUser(userId: string): Promise<{ userId: string; isActive: boolean }> {
  const response = await adminApi.patch(`${adminPath.users}/${userId}/reactivate`);
  return response.data;
}
