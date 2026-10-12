import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function SidebarFooter({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('grid gap-[7px] border-t border-border pt-3 max-[720px]:group-data-[sidebar-state=collapsed]/sidebar:hidden', className)}
      {...props}
    />
  )
}
