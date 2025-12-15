import React from 'react'

import { cn } from '@/lib/utils'

/** Card container used to group related UI. */
export const Card = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(function Card(
  { className, ...props },
  ref
) {
  return (
    <div
      ref={ref}
      className={cn('rounded-lg border border-slate-200 bg-white shadow-sm', className)}
      {...props}
    />
  )
})

Card.displayName = 'Card'

/** Card header area, typically contains title/description. */
export const CardHeader = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(function CardHeader(
  { className, ...props },
  ref
) {
  return (
    <div
      ref={ref}
      className={cn('flex flex-col gap-1 border-b border-slate-200 px-6 py-4', className)}
      {...props}
    />
  )
})

CardHeader.displayName = 'CardHeader'

/** Card content area. */
export const CardContent = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(function CardContent(
  { className, ...props },
  ref
) {
  return (
    <div ref={ref} className={cn('px-4 py-4 sm:px-6', className)} {...props} />
  )
})

CardContent.displayName = 'CardContent'

/** Card footer area, typically for actions. */
export const CardFooter = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(function CardFooter(
  { className, ...props },
  ref
) {
  return (
    <div
      ref={ref}
      className={cn('flex items-center gap-2 border-t border-slate-200 px-6 py-4', className)}
      {...props}
    />
  )
})

CardFooter.displayName = 'CardFooter'

export const CardTitle = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(function CardTitle(
  { className, ...props },
  ref
) {
  return (
    <h2 ref={ref} className={cn('text-xl font-bold text-slate-900', className)} {...props} />
  )
})

CardTitle.displayName = 'CardTitle'

export const CardDescription = React.forwardRef<
  HTMLParagraphElement,
  React.HTMLAttributes<HTMLParagraphElement>
>(function CardDescription({ className, ...props }, ref) {
  return <p ref={ref} className={cn('text-sm text-slate-600', className)} {...props} />
})

CardDescription.displayName = 'CardDescription'
