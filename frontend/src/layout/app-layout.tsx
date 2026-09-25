import { useEffect } from 'react'
import { Outlet } from 'react-router-dom'
import { AppSidebar } from '../components/common/app-sidebar'
import { AssistantPanel } from '../components/common/assistant-panel'
import { SidebarProvider } from '../components/ui/sidebar'
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
      <main className="content-shell"><Outlet /></main>
      <AssistantPanel open={assistantOpen} />
    </SidebarProvider>
  )
}
