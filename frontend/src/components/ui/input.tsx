import type { InputHTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn('w-full rounded-[7px] border border-border bg-surface text-text-strong outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary/55', className)}
      {...props}
    />
  )
}
