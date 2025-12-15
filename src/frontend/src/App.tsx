import { BrowserRouter as Router, Routes, Route, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { AuthProvider } from './state/AuthContext';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { Shop } from './pages/Shop';
import { ProductDetailPage } from './pages/ProductDetail';
import { CheckoutPage } from './pages/Checkout';
import { OrderConfirmationPage } from './pages/OrderConfirmation';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { AdminRoutes } from './routes/AdminRoutes';
import { CartProvider } from './state/CartContext';
import { AdminProvider } from './state/admin/AdminContext';
import { CartBadge } from './components/CartBadge';
import { Cart } from './components/Cart';
import './App.css';
import { useCallback } from 'react';
import { NotificationListener } from './components/NotificationListener';
import { ErrorBoundary } from './components/ErrorBoundary';
import { ToastProvider } from './context/ToastContext';
import { MainLayout } from './components/layouts/MainLayout';

function AppShell() {
  const location = useLocation();
  const navigate = useNavigate();

  const openCart = useCallback(() => {
    navigate('/cart');
  }, [navigate]);

  const closeCart = useCallback(() => {
    if (location.pathname === '/cart') {
      if (window.history.length > 1) {
        navigate(-1);
      } else {
        navigate('/shop', { replace: true });
      }
      return;
    }
    navigate('/shop');
  }, [location.pathname, navigate]);

  const cartIsOpen = location.pathname === '/cart';

  return (
    <ErrorBoundary>
      <MainLayout headerRight={<CartBadge onClick={openCart} />}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/shop" element={<Shop />} />
          <Route path="/products/:id" element={<ProductDetailPage />} />

          {/* Deep-linkable cart route */}
          <Route path="/cart" element={<div />} />

          <Route
            path="/checkout"
            element={
              <ProtectedRoute>
                <CheckoutPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/orders/:orderNumber"
            element={
              <ProtectedRoute>
                <OrderConfirmationPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <DashboardPage />
              </ProtectedRoute>
            }
          />

          {AdminRoutes}

          <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>

        <Cart open={cartIsOpen} onClose={closeCart} />
      </MainLayout>
    </ErrorBoundary>
  );
}

function App() {
  return (
    <AuthProvider>
      <CartProvider>
        <AdminProvider>
          <ToastProvider>
            <NotificationListener />
            <Router>
              <AppShell />
            </Router>
          </ToastProvider>
        </AdminProvider>
      </CartProvider>
    </AuthProvider>
  );
}

export default App;
