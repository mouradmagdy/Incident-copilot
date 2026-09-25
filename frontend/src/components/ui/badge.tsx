import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function Badge({ className, ...props }: HTMLAttributes<HTMLSpanElement>) {
  return <span className={cn('ui-badge', className)} {...props} />
}
