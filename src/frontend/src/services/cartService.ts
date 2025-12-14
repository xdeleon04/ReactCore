import api from './api';
import type {
  AddToCartRequest,
  AddToCartResponse,
  Cart,
  UpdateCartItemRequest,
  UpdateCartItemResponse,
} from '../types/Cart';

export async function getCart(): Promise<Cart> {
  const response = await api.get<Cart>('/carts/current');
  return response.data;
}

export async function addToCart(request: AddToCartRequest): Promise<AddToCartResponse> {
  const response = await api.post<AddToCartResponse>('/carts/items', request);
  return response.data;
}

export async function updateCartItem(itemId: number, request: UpdateCartItemRequest): Promise<UpdateCartItemResponse> {
  const response = await api.put<UpdateCartItemResponse>(`/carts/items/${itemId}`, request);
  return response.data;
}

export async function removeCartItem(itemId: number): Promise<void> {
  await api.delete(`/carts/items/${itemId}`);
}
