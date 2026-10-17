import { useMemo, useState, type ReactNode } from 'react'
import { SidebarContext } from '../../hooks/ui/use-sidebar'

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
        className="app-shell group/sidebar grid h-screen w-full min-w-0 grid-cols-[184px_minmax(0,1fr)] overflow-hidden bg-background transition-[grid-template-columns] duration-200 ease-out will-change-[grid-template-columns] motion-reduce:transition-none data-[sidebar-state=collapsed]:grid-cols-[44px_minmax(0,1fr)] max-[720px]:block"
        data-sidebar-state={open ? 'expanded' : 'collapsed'}
        data-assistant-state={assistantOpen ? 'open' : 'closed'}
      >
        {children}
      </div>
    </SidebarContext.Provider>
  )
}
