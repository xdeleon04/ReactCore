export type InventoryStatus = "in-stock" | "low-stock" | "out-of-stock";

export interface ProductListItem {
  id: number;
  name: string;
  description?: string | null;
  price: number;
  category: string;
  imageUrl?: string | null;
  stockQuantity: number;
  reorderLevel: number;
  status: InventoryStatus;
}

export interface ProductListResponse {
  items: ProductListItem[];
  total: number;
  skip: number;
  take: number;
}

export type ProductSpecifications = Record<string, string | number | boolean | null>;

export interface RelatedProduct {
  id: number;
  name: string;
  price: number;
  imageUrl?: string | null;
  category: string;
}

export interface ProductDetail {
  id: number;
  name: string;
  description?: string | null;
  price: number;
  category: string;
  imageUrl?: string | null;
  imageUrls: string[];
  specifications: ProductSpecifications;
  stockQuantity: number;
  reorderLevel: number;
  status: InventoryStatus;
  relatedProducts: RelatedProduct[];
  createdAt: string;
  updatedAt: string;
}

export interface InventoryStatusResponse {
  productId: number;
  stockQuantity: number;
  status: InventoryStatus;
  reorderLevel: number;
  lastUpdated: string;
}
