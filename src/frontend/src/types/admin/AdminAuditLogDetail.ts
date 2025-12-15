export interface AdminAuditLogDetail {
  id: number;
  adminId: string;
  adminEmail: string;
  action: string;
  entityType: string;
  entityId: string;
  oldValues?: string | null;
  newValues?: string | null;
  timestamp: string;
  ipAddress?: string | null;
  reason?: string | null;
}
