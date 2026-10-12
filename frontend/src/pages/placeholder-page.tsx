import { Construction } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { navigationItems } from '../router/navigation'

export function PlaceholderPage() {
  const location = useLocation()
  const page = navigationItems.find((item) => item.path === location.pathname)

  return (
    <div className="grid min-h-screen place-content-center p-8 text-center text-muted">
      <span className="mx-auto mb-3 grid size-[46px] place-items-center rounded-xl bg-primary-soft text-primary"><Construction size={22} /></span>
      <h1 className="text-[22px] text-text-strong">{page?.label ?? 'Page'}</h1>
      <p className="mt-[7px] max-w-[420px] text-[11px]">This section is part of the interface and will be connected in a later iteration.</p>
    </div>
  )
}
