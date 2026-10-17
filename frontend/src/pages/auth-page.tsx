import { ArrowRight, LockKeyhole, Mail, UserRound } from 'lucide-react'
import { useState, type FormEvent, type ReactNode } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Button } from '../components/ui/button'
import { Input } from '../components/ui/input'
import { useAuth } from '../hooks/auth/use-auth'

export function AuthPage({ mode }: { mode: 'login' | 'signup' }) {
  const { user, login, register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const isSignup = mode === 'signup'

  if (user) return <Navigate to="/" replace />

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      if (isSignup) {
        await register({ displayName, email, password })
      } else {
        await login({ email, password })
      }

      const destination = (location.state as { from?: string } | null)?.from
      navigate(destination && destination !== '/login' ? destination : '/', {
        replace: true,
      })
    } catch (submissionError) {
      setError(submissionError instanceof Error
        ? submissionError.message
        : 'The request could not be completed.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-background px-5 py-10">
      <section className="w-full max-w-[400px] rounded-xl border border-border bg-surface p-7 shadow-[0_8px_30px_rgb(15_23_42/6%)] sm:p-8">
        <header className="mb-7 text-center">
          <h1 className="text-2xl font-semibold tracking-[-.03em] text-text-strong">
            {isSignup ? 'Create account' : 'Sign in'}
          </h1>
          <p className="mt-1.5 text-xs text-muted">Incident Copilot</p>
        </header>

        <form className="space-y-4" onSubmit={submit}>
          {isSignup && (
            <AuthField label="Display name" icon={<UserRound size={16} />}>
              <Input
                autoComplete="name"
                className="h-11 pl-10 text-sm"
                maxLength={80}
                required
                value={displayName}
                onChange={(event) => setDisplayName(event.target.value)}
              />
            </AuthField>
          )}

          <AuthField label="Email" icon={<Mail size={16} />}>
            <Input
              type="email"
              autoComplete="email"
              className="h-11 pl-10 text-sm"
              maxLength={254}
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </AuthField>

          <AuthField label="Password" icon={<LockKeyhole size={16} />}>
            <Input
              type="password"
              autoComplete={isSignup ? 'new-password' : 'current-password'}
              className="h-11 pl-10 text-sm"
              minLength={isSignup ? 10 : undefined}
              required
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </AuthField>

          {isSignup && (
            <p className="text-[11px] leading-relaxed text-muted">
              Use at least 10 characters with uppercase, lowercase, and a number.
            </p>
          )}

          {error && (
            <p className="rounded-lg border border-danger/30 bg-danger-soft px-3 py-2.5 text-xs text-danger" role="alert">
              {error}
            </p>
          )}

          <Button className="h-11 w-full text-sm" disabled={isSubmitting} type="submit">
            {isSubmitting
              ? isSignup ? 'Creating account…' : 'Signing in…'
              : isSignup ? 'Create account' : 'Sign in'}
            {!isSubmitting && <ArrowRight size={16} />}
          </Button>
        </form>

        <p className="mt-6 text-center text-xs text-muted">
          {isSignup ? 'Already have an account?' : 'Need an account?'}{' '}
          <Link className="font-semibold text-primary-text hover:underline" to={isSignup ? '/login' : '/signup'}>
            {isSignup ? 'Sign in' : 'Sign up'}
          </Link>
        </p>
      </section>
    </main>
  )
}

function AuthField({
  label,
  icon,
  children,
}: {
  label: string
  icon: ReactNode
  children: ReactNode
}) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-xs font-semibold text-text">{label}</span>
      <span className="relative block">
        <span className="pointer-events-none absolute top-1/2 left-3 z-10 -translate-y-1/2 text-muted">{icon}</span>
        {children}
      </span>
    </label>
  )
}
