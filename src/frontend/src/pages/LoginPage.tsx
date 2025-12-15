import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { LoginForm } from '../components/LoginForm';
import { useAuth } from '../hooks/useAuth';

export const LoginPage: React.FC = () => {
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (isAuthenticated) {
      const role = (user?.role || '').toLowerCase();
      navigate(role === 'admin' ? '/admin/dashboard' : '/dashboard', { replace: true });
    }
  }, [isAuthenticated, user?.role, navigate]);

  return (
    <div className="page-container">
      <div className="login-page">
        <h1>Welcome Back</h1>
        <LoginForm />
      </div>
    </div>
  );
};
