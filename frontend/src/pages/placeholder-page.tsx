import { Construction } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { navigationItems } from '../router/navigation'

export function PlaceholderPage() {
  const location = useLocation()
  const page = navigationItems.find((item) => item.path === location.pathname)

  return (
    <div className="placeholder-page">
      <span><Construction size={22} /></span>
      <h1>{page?.label ?? 'Page'}</h1>
      <p>This section is part of the interface and will be connected in a later iteration.</p>
    </div>
  )
}
