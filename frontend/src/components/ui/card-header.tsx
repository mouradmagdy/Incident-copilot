import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function CardHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('flex min-h-9 items-center justify-between px-3 py-2 text-[10px] text-text-strong', className)}
      {...props}
    />
  )
}
