import React from 'react'
import { NavLink } from 'react-router-dom'

import { cn } from '@/lib/utils'

export const Sidebar: React.FC = () => {
  return (
    <aside
      className="hidden w-60 shrink-0 border-r border-slate-200 bg-white lg:block"
      aria-label="Sidebar"
    >
      <nav className="p-4" aria-label="Sidebar navigation">
        <ul className="space-y-1 text-sm">
          <li>
            <NavLink
              to="/dashboard"
              className={({ isActive }) =>
                cn(
                  'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                  isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                )
              }
            >
              Dashboard
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/shop"
              className={({ isActive }) =>
                cn(
                  'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                  isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                )
              }
            >
              Products
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/checkout"
              className={({ isActive }) =>
                cn(
                  'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                  isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                )
              }
            >
              Checkout
            </NavLink>
          </li>
          <li>
            <NavLink
              to="/admin"
              className={({ isActive }) =>
                cn(
                  'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                  isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                )
              }
            >
              Admin
            </NavLink>
          </li>
        </ul>
      </nav>
    </aside>
  )
}
