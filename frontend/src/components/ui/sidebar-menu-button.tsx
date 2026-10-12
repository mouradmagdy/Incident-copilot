import { Slot } from '@radix-ui/react-slot'
import type { ButtonHTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function SidebarMenuButton({
  asChild = false,
  className,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { asChild?: boolean }) {
  const Component = asChild ? Slot : 'button'

  return (
    <Component
      className={cn('flex w-full max-w-full cursor-pointer items-center gap-2.5 overflow-hidden rounded-[7px] border-0 bg-transparent px-2.5 py-[9px] text-left text-[11px] text-text no-underline transition-colors hover:bg-surface-muted hover:text-text-strong aria-[current=page]:bg-primary-soft aria-[current=page]:font-semibold aria-[current=page]:text-primary-text group-data-[sidebar-state=collapsed]/sidebar:justify-center group-data-[sidebar-state=collapsed]/sidebar:gap-0 group-data-[sidebar-state=collapsed]/sidebar:px-0 max-[720px]:w-auto max-[720px]:shrink-0', className)}
      {...props}
    />
  )
}
