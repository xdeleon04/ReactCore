import React from 'react'
import { NavLink, useLocation } from 'react-router-dom'

import { cn } from '@/lib/utils'
import { useAdmin } from '@/state/admin/useAdmin'

export const Sidebar: React.FC = () => {
  const location = useLocation()
  const { isAdmin } = useAdmin()
  const inAdmin = location.pathname === '/admin' || location.pathname.startsWith('/admin/')

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
          {isAdmin ? (
            <li className="pt-2">
              <div className="px-3 pb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">
                Admin
              </div>
              <ul className="space-y-1">
                <li>
                  <NavLink
                    to="/admin/dashboard"
                    className={({ isActive }) =>
                      cn(
                        'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                        isActive || (inAdmin && location.pathname === '/admin')
                          ? 'bg-slate-100 font-medium text-slate-900'
                          : 'text-slate-700'
                      )
                    }
                  >
                    Overview
                  </NavLink>
                </li>
                <li>
                  <NavLink
                    to="/admin/users"
                    className={({ isActive }) =>
                      cn(
                        'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                        isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                      )
                    }
                  >
                    Users
                  </NavLink>
                </li>
                <li>
                  <NavLink
                    to="/admin/products"
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
                    to="/admin/orders"
                    className={({ isActive }) =>
                      cn(
                        'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                        isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                      )
                    }
                  >
                    Orders
                  </NavLink>
                </li>
                <li>
                  <NavLink
                    to="/admin/reports"
                    className={({ isActive }) =>
                      cn(
                        'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                        isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                      )
                    }
                  >
                    Reports
                  </NavLink>
                </li>
                <li>
                  <NavLink
                    to="/admin/api-usage"
                    className={({ isActive }) =>
                      cn(
                        'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                        isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                      )
                    }
                  >
                    API Usage
                  </NavLink>
                </li>
                <li>
                  <NavLink
                    to="/admin/audit-logs"
                    className={({ isActive }) =>
                      cn(
                        'block rounded px-3 py-2 transition duration-300 hover:bg-slate-100',
                        isActive ? 'bg-slate-100 font-medium text-slate-900' : 'text-slate-700'
                      )
                    }
                  >
                    Audit Logs
                  </NavLink>
                </li>
              </ul>
            </li>
          ) : null}
        </ul>
      </nav>
    </aside>
  )
}
