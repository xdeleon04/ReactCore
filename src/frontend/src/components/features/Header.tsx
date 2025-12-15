import React from 'react'
import { NavLink } from 'react-router-dom'

import { cn } from '@/lib/utils'

export type HeaderProps = {
  right?: React.ReactNode
}

export const Header: React.FC<HeaderProps> = ({ right }) => {
  return (
    <header className="sticky top-0 z-40 h-16 border-b border-slate-200 bg-white">
      <div className="mx-auto flex h-full max-w-6xl items-center justify-between gap-4 px-4">
        <div className="flex items-center gap-4">
          <NavLink to="/dashboard" className="text-sm font-semibold text-slate-900">
            ReactCore
          </NavLink>

          <nav className="hidden items-center gap-3 text-sm lg:flex" aria-label="Primary">
            <NavLink
              to="/dashboard"
              className={({ isActive }) =>
                cn(
                  'rounded px-2 py-1 transition duration-300 hover:bg-slate-100',
                  isActive ? 'bg-slate-100 text-slate-900' : 'text-slate-700'
                )
              }
            >
              Dashboard
            </NavLink>
            <NavLink
              to="/shop"
              className={({ isActive }) =>
                cn(
                  'rounded px-2 py-1 transition duration-300 hover:bg-slate-100',
                  isActive ? 'bg-slate-100 text-slate-900' : 'text-slate-700'
                )
              }
            >
              Products
            </NavLink>
            <NavLink
              to="/checkout"
              className={({ isActive }) =>
                cn(
                  'rounded px-2 py-1 transition duration-300 hover:bg-slate-100',
                  isActive ? 'bg-slate-100 text-slate-900' : 'text-slate-700'
                )
              }
            >
              Checkout
            </NavLink>
          </nav>
        </div>

        <div className="flex items-center gap-2">{right}</div>
      </div>
    </header>
  )
}
