export interface TopProduct {
  productId: number;
  productName: string;
  unitsSold: number;
  revenue: number;
}

export interface OrderStatusBreakdown {
  status: string;
  count: number;
  percentage: number;
}

export interface SalesReport {
  dateRange: {
    start: string;
    end: string;
  };
  summary: {
    totalRevenue: number;
    totalOrders: number;
    averageOrderValue: number;
    totalItemsSold: number;
    uniqueCustomers: number;
  };
  topProducts: TopProduct[];
  orderStatusBreakdown: OrderStatusBreakdown[];
  generatedAt: string;
}
