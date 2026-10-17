import { useEffect, useRef } from 'react'
import { usePanelRef } from 'react-resizable-panels'
import { Outlet } from 'react-router-dom'
import { AppSidebar } from '../components/common/app-sidebar'
import { AssistantPanel } from '../components/common/assistant-panel'
import { ResizableHandle } from '../components/ui/resizable-handle'
import { ResizablePanel } from '../components/ui/resizable-panel'
import { ResizablePanelGroup } from '../components/ui/resizable-panel-group'
import { SidebarProvider } from '../components/ui/sidebar-provider'
import { useUiStore } from '../stores/ui-store'

export function AppLayout() {
  const theme = useUiStore((state) => state.theme)
  const assistantOpen = useUiStore((state) => state.assistantOpen)
  const setAssistantOpen = useUiStore((state) => state.setAssistantOpen)
  const assistantPanelRef = usePanelRef()
  const assistantSizeRef = useRef(32)

  useEffect(() => {
    document.documentElement.dataset.theme = theme
  }, [theme])

  useEffect(() => {
    let stateCheckFrame: number | undefined
    const animationFrame = requestAnimationFrame(() => {
      if (assistantOpen) {
        assistantPanelRef.current?.resize(`${assistantSizeRef.current}%`)
        stateCheckFrame = requestAnimationFrame(() => {
          if (assistantPanelRef.current?.isCollapsed()) {
            setAssistantOpen(false)
          }
        })
      } else {
        assistantPanelRef.current?.collapse()
      }
    })

    return () => {
      cancelAnimationFrame(animationFrame)
      if (stateCheckFrame !== undefined) cancelAnimationFrame(stateCheckFrame)
    }
  }, [assistantOpen, assistantPanelRef, setAssistantOpen])

  return (
    <SidebarProvider defaultOpen={false} assistantOpen={assistantOpen}>
      <AppSidebar />
      <ResizablePanelGroup className="smooth-panel-group h-screen min-w-0" orientation="horizontal">
        <ResizablePanel id="main-content" minSize="45%">
          <main className="h-screen min-w-0 overflow-x-hidden overflow-y-auto"><Outlet /></main>
        </ResizablePanel>
        <ResizableHandle
          id="assistant-resize-handle"
          withHandle={assistantOpen}
          disabled={!assistantOpen}
          className={assistantOpen ? 'opacity-100' : 'w-0 opacity-0 pointer-events-none'}
        />
        <ResizablePanel
          id="assistant-panel"
          panelRef={assistantPanelRef}
          defaultSize="0%"
          minSize="28%"
          maxSize="55%"
          collapsedSize="0%"
          collapsible
          style={{ overflow: 'hidden' }}
          className={assistantOpen
            ? 'opacity-100 transition-opacity duration-200 ease-out'
            : 'opacity-0 pointer-events-none transition-opacity duration-150 ease-in'}
          aria-hidden={!assistantOpen}
          onResize={({ asPercentage }) => {
            if (asPercentage < 1) {
              if (assistantOpen) setAssistantOpen(false)
              return
            }

            if (asPercentage >= 28) {
              assistantSizeRef.current = asPercentage
            }
          }}
        >
          <div className="h-full min-w-[280px]">
            <AssistantPanel />
          </div>
        </ResizablePanel>
      </ResizablePanelGroup>
    </SidebarProvider>
  )
}
