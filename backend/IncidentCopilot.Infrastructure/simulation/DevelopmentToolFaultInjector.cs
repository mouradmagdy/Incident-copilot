using IncidentCopilot.Application.AI.Tools;

namespace IncidentCopilot.Infrastructure.Simulation;

public sealed class DevelopmentToolFaultInjector : IToolFaultState
{
    private int _currentFault;

    public ToolFault CurrentFault =>
        (ToolFault)Volatile.Read(ref _currentFault);

    public void Set(ToolFault fault)
    {
        Volatile.Write(ref _currentFault, (int)fault);
    }
}
