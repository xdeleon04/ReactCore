import { adminApi, adminPath } from './_base';
import type { PagedResult } from '../../types/admin/AdminResponse';
import type {
  AdminOrderDetail,
  AdminOrderListItem,
  AdminOrderStatusChangeRequest,
  AdminOrderStatusChangeResponse,
} from '../../types/admin/Order';

export type ListAdminOrdersParams = {
  orderNumber?: string;
  email?: string;
  status?: string;
  startDate?: string;
  endDate?: string;
  skip?: number;
  take?: number;
};

export async function listOrders(params: ListAdminOrdersParams): Promise<PagedResult<AdminOrderListItem>> {
  const response = await adminApi.get<PagedResult<AdminOrderListItem>>(adminPath.orders, { params });
  return response.data;
}

export async function getOrderDetail(orderId: number): Promise<AdminOrderDetail> {
  const response = await adminApi.get<AdminOrderDetail>(`${adminPath.orders}/${orderId}`);
  return response.data;
}

export async function updateOrderStatus(
  orderId: number,
  request: AdminOrderStatusChangeRequest,
): Promise<AdminOrderStatusChangeResponse> {
  const response = await adminApi.patch<AdminOrderStatusChangeResponse>(`${adminPath.orders}/${orderId}/status`, request);
  return response.data;
}
