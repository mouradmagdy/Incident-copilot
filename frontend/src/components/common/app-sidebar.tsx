import { Bot, LogOut, Moon, PanelLeftOpen, Sun } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import copilotLogo from '../../assets/incident-copilot-logo.png'
import { useAuth } from '../../hooks/auth/use-auth'
import { useSidebar } from '../../hooks/ui/use-sidebar'
import { navigationItems } from '../../router/navigation'
import { useUiStore } from '../../stores/ui-store'
import { Sidebar } from '../ui/sidebar'
import { SidebarContent } from '../ui/sidebar-content'
import { SidebarFooter } from '../ui/sidebar-footer'
import { SidebarHeader } from '../ui/sidebar-header'
import { SidebarMenuButton } from '../ui/sidebar-menu-button'
import { SidebarTrigger } from '../ui/sidebar-trigger'

export function AppSidebar() {
  const { open, toggle } = useSidebar()
  const { user, logout } = useAuth()
  const { theme, assistantOpen, toggleTheme, setAssistantOpen } = useUiStore()
  const initials = getInitials(user?.displayName ?? 'User')

  return (
    <Sidebar>
      <SidebarHeader>
        {open ? (
          <div className="flex h-[34px] min-w-0 flex-1 items-center gap-2 overflow-hidden px-[5px] text-sm whitespace-nowrap text-sidebar-foreground">
            <span className="grid size-7 shrink-0 place-items-center overflow-hidden rounded-[7px] bg-white shadow-sm">
              <img className="size-full scale-[1.72] object-cover" src={copilotLogo} alt="" draggable={false} />
            </span>
            <strong className="min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap opacity-100 transition-[max-width,opacity] duration-200">Copilot</strong>
          </div>
        ) : (
          <button
            type="button"
            className="group/logo flex h-[34px] w-full cursor-pointer items-center justify-center overflow-hidden border-0 bg-transparent px-[5px] text-sm whitespace-nowrap text-sidebar-foreground"
            aria-label="Expand sidebar"
            title="Expand sidebar"
            onClick={toggle}
          >
            <span className="relative grid size-7 shrink-0 place-items-center overflow-hidden rounded-[7px] bg-white text-slate-700 shadow-sm">
              <img className="col-start-1 row-start-1 size-full scale-[1.72] object-cover transition duration-150 group-hover/logo:scale-[1.4] group-hover/logo:opacity-0 group-focus-visible/logo:scale-[1.4] group-focus-visible/logo:opacity-0" src={copilotLogo} alt="" draggable={false} />
              <PanelLeftOpen className="col-start-1 row-start-1 scale-80 opacity-0 transition duration-150 group-hover/logo:scale-100 group-hover/logo:opacity-100 group-focus-visible/logo:scale-100 group-focus-visible/logo:opacity-100" size={16} />
            </span>
          </button>
        )}
        <SidebarTrigger />
      </SidebarHeader>

      <SidebarContent>
        <nav className="mt-[17px] flex flex-col gap-[3px] max-[720px]:mt-[7px] max-[720px]:flex-row max-[720px]:overflow-x-auto" aria-label="Primary navigation">
          {navigationItems.map(({ label, path, icon: Icon }) => (
            <SidebarMenuButton key={path} asChild>
              <NavLink
                to={path}
                end={path === '/'}
                title={label}
              >
                <Icon className="shrink-0" size={15} />
                <span className="max-w-[150px] overflow-hidden whitespace-nowrap opacity-100 transition-[max-width,opacity] duration-200 group-data-[sidebar-state=collapsed]/sidebar:max-w-0 group-data-[sidebar-state=collapsed]/sidebar:opacity-0 group-data-[sidebar-state=collapsed]/sidebar:pointer-events-none">{label}</span>
              </NavLink>
            </SidebarMenuButton>
          ))}
        </nav>
      </SidebarContent>

      <SidebarFooter>
        <div className="grid gap-1 border-b border-sidebar-border pb-2">
          <SidebarMenuButton type="button" title={theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode'} onClick={toggleTheme}>
            {theme === 'light' ? <Moon className="shrink-0" size={15} /> : <Sun className="shrink-0" size={15} />}
            <span className="max-w-[150px] overflow-hidden whitespace-nowrap opacity-100 transition-[max-width,opacity] duration-200 group-data-[sidebar-state=collapsed]/sidebar:max-w-0 group-data-[sidebar-state=collapsed]/sidebar:opacity-0 group-data-[sidebar-state=collapsed]/sidebar:pointer-events-none">{theme === 'light' ? 'Dark mode' : 'Light mode'}</span>
          </SidebarMenuButton>
          <SidebarMenuButton
            className={assistantOpen ? 'bg-sidebar-primary text-white hover:bg-sidebar-primary' : undefined}
            type="button"
            title={assistantOpen ? 'Close AI assistant' : 'Open AI assistant'}
            aria-pressed={assistantOpen}
            onClick={() => setAssistantOpen(!assistantOpen)}
          >
            <Bot className="shrink-0" size={15} />
            <span className="max-w-[150px] overflow-hidden whitespace-nowrap opacity-100 transition-[max-width,opacity] duration-200 group-data-[sidebar-state=collapsed]/sidebar:max-w-0 group-data-[sidebar-state=collapsed]/sidebar:opacity-0 group-data-[sidebar-state=collapsed]/sidebar:pointer-events-none">AI assistant</span>
          </SidebarMenuButton>
        </div>
        <div className="flex min-w-0 items-center gap-2 p-[5px] group-data-[sidebar-state=collapsed]/sidebar:justify-center group-data-[sidebar-state=collapsed]/sidebar:gap-0 group-data-[sidebar-state=collapsed]/sidebar:px-0">
          <span className="grid size-7 shrink-0 place-items-center rounded-full bg-sidebar-primary text-[9px] font-bold text-white">{initials}</span>
          <div className="flex min-h-7 max-w-[150px] flex-col justify-center overflow-hidden whitespace-nowrap opacity-100 transition-[max-width,opacity] duration-200 group-data-[sidebar-state=collapsed]/sidebar:max-w-0 group-data-[sidebar-state=collapsed]/sidebar:opacity-0 group-data-[sidebar-state=collapsed]/sidebar:pointer-events-none">
            <strong className="block overflow-hidden text-[9px] text-ellipsis whitespace-nowrap text-sidebar-foreground">{user?.displayName}</strong>
            <span className="mt-0.5 block overflow-hidden text-[8px] text-ellipsis text-sidebar-foreground/65">{user?.email}</span>
          </div>
        </div>
        <button className="flex w-full cursor-pointer items-center gap-2 rounded-md border-0 bg-transparent p-[7px] text-[9px] text-sidebar-foreground/75 transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground group-data-[sidebar-state=collapsed]/sidebar:justify-center group-data-[sidebar-state=collapsed]/sidebar:gap-0 group-data-[sidebar-state=collapsed]/sidebar:px-0" type="button" title="Log out" onClick={() => void logout()}>
          <LogOut className="shrink-0" size={15} />
          <span className="max-w-[150px] overflow-hidden whitespace-nowrap opacity-100 transition-[max-width,opacity] duration-200 group-data-[sidebar-state=collapsed]/sidebar:max-w-0 group-data-[sidebar-state=collapsed]/sidebar:opacity-0 group-data-[sidebar-state=collapsed]/sidebar:pointer-events-none">Log out</span>
        </button>
      </SidebarFooter>
    </Sidebar>
  )
}

function getInitials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('') || 'U'
}
