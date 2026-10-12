import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('rounded-[9px] border border-border bg-surface shadow-[0_1px_2px_rgb(15_23_42/2%)]', className)}
      {...props}
    />
  )
}
