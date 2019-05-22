// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Action, CONFIGURE_CLIENT, CONFIGURE_CODE_SOURCE, CONFIGURE_COMPLETION_PROVIDER, CONFIGURE_VERSION, CONFIGURE_BLAZOR, NOTIFY_HOST_PROVIDED_CONFIGURATION, ENABLE_TELEMETRY, CONFIGURE_EDITOR_ID } from "../constants/ActionTypes";
import { IConfigState } from "../IState";

const defaultCodeFragment = `using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
  public static void Main()
  {
    foreach (var i in Fibonacci().Take(20))
    {
      Console.WriteLine(i);
    }
  }

  private static IEnumerable<int> Fibonacci()
  {
    int current = 1, next = 1;

    while (true) 
    {
      yield return current;
      next = current + (current = next);
    }
  }
}
`;
const initialState: IConfigState = {
    client: undefined,
    completionProvider: "roslyn",
    from: "default",
    defaultWorkspace: {
        workspaceType: "script",
        files: [],
        buffers: [{ id: "Program.cs", content: defaultCodeFragment, position: 0 }],
        usings: [],
    },
    defaultCodeFragment,
    version: 1,
    hostOrigin: undefined,
    applicationInsightsClient: undefined
};

export default function configReducer(state: IConfigState = initialState, action: Action): IConfigState {
    if (!action) {
        return state;
    }

    switch (action.type) {
        case CONFIGURE_CLIENT:
            return {
                ...state,
                client: action.client
            };
        case CONFIGURE_CODE_SOURCE:
            return {
                ...state,
                from: action.from,
                defaultCodeFragment: action.sourceCode
            };
        case CONFIGURE_VERSION:
            return {
                ...state,
                version: action.version
            };
        case CONFIGURE_COMPLETION_PROVIDER:
            return {
                ...state,
                completionProvider: action.completionProvider
            };
        case CONFIGURE_BLAZOR:
            return {
                ...state,
                useLocalCodeRunner: true
            };
        case NOTIFY_HOST_PROVIDED_CONFIGURATION:
            return {
                ...state,
                hostOrigin: action.configuration.hostOrigin
            };
        case ENABLE_TELEMETRY:
            return {
                ...state,
                applicationInsightsClient: action.client
            };
        case CONFIGURE_EDITOR_ID:
            return {
                ...state,
                editorId: action.editorId
            };
        default:        
            return state;
    }
}
