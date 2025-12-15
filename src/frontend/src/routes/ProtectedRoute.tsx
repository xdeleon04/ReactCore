import React, { type ReactNode, useContext } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { AdminContext } from '../state/admin/AdminContext';

interface ProtectedRouteProps {
  children?: ReactNode;
  requiredRole?: string | string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredRole }) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const needsAdmin =
    requiredRole === 'admin' || (Array.isArray(requiredRole) && requiredRole.map((r) => r.toLowerCase()).includes('admin'));
  const adminCtx = useContext(AdminContext);

  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole) {
    const userRole = (user?.role || '').toLowerCase();
    const requiredRoles = Array.isArray(requiredRole) ? requiredRole : [requiredRole];
    const required = requiredRoles.map((r) => r.toLowerCase());

    const allowed = needsAdmin ? Boolean(adminCtx?.isAdmin === true || userRole === 'admin') : required.includes(userRole);
    if (!allowed) {
      return <Navigate to="/dashboard" replace />;
    }
  }

  return children ? <>{children}</> : <Outlet />;
};
