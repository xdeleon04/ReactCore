import api from './api';
import type { InventoryStatusResponse, InventoryStatus } from '../types/Product';

export async function getInventoryStatus(productId: number): Promise<InventoryStatusResponse> {
  const response = await api.get<InventoryStatusResponse>(`/products/${productId}/inventory`);
  return response.data;
}

export async function subscribeToRestock(productId: number): Promise<void> {
  await api.post(`/products/${productId}/notify`);
}

export interface TriggeredNotification {
  productId: number;
  productName: string;
  stockQuantity: number;
  reorderLevel: number;
  status: InventoryStatus;
  triggeredAt: string;
}

export async function getPendingNotifications(): Promise<TriggeredNotification[]> {
  const response = await api.get<TriggeredNotification[]>('/notifications/pending');
  return response.data;
}
