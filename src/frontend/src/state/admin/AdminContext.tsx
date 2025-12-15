import { createContext, type ReactNode, useMemo } from 'react';
import { useAuth } from '../../hooks/useAuth';

export type AdminContextType = {
  isAdmin: boolean;
};

// eslint-disable-next-line react-refresh/only-export-components
export const AdminContext = createContext<AdminContextType | undefined>(undefined);

export function AdminProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();

  const value = useMemo<AdminContextType>(() => {
    return { isAdmin: (user?.role || '').toLowerCase() === 'admin' };
  }, [user?.role]);

  return <AdminContext.Provider value={value}>{children}</AdminContext.Provider>;
}
