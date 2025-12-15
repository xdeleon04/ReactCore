import { adminApi, adminPath } from './_base';
import type { ApiQuotaStatus } from '../../types/admin/ApiUsage';

export async function getApiUsage(): Promise<ApiQuotaStatus> {
  const res = await adminApi.get<ApiQuotaStatus>(adminPath.apiUsage);
  return res.data;
}
