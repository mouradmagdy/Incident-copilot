import * as ResizablePrimitive from 'react-resizable-panels'
import type { ComponentProps } from 'react'
import { cn } from '../../utils/classnames'

export function ResizablePanelGroup({
  className,
  ...props
}: ComponentProps<typeof ResizablePrimitive.Group>) {
  return (
    <ResizablePrimitive.Group
      className={cn('h-screen min-w-0', className)}
      {...props}
    />
  )
}
