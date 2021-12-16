using Ceptic.Stream;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common
{
    public interface IRemovableManagers
    {
        IStreamManager RemoveManager(Guid managerId);
        void HandleNewConnection(StreamHandler stream);
    }
}
