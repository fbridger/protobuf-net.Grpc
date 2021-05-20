using MegaCorp;
using Microsoft.Extensions.Logging;
using Shared_CS;
using System;
using System.Threading.Tasks;

namespace Server_CS
{
    public class MyCalculator : ICalculator
    {
        ValueTask<MultiplyResult> ICalculator.MultiplyAsync(MultiplyRequest request)
        {
            var result = new MultiplyResult { Result = request.X * request.Y };
            return new ValueTask<MultiplyResult>(result);
        }
        public TimeResult GetTime()
        {
            return new TimeResult { Id = Guid.NewGuid(), Time = DateTime.UtcNow };
        }
    }
}
