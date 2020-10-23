using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Maxwell.Extensions
{
    public static class TaskExtensions
    {
        public static ConfiguredTaskAwaitable AnyContext(this Task task)
        {
            return (task ?? Task.CompletedTask)
                .ConfigureAwait(false);
        }

        public static ConfiguredTaskAwaitable<T> AnyContext<T>(this Task<T> task)
        {
            return (task ?? Task.FromResult(default(T)))
                .ConfigureAwait(false);
        }
    }
}
