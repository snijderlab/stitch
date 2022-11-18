using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace Stitch {
    public class RunTimeException : Exception {
        public new Exception InnerException;
        public InputNameSpace.ErrorMessage ErrorMessage;

        public RunTimeException(InputNameSpace.ErrorMessage message) {
            ErrorMessage = message;
            InnerException = null;
        }

        public RunTimeException(InputNameSpace.ErrorMessage message, Exception exception) {
            ErrorMessage = message;
            InnerException = exception;
        }
    }
}