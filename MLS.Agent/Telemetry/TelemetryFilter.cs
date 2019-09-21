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
                    if (CheckCommand(commandName, arguments, out var ruleItems))
                    {
                        result.Add(CreateEntry(commandName, ruleItems));
                    }
                }
            }

            return result.Select(r => r.WithAppliedToPropertiesValue(_hash)).ToList();
        }

        private bool CheckCommand(string commandName, IEnumerable<Token> arguments, out ImmutableArray<CommandRuleItem> outRuleItems)
        {
            var ruleItems = ImmutableArray.CreateBuilder<CommandRuleItem>();

            var passed = false;

            foreach (var rule in ParseResultMatchingRules)
            {
                // We have a rule that passed; we are done.
                if (passed)
                {
                    break;
                }

                if (rule.CommandName == commandName)
                {
                    // We have a valid rule so far.
                    passed = true;

                    var tokens = new Queue<Token>(arguments);

                    var matchResult = NextItemMatch(tokens);

                    foreach (var item in rule.Items)
                    {
                        // Stop checking items since our rule already failed.
                        if (!passed)
                        {
                            break;
                        }

                        switch (item)
                        {
                            case OptionItem opt:
                                {
                                    if (matchResult.firstToken?.Type == TokenType.Option &&
                                        opt.Option == matchResult.firstToken?.Value && 
                                        (matchResult.secondToken == null || matchResult.secondToken.Type == TokenType.Argument) &&
                                        (String.IsNullOrEmpty(opt.Value) || opt.Value == matchResult.secondToken?.Value))
                                    {
                                        ruleItems.Add(item);
                                        matchResult = NextItemMatch(tokens);
                                    }
                                    break;
                                }
                            case ArgumentItem arg:
                                {
                                    if (arg.TokenType == matchResult.firstToken?.Type && 
                                        arg.Value == matchResult.firstToken?.Value &&
                                        matchResult.secondToken == null)
                                    {
                                        ruleItems.Add(item);
                                        matchResult = NextItemMatch(tokens);
                                    }
                                    else if (arg.IsOptional)
                                    {
                                        matchResult = NextItemMatch(tokens);
                                    }
                                    else
                                    {
                                        passed = false;
                                    }
                                    break;
                                }
                            case IgnoreItem ignore:
                                {
                                    if (ignore.TokenType == matchResult.firstToken?.Type && matchResult.secondToken != null)
                                    {
                                        matchResult = NextItemMatch(tokens);
                                    } 
                                    else if (ignore.IsOptional)
                                    {
                                        matchResult = NextItemMatch(tokens);
                                    }
                                    else
                                    {
                                        passed = false;
                                    }
                                    break;
                                }
                            default:
                                break;
                        }
                    }

                    // If the rule is passing at this state, check if there is no match result.
                    // If there is a match result, the rule did not pass.
                    passed = passed ? (matchResult.firstToken == null && matchResult.secondToken == null) : false;
                }
            }

            outRuleItems = ruleItems.ToImmutable();

            return passed;

            (Token firstToken, Token secondToken) NextItemMatch(Queue<Token> tokens)
            {
                if (tokens.TryDequeue(out var firstToken))
                {
                    if (firstToken.Type == TokenType.Option && tokens.TryPeek(out var peek) && peek.Type == TokenType.Argument)
                    {
                        return (firstToken, tokens.Dequeue());
                    }
                    else
                    {
                        return (firstToken, null);
                    }
                }
                else
                {
                    return (null, null);
                }
            }
        }

        private ApplicationInsightsEntryFormat CreateEntry(string commandName, IEnumerable<CommandRuleItem> ruleItems)
        {
            var keyValues = new List<KeyValuePair<string, string>>();

            keyValues.Add(new KeyValuePair<string, string>("verb", commandName));

            foreach (var item in ruleItems)
            {
                switch(item)
                {
                    case OptionItem opt:
                        {
                            keyValues.Add(new KeyValuePair<string, string>(opt.EntryKey, opt.Value));
                            break;
                        }
                    case ArgumentItem arg:
                        {
                            keyValues.Add(new KeyValuePair<string, string>(arg.EntryKey, arg.Value));
                            break;
                        }
                    default:
                        break;
                }
            }

            return new ApplicationInsightsEntryFormat("parser/command", new Dictionary<string, string>(keyValues));
        }

        private abstract class CommandRuleItem
        {
        }

        private class OptionItem : CommandRuleItem
        {
            public OptionItem(string option, string value, string entryKey)
            {
                Option = option;
                Value = value;
                EntryKey = entryKey;
            }

            public string Option { get; }
            public string Value { get; }
            public string EntryKey { get; }
        }

        private class ArgumentItem : CommandRuleItem
        {
            public ArgumentItem(string value, TokenType type, string entryKey, bool isOptional)
            {
                Value = value;
                TokenType = type;
                EntryKey = entryKey;
                IsOptional = isOptional;
            }

            public string Value { get; }
            public TokenType TokenType { get; }
            public string EntryKey { get; }
            public bool IsOptional { get; }
        }

        private class IgnoreItem : CommandRuleItem
        {
            public IgnoreItem(TokenType type, bool isOptional)
            {
                TokenType = type;
                IsOptional = isOptional;
            }

            public TokenType TokenType { get; }
            public bool IsOptional { get; }
        }

        private class CommandRule
        {
            public CommandRule(string commandName, IEnumerable<CommandRuleItem> items)
            {
                CommandName = commandName;
                Items = ImmutableArray.CreateRange(items);
            }

            public string CommandName { get; }
            public ImmutableArray<CommandRuleItem> Items { get; }
        }

        private static CommandRuleItem Opt(string option, string argument, string entryKey)
        {
            return new OptionItem(option, argument, entryKey);
        }

        private static CommandRuleItem Arg(string value, TokenType type, string entryKey, bool isOptional)
        {
            return new ArgumentItem(value, type, entryKey, isOptional);
        }

        private static CommandRuleItem Ignore(TokenType type, bool isOptional)
        {
            return new IgnoreItem(type, isOptional);
        }

        private static CommandRule[] ParseResultMatchingRules => new CommandRule[]
        {
            new CommandRule("jupyter",
                new CommandRuleItem[]{  Arg("install", TokenType.Command, "subcommand", isOptional: false) }),

            new CommandRule("jupyter", 
                new CommandRuleItem[]{
                    Opt("--default-kernel", "csharp", "default-kernel"),
                    Opt("--default-kernel", "fsharp", "default-kernel"),
                    Opt("--default-kernel", String.Empty, "default-kernel"),
                    Ignore(TokenType.Argument, isOptional: true)
                })
        };
    }
}
