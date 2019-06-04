// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Project } from "../project";
import { ISession } from "../session";

export type Scaffolding = "method" | "class";

export function createScaffoldingProject(session: ISession, packageName: string, scaffolding: Scaffolding): Promise<Project> {
    throw new Error("not implemented");
}