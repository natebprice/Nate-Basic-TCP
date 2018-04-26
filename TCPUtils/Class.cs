using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

namespace TCPUtils
{
    public class TCPUtils
    {
        public long ElapsedSecondsSince(DateTime startTime)
        {
            DateTime now = DateTime.Now;
            long elapsedTicks = now.Ticks - startTime.Ticks;
            long elapsedSec = elapsedTicks / 10000000;
            return (elapsedSec);
        }
    }
}
