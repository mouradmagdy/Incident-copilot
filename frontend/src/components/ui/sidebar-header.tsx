import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function SidebarHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('flex items-center justify-between gap-1 group-data-[sidebar-state=collapsed]/sidebar:justify-center max-[720px]:flex-row', className)}
      {...props}
    />
  )
}
