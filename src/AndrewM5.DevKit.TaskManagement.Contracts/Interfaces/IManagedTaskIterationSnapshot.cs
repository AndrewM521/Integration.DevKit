using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

public interface IManagedTaskIterationSnapshot
{
    public int IterationNumber { get; }

    public ManagedTaskState State { get; }

    public DateTime StartDTM { get; }

    public DateTime EndDTM { get; }

    public TimeSpan Runtime { get; }

    public Exception? Exception { get; }

    public string GetIterationInfo(bool includeIndent = true);
}
