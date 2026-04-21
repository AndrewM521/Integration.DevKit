using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

public class TimeStrategySettings
{
    public bool SkipFirstIterationWait { get; set; } = true;

    // Skips missed iteration executions but gets the target dtm to or passed the current dtm
    public bool FastForwardToPresent { get; set; } = true;

    /// <summary>
    /// Gets the specific date the strategy should begin from. If null, defaults to the current date.
    /// </summary>
    public DateOnly? CustomStartDate { get; set; }

    /// <summary>
    /// Gets the specific time of day the strategy should begin from. If null, defaults to the current time.
    /// </summary>
    public TimeSpan? CustomStartTime { get; set; }
}
