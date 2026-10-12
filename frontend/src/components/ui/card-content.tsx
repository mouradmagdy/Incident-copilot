import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function CardContent({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('px-3 pb-3', className)} {...props} />
}
