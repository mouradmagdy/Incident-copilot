import { PanelLeftClose } from 'lucide-react'
import { useSidebar } from '../../hooks/ui/use-sidebar'
import { Button } from './button'

export function SidebarTrigger() {
  const { open, toggle } = useSidebar()

  if (!open) return null

  return (
    <Button
      variant="ghost"
      size="icon"
      type="button"
      className="size-7 shrink-0"
      aria-label="Collapse sidebar"
      onClick={toggle}
    >
      <PanelLeftClose size={15} />
    </Button>
  )
}
