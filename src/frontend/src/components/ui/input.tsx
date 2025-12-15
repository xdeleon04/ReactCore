import React from 'react'

import { cn } from '@/lib/utils'

export type InputProps = React.InputHTMLAttributes<HTMLInputElement>

/** Input component used for forms and filters. */
export const Input = React.forwardRef<HTMLInputElement, InputProps>(function Input(
  { className, type = 'text', ...props },
  ref
) {
  return (
    <input
      ref={ref}
      type={type}
      className={cn(
        'w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900',
        'transition duration-300 ease-in-out',
        'placeholder:text-slate-400',
        'focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/10 focus-visible:border-blue-600',
        'aria-[invalid=true]:border-red-600',
        'disabled:cursor-not-allowed disabled:bg-slate-100 disabled:opacity-50',
        className
      )}
      {...props}
    />
  )
})

Input.displayName = 'Input'
