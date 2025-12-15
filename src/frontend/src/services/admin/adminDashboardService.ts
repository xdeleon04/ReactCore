import { adminApi, adminPath } from './_base';
import type { DashboardSummary } from '../../types/admin/DashboardSummary';

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const res = await adminApi.get<DashboardSummary>(adminPath.dashboardSummary);
  return res.data;
}
