import { useEffect } from 'react'
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

  useEffect(() => {
    document.documentElement.dataset.theme = theme
  }, [theme])

  return (
    <SidebarProvider defaultOpen={false} assistantOpen={assistantOpen}>
      <AppSidebar />
      <ResizablePanelGroup className="h-screen min-w-0" orientation="horizontal">
        <ResizablePanel id="main-content" minSize="45%">
          <main className="h-screen min-w-0 overflow-y-auto"><Outlet /></main>
        </ResizablePanel>
        {assistantOpen && (
          <>
            <ResizableHandle id="assistant-resize-handle" withHandle />
            <ResizablePanel
              id="assistant-panel"
              defaultSize="32%"
              minSize="28%"
              maxSize="55%"
            >
              <AssistantPanel />
            </ResizablePanel>
          </>
        )}
      </ResizablePanelGroup>
    </SidebarProvider>
  )
}
