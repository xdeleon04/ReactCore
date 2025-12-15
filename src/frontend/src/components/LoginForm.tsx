import React, { useState } from 'react';
import { validateEmail, validatePassword } from '../utils/validation';
import { useAuth } from '../hooks/useAuth';
import { Button } from './ui/button';
import { Input } from './ui/input';

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
    <div className="space-y-4">
      {apiError ? (
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800" role="alert">
          {apiError}
        </div>
      ) : null}

      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        <div className="space-y-1">
          <label htmlFor="email" className="text-sm font-medium text-slate-900">
            Email
          </label>
          <Input
            type="email"
            id="email"
            value={email}
            onChange={handleEmailChange}
            disabled={isLoading}
            autoComplete="email"
            aria-invalid={!!emailError}
            aria-describedby={emailError ? 'email-error' : undefined}
          />
          {emailError ? (
            <div id="email-error" className="text-sm text-red-700">
              {emailError}
            </div>
          ) : null}
        </div>

        <div className="space-y-1">
          <label htmlFor="password" className="text-sm font-medium text-slate-900">
            Password
          </label>
          <Input
            type="password"
            id="password"
            value={password}
            onChange={handlePasswordChange}
            disabled={isLoading}
            autoComplete="current-password"
            aria-invalid={!!passwordError}
            aria-describedby={passwordError ? 'password-error' : undefined}
          />
          {passwordError ? (
            <div id="password-error" className="text-sm text-red-700">
              {passwordError}
            </div>
          ) : null}
        </div>

        <Button type="submit" className="w-full" disabled={!isFormValid} isLoading={isLoading} loadingText="Logging in...">
          Sign in
        </Button>
      </form>
    </div>
  );
};
