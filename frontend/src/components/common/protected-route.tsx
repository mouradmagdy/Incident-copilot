import { Navigate, Outlet, useLocation } from 'react-router-dom'
import copilotLogo from '../../assets/incident-copilot-logo.png'
import { useAuth } from '../../hooks/auth/use-auth'

export function ProtectedRoute() {
  const { user, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="grid h-screen place-items-center bg-background">
        <div className="grid place-items-center gap-3 text-sm text-muted">
          <span className="grid size-12 place-items-center overflow-hidden rounded-xl bg-white shadow-sm">
            <img className="size-full scale-[1.72] object-cover" src={copilotLogo} alt="" />
          </span>
          Loading Incident Copilot…
        </div>
      </div>
    )
  }

  return user
    ? <Outlet />
    : <Navigate to="/login" replace state={{ from: location.pathname }} />
}
