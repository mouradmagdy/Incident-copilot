import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function SidebarContent({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('min-h-0 overflow-x-hidden overflow-y-auto max-[720px]:group-data-[sidebar-state=collapsed]/sidebar:hidden', className)}
      {...props}
    />
  )
}
