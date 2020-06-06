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

export class SemverSort {

    /* Sorts strings using semver rules and common Docker tag conventions.
     * - "latest"
     * - "latest-*" in alpha order
     * - Semver versions, without prerelease info, with or without a leading 'v' or 'V'.
     *      - Less specific versions beat more specific ('1.2' comes before '1.2.3')
     *      - Any version with info beyond the revision ('1.2.3beta', '1.2.3-a5', etc) is considered prerelease
     *      - Patch versions ('1.2.3.4') fall under prerelease
     * - Semver prerelease versions, sorted by base version and then alpha
     * - Alpha
     */
    public static sort(a: string, b: string): number {
        const version = /^[vV]?(?:(?<major>\d+)\.?)(?:(?<minor>\d+)\.?)?(?<rev>\d+)?(?<pre>.+)?$/;
        const latest = /^latest.+/;

        if (a === b) { return 0; }

        // latest first
        if (a.toLowerCase() === 'latest') { return -1; }
        if (b.toLowerCase() === 'latest') { return 1; }

        // qualified latest next
        if (latest.test(a) && !latest.test(b)) { return -1; }
        if (latest.test(b) && !latest.test(a)) { return 1; }
        if (latest.test(a) && latest.test(b)) { return a > b ? 1 : -1; }

        // versions ahead of non-versions
        if (version.test(a) && !version.test(b)) { return -1; }
        if (version.test(b) && !version.test(a)) { return 1; }
        if (version.test(a) && version.test(b)) {
            // the meaty part - descend the semver parts, and compare individually
            const versionA = version.exec(a);
            const versionB = version.exec(b);

            if (!versionA.groups.pre && versionB.groups.pre) { return -1; }
            if (!versionB.groups.pre && versionA.groups.pre) { return 1; }

            const major = SemverSort.compareVersionPart(versionA.groups.major, versionB.groups.major);
            const minor = SemverSort.compareVersionPart(versionA.groups.minor, versionB.groups.minor);
            const rev = SemverSort.compareVersionPart(versionA.groups.rev, versionB.groups.rev);
            const pre = versionA.groups.pre > versionB.groups.pre ? 1 : -1;

            if (major === 0) {
                if (minor === 0) {
                    if (rev === 0) {
                        return pre;
                    } else {
                        return rev;
                    }
                } else {
                    return minor;
                }
            } else {
                return major;
            }
        }

        // last, just do alpha comparison
        return a > b ? 1 : -1;
    }

    static compareVersionPart(a: string, b: string): number {
        if (a && !b) { return 1; }
        if (b && !a) { return -1; }

        const aa = parseInt(a, 10);
        const bb = parseInt(b, 10);

        return bb - aa;
    }
}
