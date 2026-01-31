using AndrewM5.DevKit.Threading.Abstractions;
using AndrewM5.DevKit.Threading.Scheduling.Abstractions;
using AndrewM5.DevKit.Threading.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.Threading.Scheduling;

public static class ThreadingSchedulerHost
{
    private const string NotInitializedMsg = "ThreadingSchedulerHost has not been initialized.";

    private static IServiceProvider? _serviceProvider;
    private static ITaskSchedulerService? _schedulerService;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        _serviceProvider = serviceProvider;
        _schedulerService = _serviceProvider.GetService<ITaskSchedulerService>();
        if (_schedulerService == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskSchedulerService)} is not registered. Make sure you call ... when configuring services before initializing {nameof(ThreadingSchedulerHost)}.");
        }
    }

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _serviceProvider;
        }
    }

    public static ITaskSchedulerService SchedulerService
    {
        get
        {
            if (_schedulerService == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _schedulerService;
        }
    }
}
