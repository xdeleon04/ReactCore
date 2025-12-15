import { adminApi, adminPath } from './_base';
import type { AdminAction } from '../../types/admin/AdminAction';
import type { AdminAuditLogDetail } from '../../types/admin/AdminAuditLogDetail';

export interface ListAuditLogsParams {
  startDate?: string;
  endDate?: string;
  skip?: number;
  take?: number;
}

export interface PagedAdminActions {
  items: AdminAction[];
  total: number;
  skip: number;
  take: number;
}

export async function listAuditLogs(params: ListAuditLogsParams): Promise<PagedAdminActions> {
  const res = await adminApi.get<PagedAdminActions>(adminPath.auditLogs, { params });
  return res.data;
}

export async function getAuditLogDetail(auditLogId: number): Promise<AdminAuditLogDetail> {
  const res = await adminApi.get<AdminAuditLogDetail>(`${adminPath.auditLogs}/${auditLogId}`);
  return res.data;
}
