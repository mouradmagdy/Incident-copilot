import { X } from 'lucide-react'
import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { Button } from '../ui/button'

export function DetailPanel({
  title,
  subtitle,
  badge,
  onClose,
  children,
}: {
  title: string
  subtitle?: string
  badge?: ReactNode
  onClose: () => void
  children: ReactNode
}) {
  useEffect(() => {
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }

    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [onClose])

  return createPortal(
    <div
      className="fixed inset-0 z-[100] flex justify-end bg-[#031b35]/45 backdrop-blur-[2px]"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose()
      }}
    >
      <aside className="flex h-full w-[min(430px,100vw)] min-w-0 flex-col overflow-hidden border-l border-border bg-surface shadow-[-20px_0_55px_rgb(3_27_53/24%)] motion-safe:animate-[detail-panel-in_180ms_ease-out]" role="dialog" aria-modal="true" aria-label={`${title} details`}>
        <header className="flex min-h-16 shrink-0 items-start gap-3 border-b border-border px-5 py-4">
          <div className="min-w-0 flex-1">
            <div className="flex min-w-0 items-center gap-2">
              <h2 className="truncate text-sm font-semibold text-text-strong">{title}</h2>
              {badge}
            </div>
            {subtitle && <p className="mt-0.5 truncate text-[10px] text-muted">{subtitle}</p>}
          </div>
          <Button className="-mt-1 -mr-1" variant="ghost" size="icon" type="button" aria-label="Close details" onClick={onClose}>
            <X size={15} />
          </Button>
        </header>
        <div className="min-h-0 flex-1 overflow-y-auto p-5">{children}</div>
      </aside>
    </div>,
    document.body,
  )
}
