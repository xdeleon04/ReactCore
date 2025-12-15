export interface AdminProductListItem {
  id: number;
  name: string;
  price: number;
  category: string;
  stockQuantity: number;
  reorderLevel: number;
  isDeleted: boolean;
  updatedAt: string;
}

export interface AdminProductDetail {
  id: number;
  name: string;
  description?: string | null;
  price: number;
  category: string;
  imageUrl?: string | null;
  imageUrls: string[];
  specifications: Record<string, unknown>;
  stockQuantity: number;
  reorderLevel: number;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AdminProductUpsertRequest {
  name: string;
  description?: string | null;
  price: number;
  category: string;
  stockQuantity: number;
  reorderLevel?: number;
  specifications?: Record<string, unknown>;
  imageUrl?: string | null;
}

export interface AdminProductCreateResponse {
  id: number;
  name: string;
  price: number;
  category: string;
  stockQuantity: number;
  createdAt: string;
}

export interface AdminProductUpdateResponse {
  success: boolean;
  productId: number;
  updatedAt: string;
}
