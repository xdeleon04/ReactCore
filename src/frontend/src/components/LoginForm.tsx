import React, { useState } from 'react';
import { validateEmail, validatePassword } from '../utils/validation';
import { useAuth } from '../hooks/useAuth';

export const LoginForm: React.FC = () => {
  const { login, isLoading, error: apiError } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [emailError, setEmailError] = useState('');
  const [passwordError, setPasswordError] = useState('');

  const isFormValid = validateEmail(email) && validatePassword(password).isValid;

  const handleEmailChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setEmail(val);
    if (val && !validateEmail(val)) {
      setEmailError('Invalid email format.');
    } else {
      setEmailError('');
    }
  };

  const handlePasswordChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setPassword(val);
    const validation = validatePassword(val);
    if (val && !validation.isValid) {
      setPasswordError(validation.message || 'Invalid password.');
    } else {
      setPasswordError('');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isFormValid) return;

    try {
      await login(email, password);
      console.log('Login successful');
    } catch (err) {
      console.error('Login failed', err);
    }
  };

  return (
    <div className="login-form-container">
      <h2>Login</h2>
      {apiError && <div className="error-message" role="alert">{apiError}</div>}
      <form onSubmit={handleSubmit} noValidate>
        <div className="form-group">
          <label htmlFor="email">Email</label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={handleEmailChange}
            disabled={isLoading}
            aria-invalid={!!emailError}
            aria-describedby={emailError ? "email-error" : undefined}
          />
          {emailError && <span id="email-error" className="error-text">{emailError}</span>}
        </div>

        <div className="form-group">
          <label htmlFor="password">Password</label>
          <input
            type="password"
            id="password"
            value={password}
            onChange={handlePasswordChange}
            disabled={isLoading}
            aria-invalid={!!passwordError}
            aria-describedby={passwordError ? "password-error" : undefined}
          />
          {passwordError && <span id="password-error" className="error-text">{passwordError}</span>}
        </div>

        <button type="submit" disabled={!isFormValid || isLoading}>
          {isLoading ? 'Logging in...' : 'Login'}
        </button>
      </form>
    </div>
  );
};
