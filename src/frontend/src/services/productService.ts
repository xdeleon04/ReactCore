import api from './api';
import type { ProductDetail, ProductListResponse, RelatedProduct } from '../types/Product';

export interface GetProductsParams {
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  skip?: number;
  take?: number;
  page?: number;
  pageSize?: number;
}

export async function getProducts(params: GetProductsParams): Promise<ProductListResponse> {
  const query: Record<string, string | number> = {};

  if (params.category) query.category = params.category;
  if (params.minPrice !== undefined) query.minPrice = params.minPrice;
  if (params.maxPrice !== undefined) query.maxPrice = params.maxPrice;

  if (params.skip !== undefined) query.skip = params.skip;
  if (params.take !== undefined) query.take = params.take;
  if (params.page !== undefined) query.page = params.page;
  if (params.pageSize !== undefined) query.pageSize = params.pageSize;

  const response = await api.get<ProductListResponse>('/products', { params: query });
  return response.data;
}

export async function getProductById(id: number): Promise<ProductDetail> {
  const response = await api.get<ProductDetail>(`/products/${id}`);
  return response.data;
}

export async function getRelatedProducts(productId: number, count = 4): Promise<RelatedProduct[]> {
  const response = await api.get<RelatedProduct[]>(`/products/${productId}/related`, { params: { count } });
  return response.data;
}
