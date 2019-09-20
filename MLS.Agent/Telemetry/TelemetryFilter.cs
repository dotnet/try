// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System;
using System.Linq;
using System.CommandLine;
using MLS.Agent.Telemetry.Utils;
using System.Collections;
using System.Collections.Immutable;

namespace MLS.Agent.Telemetry
{
    public class TelemetryFilter : ITelemetryFilter
    {
        private const string DotNetTryName = "dotnet-try";
        private readonly Func<string, string> _hash;

        public TelemetryFilter(Func<string, string> hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public IEnumerable<ApplicationInsightsEntryFormat> Filter(object objectToFilter)
        {
            if (objectToFilter == null)
            {
                return new List<ApplicationInsightsEntryFormat>();
            }

            var result = new List<ApplicationInsightsEntryFormat>();

            if (objectToFilter is ParseResult parseResult)
            {
                var isDotNetTryCommand = parseResult.RootCommandResult?.Token.Value == DotNetTryName;
                var tokens = parseResult?.Tokens.Where(x => x.Type != TokenType.Directive); // skip directives as we do not care right now
                var commandName = tokens?.Take(1).Where(x => x.Type == TokenType.Command).Select(x => x.Value).FirstOrDefault();

                if (isDotNetTryCommand && !String.IsNullOrWhiteSpace(commandName))
                {
                    var arguments = tokens.Skip(1);
                    if (TryFindSuccessfulRule(commandName, arguments, out var rule))
                    {
                        result.Add(CreateEntry(rule));
                    }
                }
            }

            return result.Select(r => r.WithAppliedToPropertiesValue(_hash)).ToList();
        }

        private bool TryFindSuccessfulRule(string commandName, IEnumerable<Token> arguments, out ParserFilterRule outRule)
        {
            outRule = 
                ParseResultMatchingRules
                .FirstOrDefault(x => x.Command == commandName &&
                                     x.Items.Select(item => item.Tokens)
                                            .Aggregate((item1, item2) => item1.AddRange(item2))
                                            .SequenceEqual(arguments));

            return outRule != null;
        }

        private ApplicationInsightsEntryFormat CreateEntry(ParserFilterRule rule)
        {
            var keyValues = new List<KeyValuePair<string, string>>();

            keyValues.Add(new KeyValuePair<string, string>("verb", rule.Command));

            foreach (var item in rule.Items)
            {
                switch(item)
                {
                    case ParserFilterOption opt:
                        {
                            keyValues.Add(new KeyValuePair<string, string>(opt.Key, opt.Argument));
                            break;
                        }
                    case ParserFilterArgument arg:
                        {
                            keyValues.Add(new KeyValuePair<string, string>(arg.Key, arg.Token.Value));
                            break;
                        }
                    default:
                        break;
                }
            }

            return new ApplicationInsightsEntryFormat("parser/command", new Dictionary<string, string>(keyValues));
        }

        private abstract class ParserFilterItem
        {
            public abstract ImmutableArray<Token> Tokens { get; }
        }

        private class ParserFilterOption : ParserFilterItem
        {
            public ParserFilterOption(string option, string argument, string key)
            {
                Tokens = ImmutableArray.CreateRange(
                    new Token[] { new Token(option, TokenType.Option), new Token(argument, TokenType.Argument) });
                Argument = argument;
                Key = key;
            }

            public override ImmutableArray<Token> Tokens { get; }
            public string Argument { get; }
            public string Key { get; }
        }

        private class ParserFilterArgument : ParserFilterItem
        {
            public ParserFilterArgument(string value, TokenType type, string key)
            {
                Token = new Token(value, type);
                Key = key;
                Tokens = ImmutableArray.Create(Token);
            }

            public override ImmutableArray<Token> Tokens { get; }
            public Token Token { get; }
            public string Key { get; }
        }

        private class ParserFilterRule
        {
            public ParserFilterRule(string command, IEnumerable<ParserFilterItem> arguments)
            {
                Command = command;
                Items = ImmutableArray.CreateRange(arguments);
            }

            public string Command { get; }
            public ImmutableArray<ParserFilterItem> Items { get; }
        }

        private static ParserFilterItem Opt(string option, string argument, string key)
        {
            return new ParserFilterOption(option, argument, key);
        }

        private static ParserFilterItem Arg(string value, TokenType type, string key)
        {
            return new ParserFilterArgument(value, type, key);
        }

        private static ParserFilterRule[] ParseResultMatchingRules => new ParserFilterRule[]
        {
            new ParserFilterRule("jupyter", 
                new ParserFilterItem[]{}),
            new ParserFilterRule("jupyter", 
                new ParserFilterItem[]{  Arg("install", TokenType.Command, "subcommand") }),
            new ParserFilterRule("jupyter", 
                new ParserFilterItem[]{  Opt("--default-kernel", "csharp", "default-kernel") }),
            new ParserFilterRule("jupyter", 
                new ParserFilterItem[]{  Opt("--default-kernel", "fsharp", "default-kernel") }),
        };
    }
}
