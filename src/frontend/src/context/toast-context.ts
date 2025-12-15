import { createContext } from 'react'

import type { ToastMessage, ToastVariant } from '@/components/ui/toast'

type ToastApi = {
  show: (variant: ToastVariant, message: ToastMessage) => void
  success: (message: ToastMessage) => void
  error: (message: ToastMessage) => void
  info: (message: ToastMessage) => void
  warning: (message: ToastMessage) => void
}

export const ToastContext = createContext<ToastApi | null>(null)
export type { ToastApi }
