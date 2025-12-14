import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './state/AuthContext';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { Shop } from './pages/Shop';
import { ProductDetailPage } from './pages/ProductDetail';
import { CheckoutPage } from './pages/Checkout';
import { OrderConfirmationPage } from './pages/OrderConfirmation';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { CartProvider } from './state/CartContext';
import { CartBadge } from './components/CartBadge';
import { Cart } from './components/Cart';
import { Toaster } from 'react-hot-toast';
import './App.css';
import { useState } from 'react';
import { NotificationListener } from './components/NotificationListener';
import { ErrorBoundary } from './components/ErrorBoundary';

function App() {
  const [cartOpen, setCartOpen] = useState(false);

  return (
    <AuthProvider>
      <CartProvider>
        <Toaster position="top-right" />
        <NotificationListener />
        <Router>
          <ErrorBoundary>
            <div className="app-container">
              <header className="flex items-center justify-between border-b border-gray-200 px-4 py-3">
                <div className="text-sm font-semibold text-gray-900">ReactCore</div>
                <CartBadge onClick={() => setCartOpen(true)} />
              </header>

              <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/shop" element={<Shop />} />
                <Route path="/products/:id" element={<ProductDetailPage />} />
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
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
              </Routes>

              <Cart open={cartOpen} onClose={() => setCartOpen(false)} />
            </div>
          </ErrorBoundary>
        </Router>
      </CartProvider>
    </AuthProvider>
  );
}

export default App;
