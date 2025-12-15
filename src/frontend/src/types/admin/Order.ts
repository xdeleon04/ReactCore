export type OrderStatus = 'Pending' | 'Processing' | 'Shipped' | 'Completed';

export interface AdminOrderListItem {
  id: number;
  orderNumber: string;
  customerEmail: string;
  total: number;
  status: OrderStatus | string;
  createdAt: string;
}

export interface AdminOrderItemDetail {
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface AdminOrderDetail {
  id: number;
  orderNumber: string;
  customerEmail: string;
  status: OrderStatus | string;
  createdAt: string;
  items: AdminOrderItemDetail[];
  subtotal: number;
  total: number;
  allowedStatusTransitions: string[];
}

export interface AdminOrderStatusChangeRequest {
  status: string;
  reason?: string;
}

export interface AdminOrderStatusChangeResponse {
  success: boolean;
  orderId: number;
  status: string;
  updatedAt: string;
}
