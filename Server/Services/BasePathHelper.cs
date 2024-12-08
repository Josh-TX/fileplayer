using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class BasePathHelper
{
#if DEBUG
    public static string BasePath = Directory.GetCurrentDirectory();
#else
    public static string BasePath = "/";
#endif
}
