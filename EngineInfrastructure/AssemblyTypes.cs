﻿using System;

namespace JustDecompile.EngineInfrastructure
{
    [Flags]
    public enum AssemblyTypes
    {
        Unknown = 0,
        WPF = 1,
        WinForms = 2,
        MVC = 4,
        Windows8Application = 8,
        UniversalWindows = 16
    }
}
