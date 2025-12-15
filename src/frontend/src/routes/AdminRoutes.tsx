import { Navigate, Route } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { AdminDashboardPage } from '../pages/admin/AdminDashboard';
import { AdminUsersPage } from '../pages/admin/Users';
import { ProductsPage } from '../pages/admin/Products';
import { OrdersPage } from '../pages/admin/Orders';
import { ReportsPage } from '../pages/admin/Reports';
import { AuditLogsPage } from '../pages/admin/AuditLogs';
import { ApiUsagePage } from '../pages/admin/ApiUsage';

export const AdminRoutes = (
  <Route
    path="/admin"
    element={<ProtectedRoute requiredRole="admin" />}
  >
    <Route index element={<Navigate to="/admin/dashboard" replace />} />
    <Route path="dashboard" element={<AdminDashboardPage />} />
    <Route path="users" element={<AdminUsersPage />} />
    <Route path="products" element={<ProductsPage />} />
    <Route path="orders" element={<OrdersPage />} />
    <Route path="reports" element={<ReportsPage />} />
    <Route path="api-usage" element={<ApiUsagePage />} />
    <Route path="audit-logs" element={<AuditLogsPage />} />
  </Route>
);
