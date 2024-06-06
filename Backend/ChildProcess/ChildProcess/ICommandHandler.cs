using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChildProcess
{
    internal interface ICommandHandler
    {
        void HandleCommand(Communication stream);

    }
}
