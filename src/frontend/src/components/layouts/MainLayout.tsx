import React from 'react'

import { Header } from '@/components/features/Header'
import { Sidebar } from '@/components/features/Sidebar'

export type MainLayoutProps = {
  headerRight?: React.ReactNode
  children: React.ReactNode
}

/**
 * MainLayout provides a consistent app shell with Header + Sidebar.
 * Pages should render inside the main content area.
 */
export const MainLayout: React.FC<MainLayoutProps> = ({ headerRight, children }) => {
  return (
    <div className="min-h-screen bg-slate-50">
      <Header right={headerRight} />
      <div className="mx-auto flex w-full max-w-6xl">
        <Sidebar />
        <main className="w-full px-4 py-6">{children}</main>
      </div>
    </div>
  )
}
