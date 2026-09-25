import { Slot } from '@radix-ui/react-slot'
import { useMemo, useState, type HTMLAttributes, type ReactNode } from 'react'
import { PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { SidebarContext, useSidebar } from '../../hooks/ui/use-sidebar'
import { cn } from '../../utils/classnames'
import { Button } from './button'

export function SidebarProvider({
  children,
  defaultOpen = true,
  assistantOpen = false,
}: {
  children: ReactNode
  defaultOpen?: boolean
  assistantOpen?: boolean
}) {
  const [open, setOpen] = useState(defaultOpen)
  const value = useMemo(() => ({ open, toggle: () => setOpen((current) => !current) }), [open])

  return (
    <SidebarContext.Provider value={value}>
      <div
        className="sidebar-provider"
        data-sidebar-state={open ? 'expanded' : 'collapsed'}
        data-assistant-state={assistantOpen ? 'open' : 'closed'}
      >
        {children}
      </div>
    </SidebarContext.Provider>
  )
}

export function Sidebar({ className, ...props }: HTMLAttributes<HTMLElement>) {
  return <aside className={cn('sidebar', className)} {...props} />
}

export function SidebarHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('sidebar-header', className)} {...props} />
}

export function SidebarContent({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('sidebar-content', className)} {...props} />
}

export function SidebarFooter({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('sidebar-footer', className)} {...props} />
}

export function SidebarMenuButton({
  asChild = false,
  className,
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { asChild?: boolean }) {
  const Component = asChild ? Slot : 'button'
  return <Component className={cn('nav-item', className)} {...props} />
}

export function SidebarTrigger() {
  const { open, toggle } = useSidebar()

  return (
    <Button
      variant="ghost"
      size="icon"
      type="button"
      className="sidebar-trigger"
      aria-label={open ? 'Collapse sidebar' : 'Expand sidebar'}
      onClick={toggle}
    >
      {open ? <PanelLeftClose size={15} /> : <PanelLeftOpen size={15} />}
    </Button>
  )
}
