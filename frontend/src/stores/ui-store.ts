import { create } from 'zustand'

export type Theme = 'light' | 'dark'
export type TimeRange = '1h' | '6h' | '24h' | '7d'

interface UiState {
  theme: Theme
  timeRange: TimeRange
  assistantOpen: boolean
  toggleTheme: () => void
  setTimeRange: (timeRange: TimeRange) => void
  setAssistantOpen: (open: boolean) => void
}

export const useUiStore = create<UiState>((set) => ({
  theme: window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light',
  timeRange: '24h',
  assistantOpen: false,
  toggleTheme: () => set((state) => ({
    theme: state.theme === 'light' ? 'dark' : 'light',
  })),
  setTimeRange: (timeRange) => set({ timeRange }),
  setAssistantOpen: (assistantOpen) => set({ assistantOpen }),
}))
