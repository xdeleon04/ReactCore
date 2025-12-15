import { adminApi, adminPath } from './_base';
import type { SalesReport } from '../../types/admin/SalesReport';

export type SalesReportParams = {
  startDate: string;
  endDate: string;
};

export async function getSalesReport(params: SalesReportParams): Promise<SalesReport> {
  const response = await adminApi.get<SalesReport>(adminPath.reports, { params });
  return response.data;
}

export async function exportSalesReportCsv(params: SalesReportParams): Promise<Blob> {
  const response = await adminApi.get<Blob>(adminPath.reportsExport, {
    params,
    responseType: 'blob',
  });
  return response.data;
}
