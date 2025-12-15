import React, { useMemo } from 'react'
import { Toaster, toast as hotToast } from 'react-hot-toast'
import type { Toast } from 'react-hot-toast'

import type { ToastMessage } from '@/components/ui/toast'
import { ToastContext, type ToastApi } from '@/context/toast-context'

function ToastBody({ title, description }: ToastMessage) {
  return (
    <div className="min-w-0">
      <div className="truncate text-sm font-medium text-slate-900">{title}</div>
      {description ? <div className="mt-1 text-sm text-slate-700">{description}</div> : null}
    </div>
  )
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const api = useMemo<ToastApi>(() => {
    const show: ToastApi['show'] = (variant, message) => {
      const borderClass =
        variant === 'success'
          ? 'border-l-green-600'
          : variant === 'error'
            ? 'border-l-red-600'
            : variant === 'warning'
              ? 'border-l-amber-600'
              : 'border-l-blue-600'

      hotToast.custom(
        (t: Toast) => (
          <div
            className={
              'pointer-events-auto w-[min(420px,calc(100vw-2rem))] rounded-lg border border-slate-200 bg-white p-3 shadow-lg ' +
              'border-l-4 ' +
              borderClass +
              (t.visible ? ' animate-enter' : ' animate-leave')
            }
            role="status"
            aria-live="polite"
          >
            <div className="flex items-start justify-between gap-3">
              <ToastBody title={message.title} description={message.description} />
              <button
                type="button"
                className="shrink-0 rounded-md px-2 py-1 text-sm text-slate-700 transition duration-300 hover:bg-slate-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50"
                onClick={() => hotToast.dismiss(t.id)}
              >
                Close
              </button>
            </div>
          </div>
        ),
        { duration: 4000 }
      )
    }

    return {
      show,
      success: message => show('success', message),
      error: message => show('error', message),
      info: message => show('info', message),
      warning: message => show('warning', message),
    }
  }, [])

  return (
    <ToastContext.Provider value={api}>
      {children}
      <Toaster position="top-right" />
    </ToastContext.Provider>
  )
}
