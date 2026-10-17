import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

export function MarkdownMessage({ content }: { content: string }) {
  return (
    <div className="rounded-lg bg-surface px-3 py-2.5 text-xs leading-relaxed text-text-strong [overflow-wrap:anywhere] [&>:first-child]:mt-0 [&>:last-child]:mb-0 [&_a]:text-primary-text [&_blockquote]:mb-2 [&_blockquote]:border-l-2 [&_blockquote]:border-border [&_blockquote]:pl-2 [&_blockquote]:text-muted [&_code]:rounded-sm [&_code]:bg-surface-muted [&_code]:px-[3px] [&_code]:py-px [&_code]:font-mono [&_code]:text-[.92em] [&_h1]:mt-[11px] [&_h1]:mb-[5px] [&_h1]:text-[1.12em] [&_h1]:leading-[1.3] [&_h2]:mt-[11px] [&_h2]:mb-[5px] [&_h2]:text-[1.12em] [&_h2]:leading-[1.3] [&_h3]:mt-[11px] [&_h3]:mb-[5px] [&_h3]:text-[1.12em] [&_h3]:leading-[1.3] [&_h4]:mt-[11px] [&_h4]:mb-[5px] [&_h4]:text-[1.12em] [&_h4]:leading-[1.3] [&_li+li]:mt-[3px] [&_ol]:mb-2 [&_ol]:list-decimal [&_ol]:pl-[17px] [&_p]:mb-2 [&_pre]:mb-2 [&_pre]:overflow-x-auto [&_pre]:rounded-md [&_pre]:bg-surface-muted [&_pre]:p-2 [&_pre_code]:bg-transparent [&_pre_code]:p-0 [&_table]:mb-2 [&_table]:block [&_table]:max-w-full [&_table]:border-collapse [&_table]:overflow-x-auto [&_td]:border [&_td]:border-border [&_td]:px-1.5 [&_td]:py-1 [&_th]:border [&_th]:border-border [&_th]:px-1.5 [&_th]:py-1 [&_th]:text-left [&_ul]:mb-2 [&_ul]:list-disc [&_ul]:pl-[17px]">
      <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
    </div>
  )
}
