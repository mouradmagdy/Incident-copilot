import {
  AlertTriangle,
  Box,
  FileText,
  LayoutDashboard,
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
]
