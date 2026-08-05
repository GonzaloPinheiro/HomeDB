using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeDB.Domain.Exceptions
{
    public class AssembledFileSizeMismatchException : Exception
    {
        public AssembledFileSizeMismatchException(Guid sessionId, long expectedSize, long actualSize)
            : base($"Assembled file size mismatch for session {sessionId}. Expected: {expectedSize} bytes, Actual: {actualSize} bytes.") { }
    }
}
