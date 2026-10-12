import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import type { ButtonHTMLAttributes } from 'react'
import { cn } from '../../utils/classnames'

const buttonVariants = cva(
  'inline-flex cursor-pointer items-center justify-center gap-1.5 rounded-[7px] font-semibold transition-[background,border-color,color] duration-150 hover:not-disabled:brightness-[.97] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary/55 disabled:cursor-not-allowed disabled:opacity-55',
  {
  variants: {
    variant: {
      default: 'border border-primary bg-primary text-white',
      outline: 'border border-border bg-surface text-text',
      ghost: 'border-0 bg-transparent text-text',
      icon: 'border-0 bg-transparent text-text',
    },
    size: {
      default: 'min-h-[34px] px-3 text-[11px]',
      sm: 'min-h-[27px] px-[9px] text-[9px]',
      icon: 'size-8 p-0',
    },
  },
  defaultVariants: {
    variant: 'default',
    size: 'default',
  },
  },
)

interface ButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

export function Button({
  className,
  variant,
  size,
  asChild = false,
  ...props
}: ButtonProps) {
  const Component = asChild ? Slot : 'button'

  return (
    <Component
      className={cn(buttonVariants({ variant, size }), className)}
      {...props}
    />
  )
}
