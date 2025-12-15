import React, { forwardRef } from 'react'

type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'outline' | 'ghost'
type ButtonSize = 'sm' | 'md' | 'lg'

export type ButtonProps = Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'disabled'> & {
  variant?: ButtonVariant
  size?: ButtonSize
  disabled?: boolean
  isLoading?: boolean
  loadingText?: string
}

function cn(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(' ')
}

const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    variant = 'primary',
    size = 'md',
    disabled = false,
    isLoading = false,
    loadingText,
    type = 'button',
    className,
    children,
    onClick,
    ...props
  },
  ref
) {
  const isDisabled = disabled || isLoading

  const sizeClasses: Record<ButtonSize, string> = {
    sm: 'px-3 py-1 text-sm',
    md: 'px-4 py-2 text-base',
    lg: 'px-6 py-3 text-lg',
  }

  const variantClasses: Record<ButtonVariant, string> = {
    primary: 'bg-blue-600 text-white hover:bg-blue-700',
    secondary: 'bg-slate-700 text-white hover:bg-slate-800',
    danger: 'bg-red-600 text-white hover:bg-red-700',
    outline: 'border border-slate-300 text-slate-900 hover:bg-slate-50',
    ghost: 'text-slate-900 hover:bg-slate-100',
  }

  const handleClick: React.MouseEventHandler<HTMLButtonElement> = event => {
    if (isDisabled) return
    onClick?.(event)
  }

  return (
    <button
      ref={ref}
      disabled={isDisabled}
      aria-busy={isLoading ? 'true' : undefined}
      type={type}
      className={cn(
        'inline-flex items-center justify-center gap-2 rounded-md font-medium',
        'transition duration-300 ease-in-out focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50 focus-visible:ring-offset-2',
        'disabled:cursor-not-allowed disabled:opacity-50',
        sizeClasses[size],
        variantClasses[variant],
        className
      )}
      onClick={handleClick}
      {...props}
    >
      {isLoading ? (
        <span
          data-testid="button-spinner"
          aria-hidden="true"
          className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
        />
      ) : null}
      {isLoading ? (loadingText ?? 'Loading...') : children}
    </button>
  )
})

export default Button
