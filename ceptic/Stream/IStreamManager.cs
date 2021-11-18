using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream
{
    interface IStreamManager : IDisposable
    {
        Guid GetManagerId();
        string GetDestination();
        bool IsHandlerLimitReached();
        StreamHandler CreateHandler();
        StreamHandler CreateHandler(Guid streamId);
        void Stop();
        void Stop(string reason);
        bool IsFullyStopped();
        string GetStopReason();
        bool IsTimedOut();
    }
}
