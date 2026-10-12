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
        className="group/sidebar grid min-h-screen grid-cols-[184px_minmax(0,1fr)] bg-background transition-[grid-template-columns] duration-300 ease-in-out data-[sidebar-state=collapsed]:grid-cols-[44px_minmax(0,1fr)] max-[720px]:block"
        data-sidebar-state={open ? 'expanded' : 'collapsed'}
        data-assistant-state={assistantOpen ? 'open' : 'closed'}
      >
        {children}
      </div>
    </SidebarContext.Provider>
  )
}
