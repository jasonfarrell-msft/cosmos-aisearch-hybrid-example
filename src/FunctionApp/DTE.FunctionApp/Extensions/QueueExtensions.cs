using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTE.FunctionApp.Extensions
{
    public static class QueueExtensions
    {
        public static T? DequeueOrDefault<T>(this Queue<T> queue) where T : class
        {
            if (queue.Count == 0)
            {
                return null;
            }
            return queue.Dequeue();
        }
    }
}
