import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function CardHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('flex min-h-10 items-center justify-between px-3 py-[9px] text-[11px] text-text-strong', className)}
      {...props}
    />
  )
}
