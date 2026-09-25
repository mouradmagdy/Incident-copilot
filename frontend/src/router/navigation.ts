import {
  AlertTriangle,
  Bell,
  Box,
  FileText,
  LayoutDashboard,
  Network,
  Settings,
  type LucideIcon,
} from 'lucide-react'

export interface NavigationItem {
  label: string
  path: string
  icon: LucideIcon
}

export const navigationItems: NavigationItem[] = [
  { label: 'Overview', path: '/', icon: LayoutDashboard },
  { label: 'Services', path: '/services', icon: Box },
  { label: 'Logs', path: '/logs', icon: FileText },
  { label: 'Incidents', path: '/incidents', icon: AlertTriangle },
  { label: 'Alerts', path: '/alerts', icon: Bell },
  { label: 'Infrastructure', path: '/infrastructure', icon: Network },
  { label: 'Settings', path: '/settings', icon: Settings },
]
