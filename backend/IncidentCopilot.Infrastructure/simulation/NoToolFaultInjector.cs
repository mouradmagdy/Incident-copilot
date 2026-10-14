using IncidentCopilot.Application.AI.Tools;

namespace IncidentCopilot.Infrastructure.Simulation;

public sealed class NoToolFaultInjector : IToolFaultInjector
{
    public ToolFault CurrentFault => ToolFault.None;
}
