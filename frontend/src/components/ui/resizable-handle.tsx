import { GripVertical } from 'lucide-react'
import * as ResizablePrimitive from 'react-resizable-panels'
import type { ComponentProps } from 'react'
import { cn } from '../../utils/classnames'

export function ResizableHandle({
  className,
  withHandle = false,
  ...props
}: ComponentProps<typeof ResizablePrimitive.Separator> & { withHandle?: boolean }) {
  return (
    <ResizablePrimitive.Separator
      className={cn('group/handle relative z-[2] flex w-px items-center justify-center bg-border outline-none transition-colors hover:bg-primary focus-visible:bg-primary data-[separator=active]:bg-primary', className)}
      {...props}
    >
      {withHandle && (
        <span className="absolute grid h-[30px] w-[15px] place-items-center rounded-[5px] border border-border bg-surface-elevated text-muted shadow-[0_1px_3px_rgb(15_23_42/8%)] group-hover/handle:border-primary/55 group-hover/handle:text-primary group-focus-visible/handle:border-primary/55 group-focus-visible/handle:text-primary" aria-hidden="true">
          <GripVertical size={12} />
        </span>
      )}
    </ResizablePrimitive.Separator>
  )
}
