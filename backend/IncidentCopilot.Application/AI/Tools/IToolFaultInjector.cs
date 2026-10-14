namespace IncidentCopilot.Application.AI.Tools;

public interface IToolFaultInjector
{
    ToolFault CurrentFault { get; }
}
