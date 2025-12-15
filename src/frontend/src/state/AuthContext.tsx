import React, { createContext, useState, useEffect, type ReactNode, useRef, useCallback } from 'react';
import { jwtDecode } from 'jwt-decode';
import api, { refreshAccessToken, setAccessToken as setApiAccessToken, onLogout } from '../services/api';

type JwtClaims = {
  exp: number;
  email?: string;
  sub?: string;
  role?: string;
  [claim: string]: unknown;
};

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

// eslint-disable-next-line react-refresh/only-export-components
export const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const refreshFnRef = useRef<(() => void) | null>(null);

  const clearRefreshTimer = () => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }
  };

  const clearLocalSession = useCallback(() => {
    setApiAccessToken(null);
    setUser(null);
    clearRefreshTimer();
  }, []);

  const logout = useCallback(async () => {
    try {
      await api.post('/auth/logout');
    } catch {
      // Ignore logout errors
    }
    clearLocalSession();
  }, [clearLocalSession]);

  useEffect(() => {
    onLogout(logout);
  }, [logout]);

  const silentRefresh = useCallback(async () => {
    try {
      const newAccessToken = await refreshAccessToken();
      setApiAccessToken(newAccessToken);

      const decoded = jwtDecode<JwtClaims>(newAccessToken);
      const email = (decoded.email || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']) as
        | string
        | undefined;
      const id = (decoded.sub || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']) as
        | string
        | undefined;
      const role = (decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']) as
        | string
        | undefined;

      setUser({ email: email ?? '', id: id ?? '', role: role ?? '' });

      const expMs = decoded.exp * 1000;
      const now = Date.now();
      const timeUntilExpiry = expMs - now;
      const refreshTime = Math.max(0, timeUntilExpiry - 120000); // Refresh 2 mins before expiry

      clearRefreshTimer();
      refreshTimerRef.current = setTimeout(() => {
        refreshFnRef.current?.();
      }, refreshTime);
    } catch (e) {
      console.error('Silent refresh failed', e);
      clearLocalSession();
    }
  }, [clearLocalSession]);

  useEffect(() => {
    refreshFnRef.current = () => {
      void silentRefresh();
    };
  }, [silentRefresh]);

  const applyAccessToken = useCallback(
    (newAccessToken: string) => {
      setApiAccessToken(newAccessToken);
      try {
        const decoded = jwtDecode<JwtClaims>(newAccessToken);
        setUser({
          email: (decoded.email || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '') as string,
          id: (decoded.sub || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || '') as string,
          role: (decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '') as string,
        });

        const expMs = decoded.exp * 1000;
        const now = Date.now();
        const timeUntilExpiry = expMs - now;
        const refreshTime = Math.max(0, timeUntilExpiry - 120000); // Refresh 2 mins before expiry

        clearRefreshTimer();
        refreshTimerRef.current = setTimeout(() => {
          refreshFnRef.current?.();
        }, refreshTime);
      } catch (e) {
        console.error('Invalid token', e);
        clearLocalSession();
      }
    },
    [clearLocalSession]
  );

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await api.post('/auth/login', { email, password });
      const { accessToken: newAccessToken } = response.data;
      applyAccessToken(newAccessToken);
    } catch (err: unknown) {
      const maybeAxiosErr = err as { response?: { data?: { message?: string } }; message?: string };
      const message = maybeAxiosErr.response?.data?.message || maybeAxiosErr.message || 'Login failed';
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
      } catch {
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
