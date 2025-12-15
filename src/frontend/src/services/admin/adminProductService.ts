import { adminApi, adminPath } from './_base';
import type { PagedResult } from '../../types/admin/AdminResponse';
import type {
  AdminProductCreateResponse,
  AdminProductDetail,
  AdminProductListItem,
  AdminProductUpsertRequest,
  AdminProductUpdateResponse,
} from '../../types/admin/Product';

export type ListAdminProductsParams = {
  search?: string;
  category?: string;
  isDeleted?: boolean;
  lowStockOnly?: boolean;
  skip?: number;
  take?: number;
};

export async function listProducts(params: ListAdminProductsParams): Promise<PagedResult<AdminProductListItem>> {
  const response = await adminApi.get<PagedResult<AdminProductListItem>>(adminPath.products, { params });
  return response.data;
}

export async function getProductDetail(productId: number): Promise<AdminProductDetail> {
  const response = await adminApi.get<AdminProductDetail>(`${adminPath.products}/${productId}`);
  return response.data;
}

export async function createProduct(request: AdminProductUpsertRequest): Promise<AdminProductCreateResponse> {
  const response = await adminApi.post<AdminProductCreateResponse>(adminPath.products, request);
  return response.data;
}

export async function updateProduct(productId: number, request: AdminProductUpsertRequest): Promise<AdminProductUpdateResponse> {
  const response = await adminApi.put<AdminProductUpdateResponse>(`${adminPath.products}/${productId}`, request);
  return response.data;
}

export async function deleteProduct(productId: number): Promise<void> {
  await adminApi.delete(`${adminPath.products}/${productId}`);
}
