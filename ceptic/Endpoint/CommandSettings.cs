using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Endpoint
{
    public class CommandSettings
    {
        public readonly int bodyMax;
        public readonly long timeMax;

        public CommandSettings(int bodyMax, long timeMax)
        {
            this.bodyMax = bodyMax;
            this.timeMax = timeMax;
        }

        public CommandSettings Copy()
        {
            return new CommandSettings(bodyMax, timeMax);
        }

        public static CommandSettings Combine(CommandSettings initial, CommandSettings updates)
        {
            int bodyMax = updates.bodyMax >= 0 ? updates.bodyMax : initial.bodyMax;
            long timeMax = updates.timeMax >= 0 ? updates.timeMax : initial.timeMax;
            return new CommandSettings(bodyMax, timeMax);
        }

        public static CommandSettings CreateWithBodyMax(int bodyMax)
        {
            return new CommandSettings(bodyMax, -1);
        }
    }
}
