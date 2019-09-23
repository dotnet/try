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

                if (parseResult.RootCommandResult?.Token.Value == DotNetTryName)
                {
                    var mainCommand = 
                        parseResult.Tokens?.FirstOrDefault(x => x.Type == TokenType.Command);
                    var mainCommandName = mainCommand?.Value;

                    var tokens = 
                        parseResult.Tokens
                        // skip directives as we do not care right now
                        ?.Where(x => x.Type != TokenType.Directive)
                        .SkipWhile(x => x != mainCommand)
                        .Skip(1);
                    
                    var entryItems = FilterCommand(mainCommandName, tokens);
                    if (entryItems != null)
                    {
                        result.Add(CreateEntry(entryItems));
                    }
                }
            }

            return result.Select(r => r.WithAppliedToPropertiesValue(_hash)).ToList();
        }

        private Nullable<ImmutableArray<KeyValuePair<string, string>>> 
            FilterCommand(string commandName, IEnumerable<Token> tokens)
        {
            if (commandName == null || tokens == null)
            {
                return null;
            }

            return Rules.Select(rule => 
            {
                if (rule.CommandName == commandName)
                {
                    return TryMatchRule(rule, tokens);
                }
                else
                {
                    return null;
                }
            }).Where(x => x != null).FirstOrDefault();
        }

        Nullable<ImmutableArray<KeyValuePair<string, string>>> 
            TryMatchRule(CommandRule rule, IEnumerable<Token> tokens)
        {
            var entryItems = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();
            entryItems.Add(new KeyValuePair<string, string>("verb", rule.CommandName));

            var tokenQueue = new Queue<Token>(tokens);
            (Token firstToken, Token secondToken) NextItem()
            {
                if (tokenQueue.TryDequeue(out var firstToken))
                {
                    if (firstToken.Type == TokenType.Option && 
                        tokenQueue.TryPeek(out var peek) && peek.Type == TokenType.Argument)
                    {
                        return (firstToken, tokenQueue.Dequeue());
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

            var itemResult = NextItem();

            // We have a valid rule so far.
            var passed = true;

            var optionItems = rule.Items.Select(item => item as OptionItem).Where(item => item != null);
            var items = rule.Items.Except(optionItems);

            // Try not to capture values directly from the tokens.
            // Capture from the rule item.
            foreach (var item in items)
            {
                // Stop checking items since our rule already failed.
                if (!passed)
                {
                    break;
                }

                // Skip until we do not have an option.
                while (itemResult.firstToken?.Type == TokenType.Option)
                {
                    var optionItem =
                        optionItems.FirstOrDefault(x =>
                            x.Option == itemResult.firstToken.Value);

                    if (optionItem != null)
                    {
                        var value =
                            optionItem.Values.FirstOrDefault(x =>
                                x == itemResult.secondToken?.Value);
                        if (value != null)
                        {
                            entryItems.Add(new KeyValuePair<string, string>(optionItem.EntryKey, value));
                            itemResult = NextItem();
                        }
                        else
                        {
                            passed = false;
                        }
                    }
                    itemResult = NextItem();
                }

                switch (item)
                {
                    case ArgumentItem argItem:
                        {
                            if (argItem.TokenType == itemResult.firstToken?.Type &&
                                argItem.Value == itemResult.firstToken?.Value &&
                                itemResult.secondToken == null)
                            {
                                entryItems.Add(new KeyValuePair<string, string>(argItem.EntryKey, argItem.Value));
                                itemResult = NextItem();
                            }
                            else if (argItem.IsOptional)
                            {
                                itemResult = NextItem();
                            }
                            else
                            {
                                passed = false;
                            }
                            break;
                        }
                    case IgnoreItem ignoreItem:
                        {
                            if (ignoreItem.TokenType == itemResult.firstToken?.Type && 
                                itemResult.secondToken != null)
                            {
                                itemResult = NextItem();
                            }
                            else if (ignoreItem.IsOptional)
                            {
                                itemResult = NextItem();
                            }
                            else
                            {
                                passed = false;
                            }
                            break;
                        }
                    default:
                        passed = false;
                        break;
                }
            }

            // If the rule is passing at this state, check if there is no result.
            // If there is a result, the rule did not pass.
            passed = passed ? (itemResult.firstToken == null && itemResult.secondToken == null) : false;

            if (passed)
            {
                return entryItems.ToImmutable();
            }
            else
            {
                return null;
            }
        }

        private ApplicationInsightsEntryFormat CreateEntry(IEnumerable<KeyValuePair<string, string>> entryItems)
        {
            return new ApplicationInsightsEntryFormat("parser/command", new Dictionary<string, string>(entryItems));
        }

        private abstract class CommandRuleItem
        {
        }

        private class OptionItem : CommandRuleItem
        {
            public OptionItem(string option, string[] values, string entryKey)
            {
                Option = option;
                Values = values.ToImmutableArray();
                EntryKey = entryKey;
            }

            public string Option { get; }
            public ImmutableArray<string> Values { get; }
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

        private static CommandRuleItem Option(string option, string[] values, string entryKey)
        {
            return new OptionItem(option, values, entryKey);
        }

        private static CommandRuleItem Arg(string value, TokenType type, string entryKey, bool isOptional)
        {
            return new ArgumentItem(value, type, entryKey, isOptional);
        }

        private static CommandRuleItem Ignore(TokenType type, bool isOptional)
        {
            return new IgnoreItem(type, isOptional);
        }

        private static CommandRule[] Rules => new CommandRule[]
        {
            new CommandRule("jupyter",
                new CommandRuleItem[]{
                    Arg("install", TokenType.Command, "subcommand", isOptional: false) }),

            new CommandRule("jupyter",
                new CommandRuleItem[]{
                    Option("--default-kernel", new string[]{ "csharp", "fsharp" }, "default-kernel"),
                    Ignore(TokenType.Argument, isOptional: true)
                })
        };
    }
}
