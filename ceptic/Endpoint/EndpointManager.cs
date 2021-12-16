using Ceptic.Endpoint.Exceptions;
using Ceptic.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Endpoint
{
    public class EndpointManager
    {
        protected readonly ConcurrentDictionary<string, CommandEntry> commandMap = new ConcurrentDictionary<string, CommandEntry>();
        protected readonly ServerSettings serverSettings;

        public EndpointManager(ServerSettings settings)
        {
            serverSettings = settings;
        }

        public void AddCommand(string command, CommandSettings settings=null)
        {
            if (settings != null)
                commandMap.TryAdd(command, new CommandEntry(command, settings));
            else
                commandMap.TryAdd(command, new CommandEntry(command, serverSettings));
        }

        public CommandEntry GetCommand(string command)
        {
            commandMap.TryGetValue(command, out var commandEntry);
            return commandEntry;
        }

        public CommandEntry RemoveCommand(string command)
        {
            commandMap.TryRemove(command, out var commandEntry);
            return commandEntry;
        }

        public EndpointValue GetEndpoint(string command, string endpoint)
        {
            var commandEntry = GetCommand(command);
            if (commandEntry != null)
                return commandEntry.GetEndpoint(endpoint);
            else
                throw new EndpointManagerException($"command '{command}' not found");
        }

        public void AddEndpoint(string command, string endpoint, EndpointEntry entry, CommandSettings settings = null)
        {
            var commandEntry = GetCommand(command);
            if (commandEntry != null)
                commandEntry.AddEndpoint(endpoint, entry, settings);
            else
                throw new EndpointManagerException($"command '{command}' not found");
        }

        public EndpointSaved RemoveEndpoint(string command, string endpoint)
        {
            return GetCommand(command)?.RemoveEndpoint(endpoint);
        }

    }
}
