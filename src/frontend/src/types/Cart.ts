export interface AddToCartRequest {
  productId: number;
  quantity: number;
}

export interface AddToCartResponse {
  success: boolean;
  cartId: number;
  cartItemId: number;
  itemCount: number;
  subtotal: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

export interface UpdateCartItemResponse {
  success: boolean;
  cartId: number;
  itemId: number;
  newQuantity: number;
  newLineTotal: number;
  subtotal: number;
}

export interface CartItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  imageUrl?: string | null;
}

export interface Cart {
  id: number;
  userId: string;
  items: CartItem[];
  itemCount: number;
  subtotal: number;
  total: number;
  createdAt: string;
  updatedAt: string;
}
