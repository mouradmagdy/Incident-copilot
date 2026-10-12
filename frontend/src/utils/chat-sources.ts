import type { ChatSource } from '../types/api'

export function getUniqueSources(sources: ChatSource[] = []) {
  const seen = new Set<string>()

  return sources.filter((source) => {
    const fileName = source.fileName.trim().toLocaleLowerCase()
    const section = source.section?.trim().toLocaleLowerCase() ?? ''
    const key = `${fileName}::${section}`

    if (seen.has(key)) return false
    seen.add(key)
    return true
  })
}
