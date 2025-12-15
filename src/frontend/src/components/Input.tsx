import { forwardRef, useEffect, useId, useMemo, useState } from 'react'

type InputSize = 'sm' | 'md' | 'lg'

export type InputProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, 'size'> & {
  label?: string
  description?: string
  error?: string
  success?: boolean
  size?: InputSize
  onValueChange?: (value: string) => void
}

function cn(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(' ')
}

const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  {
    label,
    description,
    error,
    success = false,
    size = 'md',
    id,
    className,
    value,
    readOnly,
    minLength,
    maxLength,
    type,
    disabled,
    onChange,
    onValueChange,
    'aria-describedby': ariaDescribedBy,
    'aria-invalid': ariaInvalid,
    ...props
  },
  ref
) {
  const generatedId = useId()
  const inputId = id ?? generatedId

  const descriptionId = `${inputId}-description`
  const errorId = `${inputId}-error`

  const describedBy = [
    ariaDescribedBy,
    description ? descriptionId : undefined,
    error ? errorId : undefined,
  ]
    .filter(Boolean)
    .join(' ')

  const isInvalid = ariaInvalid === 'true' || Boolean(error)

  const initialValue = useMemo(() => {
    if (typeof value === 'string') return value
    return ''
  }, [value])

  const [uncontrolledValue, setUncontrolledValue] = useState<string>(initialValue)

  useEffect(() => {
    if (readOnly && typeof value === 'string') {
      // Keep readOnly inputs controlled.
      return
    }

    // For editable inputs, treat `value` as an initial value (uncontrolled behavior).
    setUncontrolledValue(initialValue)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialValue])

  const sizeClasses: Record<InputSize, string> = {
    sm: 'px-2 py-1 text-sm',
    md: 'px-3 py-2 text-base',
    lg: 'px-4 py-3 text-lg',
  }

  const handleChange: React.ChangeEventHandler<HTMLInputElement> = event => {
    const nextRaw = event.currentTarget.value
    const nextValue = typeof maxLength === 'number' ? nextRaw.slice(0, maxLength) : nextRaw

    if (typeof minLength === 'number' && minLength > 0) {
      if (nextValue.length > 0 && nextValue.length < minLength) {
        event.currentTarget.setCustomValidity('Too short')
      } else {
        event.currentTarget.setCustomValidity('')
      }
    }

    if (!readOnly) {
      setUncontrolledValue(nextValue)
    }

    onChange?.(event)
    onValueChange?.(nextValue)
  }

  return (
    <div className="w-full">
      {label ? (
        <label htmlFor={inputId} className="mb-1 block text-sm font-medium text-slate-900">
          {label}
        </label>
      ) : null}

      <input
        ref={ref}
        id={inputId}
        className={cn(
          'block w-full rounded-md border bg-white text-slate-900 placeholder:text-slate-400',
          'transition duration-300 ease-in-out',
          'focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/30 focus-visible:ring-offset-2',
          sizeClasses[size],
          isInvalid ? 'border-red-600' : success ? 'border-green-600' : 'border-slate-300',
          disabled ? 'cursor-not-allowed bg-slate-100 opacity-60' : 'hover:border-slate-400',
          className
        )}
        aria-invalid={isInvalid ? 'true' : ariaInvalid}
        aria-describedby={describedBy || undefined}
        role={type === 'number' || type === 'search' ? undefined : 'textbox'}
        type={type}
        disabled={disabled}
        readOnly={readOnly}
        minLength={minLength}
        maxLength={maxLength}
        value={readOnly ? (value as unknown as string) : uncontrolledValue}
        onChange={handleChange}
        {...props}
      />

      {description ? (
        <p id={descriptionId} className="mt-1 text-xs text-slate-500">
          {description}
        </p>
      ) : null}

      {error ? (
        <p id={errorId} className="mt-1 text-xs text-red-600">
          {error}
        </p>
      ) : null}
    </div>
  )
})

export default Input
