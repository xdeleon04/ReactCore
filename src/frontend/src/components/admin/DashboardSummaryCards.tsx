import React from 'react';
import type { DashboardSummary } from '../../types/admin/DashboardSummary';

export const DashboardSummaryCards: React.FC<{ summary: DashboardSummary }> = ({ summary }) => {
  return (
    <section aria-label="Dashboard summary" className="grid grid-cols-1 gap-4 md:grid-cols-3">
      <div className="rounded border border-gray-200 bg-white p-4">
        <h2 className="text-sm font-semibold text-gray-700">Users</h2>
        <dl className="mt-3 grid grid-cols-2 gap-3 text-sm text-gray-900">
          <div>
            <dt className="text-xs text-gray-600">Total</dt>
            <dd className="font-medium">{summary.userMetrics.totalUsers}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Active</dt>
            <dd className="font-medium">{summary.userMetrics.activeUsers}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Inactive</dt>
            <dd className="font-medium">{summary.userMetrics.inactiveUsers}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">New (month)</dt>
            <dd className="font-medium">{summary.userMetrics.newUsersThisMonth}</dd>
          </div>
        </dl>
      </div>

      <div className="rounded border border-gray-200 bg-white p-4">
        <h2 className="text-sm font-semibold text-gray-700">Products</h2>
        <dl className="mt-3 grid grid-cols-2 gap-3 text-sm text-gray-900">
          <div>
            <dt className="text-xs text-gray-600">Total</dt>
            <dd className="font-medium">{summary.productMetrics.totalProducts}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Active</dt>
            <dd className="font-medium">{summary.productMetrics.activeProducts}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Archived</dt>
            <dd className="font-medium">{summary.productMetrics.archivedProducts}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Low stock</dt>
            <dd className="font-medium">{summary.productMetrics.lowStockCount}</dd>
          </div>
        </dl>
      </div>

      <div className="rounded border border-gray-200 bg-white p-4">
        <h2 className="text-sm font-semibold text-gray-700">Orders</h2>
        <dl className="mt-3 grid grid-cols-2 gap-3 text-sm text-gray-900">
          <div>
            <dt className="text-xs text-gray-600">Pending</dt>
            <dd className="font-medium">{summary.orderMetrics.pendingOrders}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Processing</dt>
            <dd className="font-medium">{summary.orderMetrics.processingOrders}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Orders today</dt>
            <dd className="font-medium">{summary.orderMetrics.totalOrdersToday}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-600">Revenue today</dt>
            <dd className="font-medium">{summary.orderMetrics.todayRevenue.toFixed(2)}</dd>
          </div>
        </dl>
      </div>
    </section>
  );
};
