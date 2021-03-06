/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

export class History {
    constructor(obj?: any) {
        Object.assign(this, obj);
    }

    public command: string;
    public created: string;
    public type: string;
    public args: string;

    public static From(obj: any): History {
        const history = new History();
        history.command = obj.shortCommand;
        history.created = obj.created;
        history.type = obj.type;
        history.args = (obj.commandArgs) ? obj.commandArgs.trim() : null;
        return history;
    }
}
