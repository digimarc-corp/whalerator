export class VersionSort {
    // Sorts string using assumptions about version syntax. Strings which look like 'v1.0' or '1.0' will sort ahead of
    // non-version strings, and will use descending ordering. Other strings will use ascending standard alpha sort. Case insensitive.
    public static sort(a: string, b: string): number {
        const version = /^[vV]?[0-9\.]+/;

        if (version.test(a) && !version.test(b)) { return 1; }
        if (version.test(b) && !version.test(a)) { return -1; }
        if (version.test(a) && version.test(b)) {
            return a.toLowerCase() === b.toLowerCase() ? 0 : a.toLowerCase() < b.toLowerCase() ? 1 : -1;
        } else {
            return a.toLowerCase() === b.toLowerCase() ? 0 : b.toLowerCase() < a.toLowerCase() ? 1 : -1;
        }
    }
}
