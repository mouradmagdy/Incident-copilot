import type { HTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function Sidebar({ className, ...props }: HTMLAttributes<HTMLElement>) {
  return (
    <aside
      className={cn('sticky top-0 grid h-screen min-w-0 grid-rows-[auto_minmax(0,1fr)_auto] overflow-hidden border-r border-sidebar-border bg-sidebar px-2 py-3 transition-[padding] duration-200 ease-out contain-[layout_paint] motion-reduce:transition-none group-data-[sidebar-state=collapsed]/sidebar:px-[5px] max-[720px]:static max-[720px]:h-auto max-[720px]:w-full max-[720px]:border-r-0 max-[720px]:border-b max-[720px]:px-3 max-[720px]:py-[9px]', className)}
      {...props}
    />
  )
}
