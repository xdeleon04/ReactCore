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

type AxiosLikeError<TData> = {
  response?: {
    status?: number
    data?: TData
  }
}

export function isCreateOrderConflict(
  error: unknown
): error is { response: { status: number; data: CreateOrderConflictResponse } } {
  if (typeof error !== 'object' || error === null) return false
  if (!('response' in error)) return false

  const maybe = error as AxiosLikeError<CreateOrderConflictResponse>
  return maybe.response?.status === 409
}
