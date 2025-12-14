import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom'; // Assuming react-router-dom is used
import { LoginForm } from '../components/LoginForm';
import { useAuth } from '../hooks/useAuth';

export const LoginPage: React.FC = () => {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/');
    }
  }, [isAuthenticated, navigate]);

  return (
    <div className="page-container">
      <div className="login-page">
        <h1>Welcome Back</h1>
        <LoginForm />
      </div>
    </div>
  );
};
