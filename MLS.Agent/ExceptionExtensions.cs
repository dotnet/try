// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Clockwise;
using WorkspaceServer.Servers.Scripting;

namespace MLS.Agent
{
    public static class ExceptionExtensions
    {
        public static int ToHttpStatusCode(this Exception exception)
        {
            switch (exception)
            {
                case BudgetExceededException budgetExceededException:

                    var firstExceededEntry = budgetExceededException.Budget.Entries.FirstOrDefault(e => e.BudgetWasExceeded);

                    if (firstExceededEntry?.Name == ScriptingWorkspaceServer.UserCodeCompletedBudgetEntryName)
                    {
                        return 417;
                    }

                    return 504;

                default:
                    return 500;
            }
        }
    }
}