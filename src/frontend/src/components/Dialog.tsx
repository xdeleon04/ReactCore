import React, { useEffect, useId, useMemo, useRef } from 'react'

type DialogSize = 'sm' | 'md' | 'lg'

export type DialogProps = {
  open: boolean
  title: string
  description?: string
  size?: DialogSize
  className?: string
  onClose?: () => void
  onOpenChange?: (open: boolean) => void
  children?: React.ReactNode
}

type DisabledRecord = {
  element: HTMLElement
  disabled: boolean | null
  tabIndex: number | null
  ariaDisabled: string | null
}

let openDialogCount = 0
let disabledOutsideRecords: DisabledRecord[] = []

function cn(...values: Array<string | undefined | false>) {
  return values.filter(Boolean).join(' ')
}

function isFocusable(element: HTMLElement) {
  const tagName = element.tagName.toLowerCase()

  if (element.hasAttribute('disabled')) return false
  if (element.getAttribute('aria-disabled') === 'true') return false
  if (element.getAttribute('tabindex') === '-1') return false

  if (['button', 'input', 'select', 'textarea'].includes(tagName)) return true
  if (tagName === 'a' && (element as HTMLAnchorElement).href) return true

  return element.hasAttribute('tabindex')
}

function getFocusableElements(container: HTMLElement) {
  const candidates = Array.from(
    container.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    )
  )

  return candidates.filter(isFocusable)
}

function disableOutsideInteractions(dialogRoot: HTMLElement) {
  const interactive = Array.from(
    document.querySelectorAll<HTMLElement>('button, input, select, textarea, a')
  )

  const records: DisabledRecord[] = []

  for (const element of interactive) {
    if (dialogRoot.contains(element)) continue

    const tag = element.tagName.toLowerCase()

    if (tag === 'button' || tag === 'input' || tag === 'select' || tag === 'textarea') {
      const elAny = element as unknown as { disabled: boolean }
      records.push({
        element,
        disabled: typeof elAny.disabled === 'boolean' ? elAny.disabled : null,
        tabIndex: element.getAttribute('tabindex') ? element.tabIndex : null,
        ariaDisabled: element.getAttribute('aria-disabled'),
      })

      elAny.disabled = true
      continue
    }

    // Anchors and other tabbables: remove from tab order
    records.push({
      element,
      disabled: null,
      tabIndex: element.getAttribute('tabindex') ? element.tabIndex : null,
      ariaDisabled: element.getAttribute('aria-disabled'),
    })

    element.setAttribute('aria-disabled', 'true')
    element.tabIndex = -1
  }

  disabledOutsideRecords = records
}

function restoreOutsideInteractions() {
  for (const record of disabledOutsideRecords) {
    const tag = record.element.tagName.toLowerCase()

    if (tag === 'button' || tag === 'input' || tag === 'select' || tag === 'textarea') {
      const elAny = record.element as unknown as { disabled: boolean }
      if (record.disabled !== null) {
        elAny.disabled = record.disabled
      }

      continue
    }

    if (record.ariaDisabled === null) record.element.removeAttribute('aria-disabled')
    else record.element.setAttribute('aria-disabled', record.ariaDisabled)

    if (record.tabIndex === null) record.element.removeAttribute('tabindex')
    else record.element.tabIndex = record.tabIndex
  }

  disabledOutsideRecords = []
}

export default function Dialog({
  open,
  title,
  description,
  size = 'md',
  className,
  onClose,
  onOpenChange,
  children,
}: DialogProps) {
  const wrapperRef = useRef<HTMLDivElement | null>(null)
  const dialogRef = useRef<HTMLDivElement | null>(null)
  const previouslyFocusedRef = useRef<HTMLElement | null>(null)
  const prevOpenRef = useRef<boolean>(open)

  const reactId = useId()

  const ids = useMemo(() => {
    const safeId = reactId.replace(/:/g, '')
    const base = `dialog-${safeId}`
    return {
      titleId: `${base}-title`,
      descriptionId: `${base}-description`,
    }
  }, [reactId])

  useEffect(() => {
    if (prevOpenRef.current !== open) {
      onOpenChange?.(open)
      prevOpenRef.current = open
    }
  }, [open, onOpenChange])

  useEffect(() => {
    if (!open) return

    previouslyFocusedRef.current = document.activeElement as HTMLElement | null

    // Disable outside interactions on first open dialog.
    openDialogCount += 1
    if (openDialogCount === 1 && wrapperRef.current) {
      disableOutsideInteractions(wrapperRef.current)
    }

    // Focus management
    queueMicrotask(() => {
      const dialogEl = dialogRef.current
      if (!dialogEl) return

      const autoFocusEl = dialogEl.querySelector<HTMLElement>('[autofocus]')
      if (autoFocusEl) {
        autoFocusEl.focus()
        return
      }

      const focusables = getFocusableElements(dialogEl)
      if (focusables.length > 0) {
        focusables[0].focus()
        return
      }

      dialogEl.focus()
    })

    return () => {
      openDialogCount = Math.max(0, openDialogCount - 1)
      if (openDialogCount === 0) {
        restoreOutsideInteractions()
      }

      // Restore focus best-effort
      previouslyFocusedRef.current?.focus?.()
    }
  }, [open])

  useEffect(() => {
    if (!open) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (!dialogRef.current) return

      if (event.key === 'Escape') {
        event.preventDefault()
        onClose?.()
        onOpenChange?.(false)
        return
      }

      if (event.key !== 'Tab') return

      const focusables = getFocusableElements(dialogRef.current)
      if (focusables.length === 0) return

      const first = focusables[0]
      const last = focusables[focusables.length - 1]
      const active = document.activeElement as HTMLElement | null

      if (event.shiftKey) {
        if (active === first || !active || !dialogRef.current.contains(active)) {
          event.preventDefault()
          last.focus()
        }
        return
      }

      if (active === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open, onClose, onOpenChange])

  if (!open) return null

  const sizeClass: Record<DialogSize, string> = {
    sm: 'max-w-sm',
    md: 'max-w-lg',
    lg: 'max-w-2xl',
  }

  return (
    <div
      ref={wrapperRef}
      className="fixed inset-0 z-50 flex items-center justify-center"
    >
      <div
        data-testid="dialog-backdrop"
        className="absolute inset-0 bg-black/50"
        onClick={() => {
          onClose?.()
          onOpenChange?.(false)
        }}
      />

      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={ids.titleId}
        aria-describedby={description ? ids.descriptionId : undefined}
        tabIndex={-1}
        className={cn(
          'relative w-full rounded-lg bg-white p-4 shadow-lg',
          'max-h-[80vh] overflow-auto',
          'transition duration-300 ease-in-out',
          sizeClass[size],
          className
        )}
      >
        <div>
          <h2 id={ids.titleId} className="text-lg font-semibold text-slate-900">
            {title}
          </h2>
          {description ? (
            <p id={ids.descriptionId} className="mt-1 text-sm text-slate-600">
              {description}
            </p>
          ) : null}
        </div>

        <div className="mt-4">{children}</div>

        <button
          type="button"
          aria-label="Close"
          className="absolute right-3 top-3 inline-flex h-9 w-9 items-center justify-center rounded-md text-slate-700 transition duration-300 hover:bg-slate-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500/50"
          onClick={() => {
            onClose?.()
            onOpenChange?.(false)
          }}
        >
          ×
        </button>
      </div>
    </div>
  )
}
