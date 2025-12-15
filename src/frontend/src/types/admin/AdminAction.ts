export interface AdminAction {
  id: number;
  adminEmail: string;
  action: string;
  entityType: string;
  entityId: string;
  timestamp: string;
  oldValues?: string | null;
  newValues?: string | null;
  reason?: string | null;
}
