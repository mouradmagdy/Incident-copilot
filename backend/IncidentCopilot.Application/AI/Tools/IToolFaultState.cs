namespace IncidentCopilot.Application.AI.Tools;

public interface IToolFaultState : IToolFaultInjector
{
    void Set(ToolFault fault);
}
