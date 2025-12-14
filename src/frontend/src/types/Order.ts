export interface CreateOrderRequest {
  email: string;
  cartId: number;
}

export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  orderNumber: string;
  userId: string;
  email: string;
  subtotal: number;
  total: number;
  status: string;
  items: OrderItem[];
  createdAt: string;
  updatedAt?: string;
}

export interface OrderListItem {
  id: number;
  orderNumber: string;
  subtotal: number;
  total: number;
  status: string;
  itemCount: number;
  createdAt: string;
}

export interface OrderListResponse {
  items: OrderListItem[];
  total: number;
  skip: number;
  take: number;
}

export interface InventoryConflict {
  cartItemId: number;
  productId: number;
  productName: string;
  requestedQuantity: number;
  availableQuantity: number;
  action: "REJECT";
}

export interface CreateOrderConflictResponse {
  statusCode: 409;
  message: string;
  conflicts: InventoryConflict[];
}
