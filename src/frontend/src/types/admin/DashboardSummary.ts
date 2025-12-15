export interface DashboardSummary {
  userMetrics: {
    totalUsers: number;
    activeUsers: number;
    inactiveUsers: number;
    newUsersThisMonth: number;
  };
  productMetrics: {
    totalProducts: number;
    activeProducts: number;
    archivedProducts: number;
    lowStockCount: number;
  };
  orderMetrics: {
    pendingOrders: number;
    processingOrders: number;
    totalOrdersToday: number;
    todayRevenue: number;
  };
}
