import api from './api';
import type { CreateOrderRequest, CreateOrderConflictResponse, Order } from '../types/Order';

export async function createOrder(request: CreateOrderRequest): Promise<Order> {
  const response = await api.post<Order>('/orders', request);
  return response.data;
}

export async function getOrder(orderNumber: string): Promise<Order> {
  const response = await api.get<Order>(`/orders/${encodeURIComponent(orderNumber)}`);
  return response.data;
}

export function isCreateOrderConflict(error: unknown): error is { response: { status: number; data: CreateOrderConflictResponse } } {
  return (
    typeof error === 'object' &&
    error !== null &&
    'response' in error &&
    typeof (error as any).response?.status === 'number' &&
    (error as any).response?.status === 409
  );
}
