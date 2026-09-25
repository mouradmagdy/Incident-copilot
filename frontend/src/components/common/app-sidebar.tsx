import { LogOut } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { navigationItems } from '../../router/navigation'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenuButton,
  SidebarTrigger,
} from '../ui/sidebar'

export function AppSidebar() {
  return (
    <Sidebar>
      <SidebarHeader>
        <div className="brand-lockup">
          <span className="brand-logo">IC</span>
          <strong className="sidebar-label">Incident Copilot</strong>
        </div>
        <SidebarTrigger />
      </SidebarHeader>

      <SidebarContent>
        <nav className="sidebar-nav" aria-label="Primary navigation">
          {navigationItems.map(({ label, path, icon: Icon }) => (
            <SidebarMenuButton key={path} asChild>
              <NavLink
                to={path}
                end={path === '/'}
                title={label}
              >
                <Icon size={15} />
                <span className="sidebar-label">{label}</span>
              </NavLink>
            </SidebarMenuButton>
          ))}
        </nav>
      </SidebarContent>

      <SidebarFooter>
        <div className="user-summary">
          <span className="user-avatar">JD</span>
          <div className="sidebar-label">
            <strong>Jordan Davis</strong>
            <span>Engineer</span>
          </div>
        </div>
        <button className="logout-button" type="button" title="Log out (not connected yet)">
          <LogOut size={15} />
          <span className="sidebar-label">Log out</span>
        </button>
      </SidebarFooter>
    </Sidebar>
  )
}
