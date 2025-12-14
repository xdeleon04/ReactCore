import React, { createContext, useState, useEffect, type ReactNode, useRef, useCallback } from 'react';
import { jwtDecode } from 'jwt-decode';
import api, { setAccessToken as setApiAccessToken, onLogout } from '../services/api';

interface User {
  email: string;
  id: string;
  role: string;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const refreshTimerRef = useRef<NodeJS.Timeout | null>(null);

  const clearRefreshTimer = () => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }
  };

  const logout = useCallback(async () => {
    try {
      await api.post('/auth/logout');
    } catch (e) {
      // Ignore logout errors
    }
    setApiAccessToken(null);
    setAccessToken(null);
    setUser(null);
    clearRefreshTimer();
  }, []);

  useEffect(() => {
    onLogout(logout);
  }, [logout]);

  const silentRefresh = useCallback(async () => {
    try {
      const response = await api.post('/auth/refresh');
      const { accessToken: newAccessToken } = response.data;
      setAccessToken(newAccessToken);
    } catch (e) {
      console.error("Silent refresh failed", e);
      logout();
    }
  }, [logout]);

  useEffect(() => {
    if (!accessToken) {
      clearRefreshTimer();
      return;
    }

    setApiAccessToken(accessToken);
    try {
      const decoded: any = jwtDecode(accessToken);
      setUser({
        email: decoded.email || decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"],
        id: decoded.sub || decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"],
        role: decoded.role || decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]
      });

      const exp = decoded.exp * 1000;
      const now = Date.now();
      const timeUntilExpiry = exp - now;
      const refreshTime = Math.max(0, timeUntilExpiry - 120000); // Refresh 2 mins before expiry

      clearRefreshTimer();
      refreshTimerRef.current = setTimeout(silentRefresh, refreshTime);
    } catch (e) {
      console.error("Invalid token", e);
      logout();
    }
  }, [accessToken, silentRefresh, logout]);

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await api.post('/auth/login', { email, password });
      const { accessToken: newAccessToken } = response.data;
      setAccessToken(newAccessToken);
    } catch (err: any) {
      const message = err.response?.data?.message || err.message || 'Login failed';
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  // Check for existing session on mount
  useEffect(() => {
    const initSession = async () => {
      try {
        await silentRefresh();
      } catch (e) {
        // No session
      } finally {
        setIsLoading(false);
      }
    };
    initSession();

    return () => {
      clearRefreshTimer();
    };
  }, [silentRefresh]);

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, error, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
