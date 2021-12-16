using Ceptic.Endpoint.Exceptions;
using Ceptic.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Ceptic.Endpoint
{
    public class CommandEntry
    {
        private readonly string command;
        protected readonly ConcurrentDictionary<EndpointPattern, EndpointSaved> endpointMap = new ConcurrentDictionary<EndpointPattern, EndpointSaved>();
        protected readonly CommandSettings settings;

        #region Regex
        private static readonly Regex allowedRegex = new Regex("^[!-\\[\\]-~]+$", RegexOptions.Compiled); // ! to [ and ] to ~ ascii characters
        private static readonly Regex startSlashRegex = new Regex("^/{2,}", RegexOptions.Compiled); // 2 or more slashes at start
        private static readonly Regex endSlashRegex = new Regex("/+$", RegexOptions.Compiled); // slashes at the end
        private static readonly Regex middleSlashRegex = new Regex("/{2,}", RegexOptions.Compiled); // 2 or more slashes next to each other

        // alphanumerical and -.<>_/
        private static readonly Regex allowedRegexConvert = new Regex("^[a-zA-Z0-9\\-.<>_/]+$", RegexOptions.Compiled);
        // varied portion of endpoint - cannot start with number, only letters and _
        private static readonly Regex variableRegex = new Regex("^[a-zA-Z_]+[a-zA-Z0-9_]*$", RegexOptions.Compiled);
        // non-matching braces, no content between braces, open brace at end, slash between braces, multiple braces without slash,
        // or characters between slash and outside of braces
        private static readonly Regex badBracesRegex = new Regex(
            "<[^>]*<|>[^<]*>|<[^>]+$|^[^<]+>|<>|<$|<([^/][^>]*/[^/][^>]*)+>|><|>[^/]+|/[^/]+< ", RegexOptions.Compiled);
        private static readonly Regex bracesRegex = new Regex("<([^>]*)>", RegexOptions.Compiled); // find variables in endpoint
        private static readonly string replacementRegexString = "([!-\\.0-~]+)";
        #endregion

        public CommandEntry(string command, CommandSettings settings)
        {
            this.command = command;
            this.settings = settings;
        }

        public CommandEntry(string command, ServerSettings serverSettings)
        {
            this.command = command;
            this.settings = CommandSettings.CreateWithBodyMax(serverSettings.bodyMax);
        }

        public string GetCommand()
        {
            return command;
        }

        public CommandSettings GetSettings()
        {
            return settings;
        }

        public void AddEndpoint(string endpoint, EndpointEntry entry, CommandSettings endpointSettings = null)
        {
            // convert endpoint into EndpointPattern
            var endpointPattern = ConvertEndpointIntoRegex(endpoint);
            // check if endpoint already exists
            if (endpointMap.ContainsKey(endpointPattern))
                throw new EndpointManagerException($"endpoint '{endpoint}' for command '{command}' already exists; endpoints for a command must be unique");
            // set settings for endpoint to use
            var settingsToUse = endpointSettings != null ? CommandSettings.Combine(settings, endpointSettings) : settings;
            // put pattern into endpoint map
            endpointMap.TryAdd(endpointPattern, new EndpointSaved(entry, endpointPattern.GetVariables(), settingsToUse));
        }

        public EndpointValue GetEndpoint(string endpoint)
        {
            // check that endpoint is not empty
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new EndpointManagerException("endpoint cannot be empty");
            // check if using allowed characters
            if (!allowedRegex.IsMatch(endpoint))
                throw new EndpointManagerException($"endpoint '{endpoint}' contains invalid characcters");
            // remove '/' at end of endpoint
            endpoint = endSlashRegex.Replace(endpoint, "", 1);
            // add '/' to start of endpoint if not present
            if (!endpoint.StartsWith('/'))
                endpoint = '/' + endpoint;
            // otherwise replace multiple '/' at start with single
            else
                endpoint = startSlashRegex.Replace(endpoint, "/", 1);
            // check if there are multiple slashes in the middle; if so, invalid
            if (middleSlashRegex.IsMatch(endpoint))
                throw new EndpointManagerException($"endpoint cannot contain consecutive slashes: {endpoint}");
            // search endpoint map for matching endpoint
            MatchCollection matchCollection = null;
            EndpointSaved matchEndpointSaved = null;
            foreach (var key in endpointMap.Keys)
            {
                matchCollection = key.GetPattern().Matches(endpoint);
                if (matchCollection.Count > 0)
                {
                    matchEndpointSaved = endpointMap[key];
                    break;
                }
            }
            // if nothing found, endpoint doesn't exist
            if (matchCollection == null)
                throw new EndpointManagerException($"endpoint '{endpoint}' cannot be found for command '{command}'");
            // get endpoint variable values from matcher and fill out Dictionary
            var values = new Dictionary<string, string>();
            var index = 1;
            foreach (var variableName in matchEndpointSaved.GetVariables())
            {
                values.Add(variableName, matchCollection[0].Groups[index].Value);
                index++;
            }
            return new EndpointValue(matchEndpointSaved.GetEntry(), values, matchEndpointSaved.GetSettings());
        }

        public EndpointSaved RemoveEndpoint(string endpoint)
        {
            try
            {
                endpointMap.TryRemove(ConvertEndpointIntoRegex(endpoint), out var endpointSaved);
                return endpointSaved;
            } catch (EndpointManagerException)
            {
                return null;
            }
        }

        protected EndpointPattern ConvertEndpointIntoRegex(string endpoint)
        {
            // check that endpoint is not empty
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new EndpointManagerException("endpoint definition cannot be empty");
            // check if using allowed characters
            if (!allowedRegexConvert.IsMatch(endpoint))
                throw new EndpointManagerException($"endpoint definition '{endpoint}' contains invalid characters");
            // remove '/' at end of endpoint
            endpoint = endSlashRegex.Replace(endpoint, "", 1);
            // add '/' to start of endpoint if not present
            if (!endpoint.StartsWith('/'))
                endpoint = '/' + endpoint;
            // otherwise replace multiple '/' with single
            else
                endpoint = startSlashRegex.Replace(endpoint, "/", 1);
            // check if there are multiple slashes in the middle; if so, invalid
            if (middleSlashRegex.IsMatch(endpoint))
                throw new EndpointManagerException($"endpoint definition cannot contain consecutive slashes: {endpoint}");
            // check if braces are incorrect
            if (badBracesRegex.IsMatch(endpoint))
                throw new EndpointManagerException("endpoint definition contains invalid brace placement");
            // check if variables exist in endpoint, and if so store their names and replace by regex
            var bracesMatcher = bracesRegex.Matches(endpoint);
            // escape unsafe characters in endpoint
            endpoint = Regex.Escape(endpoint);
            var variableNames = new List<string>();
            foreach (Match match in bracesMatcher)
            {
                // check if found variable is valid
                var name = match.Groups[1].Value;
                if (!variableRegex.IsMatch(name))
                    throw new EndpointManagerException($"variable '{name}' for endpoint definition '{endpoint}' must start " +
                        $"with non-numerics and only contain alphanum and underscores");
                // check if it has a unique name
                if (variableNames.Contains(name))
                    throw new EndpointManagerException($"multiple instances of variable '{name}' in endpoint definition " +
                        $"'{endpoint}'; variable names in an endpoint definition must be unique");
                // store variable name
                variableNames.Add(name);
            }
            // replace variables in endpoint with regex
            foreach (var variableName in variableNames)
            {
                // add braces to either side of variable name (escape twice)
                string safeBraces = Regex.Escape(Regex.Escape($"<{variableName}>"));
                // variable contained in braces '<variable>' acts as teh string to substitute;
                // regex statement is put in its place for usage when looking up proper endpoint
                endpoint = new Regex(safeBraces).Replace(endpoint, replacementRegexString, 1);
            }
            // add regex to make sure beginning and end of string will be included
            endpoint = $"^{endpoint}$";
            // return pattern generated from endpoint
            return new EndpointPattern(new Regex(endpoint, RegexOptions.Compiled), variableNames);
        }

    }
}
