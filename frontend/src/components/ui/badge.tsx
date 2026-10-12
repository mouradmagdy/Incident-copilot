import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function Badge({ className, ...props }: HTMLAttributes<HTMLSpanElement>) {
  return (
    <span
      className={cn('inline-flex w-max items-center gap-[5px] rounded-full text-[9px] font-semibold', className)}
      {...props}
    />
  )
}
