# Configuration

Whalerator can be started with no configuration at all, however this will leave many features disabled, like documentation discovery and static analysis. At the other end of the spectrum, you'll want multiple instances including multiple API instances and separate workers for indexing and security scanning, in which case all components will need configuration to work together correctly.

Whalerator configuration is normally applied via a `config.yaml` file. Individual options can be overridden via environment variables. For example, to override the `redisCache` setting from a `config.yaml`, just set the `REDISCACHE` environment variable. For details see [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1).

Configuration options do not need to be in any particular order, and extra options (or misspelled options) will be ignored, as long as the file follows standard YAML formatting.

## Startup options

- `--rescan` or `-r` causes Whalerator to enumerate all repositories, images, and tags in the configured registry and submit them for indexing and/or scanning.
- `--exit` or `-x` causes Whalerator to load configuration, process a rescan if requested, and then exit.
- `--nobanner` causes Whalerator to skip printing the startup banner.

## Cache Options

```{yaml}
# A StackExchange.Redis connection string. If not specified, and internal memcache will be used
redisCache: redishost

# Lifetime for cached objects. Specified as common intervals (5m, 0.5h, 2d, etc.). If unit is omitted, assumes ms. Default is 1h.
cacheTtl: 1h

# Enable local caching. This causes additional objects to be cached, moving load from the local filesystem to the configured cache.
localCache: false
```

## UI Options

These options affect the UI presented in the browser.

```{yaml}
# Visual themes offered to the end user
themes:
  - name: 'Name'
  - style: 'style.css'

# If true, the security tab will be shown. A security scan worker must be configured to actually generate results. Default is false.
vulnerabilities: true

# If true, the UI will try to automatically log in an anonymous user. A default registry supporting anonymous access must be configured. Default is false.
autoLogin: false
```

## Logging

These options affect logging.

```{yaml}
# Threshold for logging whalerator events. One of Trace, Debug, Information, Warning, Error, or Critical. Default is Warning.
logLevel: "Warning"

# Threshold for logging ASP.NET Core events. One of Trace, Debug, Information, Warning, Error, or Critical. Default is Warning.
msLogLevel: "Warning"

# If true, a log header is printed sampling output formats for various log levels. Default is false.
logHeader: false

# If true, exceptions will include a stack trace. Default is false.
logStack: false
```

## Document options

These options control the discovery of embedded and static documents like a `readme.md` or a banners.

```{yaml}
# Controls static content presented on the login or catalog views. Can be either a local file path, or inline markdown
loginBanner:
catalogBanner:

# Controls static content presented on the repository view when an image does not contain any in-built documentation.
# An entry can be just a filename/markdown, or a path + regex pattern pair. The first document to match a given repo name will be used.
staticDocs:
  - path: default-readme.md
    pattern: "whalerator"
  - "# Default Readme"

# Filesystem location for generate image indexes. This location must be shared between all instances. Defaults to a local temp folder.
indexFolder:

# Maximum number of layers to download and index while looking for documentation. If 0, all layers may be downloaded. Default is 0.
maxSearchDepth { get; set; }

# If true, all layers in an image will be indexed, even if target documents have already been located.
# If used where the Registry instance is remote, with no shared registryRoot and no maxSearchDepth, this may cause Whalerator to download excessive
# amounts of layer data
deepIndexing: true

# A list of documents to look for when indexing. If all target documents can be found before maxSearchDepth is reached, indexing will halt. If this list is empty, the documents tab will not be shown in the UI.
documents:
  - readme.md
  - relnotes.md
  - license.md

# use case-insensitive rules for document searching. Default is true.
caseInsensitive: true
```

## Security options

Options that control authentication both with the Whalerator service, and the remote Docker Registry service

```{yaml}
# The lifetime for a session token. The default is 2419200 (4 weeks)
authTokenLifetime:

# Path to a PEM-formatted private key for signing and encrypting session tokens. If left blank, a key will be generated at runtime.
authTokenKey { get; set; }

# Username and password to use when connecting to the remote Docker Registry instance. These credentials will be used only to fetch a list of all available repositories - individual users must still authenticate and have "pull" or better permissions to a repository to see or interact with it. This is useful if not all users will have catalog permissions, but you still want to show them those repositories they do have access to.
registryUser { get; set; }
registryPassword { get; set; }
```

## Registry options

Options controlling Docker Registry

```{yaml}
# The default Docker Registry instance to connect to. This should be the "public" name, as you would use with a `docker login` command. In the case of Docker Hub, this can be any one of several common names like hub.docker.com or docker.io. These will all be translated to the canonical registry-1.docker.io
registry: myregistry

# A list of registry aliases. These are important if Whalerator and your Registry will run inside a cluster, where they will need to reach one another by different host names than a "public" user would.
registryAliases:
  - registry: myregistry
    alias: cluster_registry:5000

# Root folder for your Docker Registry instance. If configured, this allows Whalerator to bypass the Registry API for many functions.
registryRoot: /var/lib/registry

# Registry cache folder. If no registryRoot is configured, layers and manifests will be downloaded to this folder as needed to enable indexing, etc. Should be a shared location with all Whalerator instances. Default is a local temp folder.
registryCache:

# A list of static repository names that should always be displayed to users, even if no catalog is available. Users must still have "pull" permissions, and the repository must actually exist. Most useful when working against Docker Hub.
repositories:
  - myrepo
  - some/other/repo

# If true, Whalerator will accept Registry event webhook calls at /api/events. Push events will cause indexing and/or scanning tasks to be queued
# for those images. See: https://docs.docker.com/registry/notifications/. Default is false.
eventSink: false

# If set, event webhooks must include the supplied value in the Authorization header. This value is static and separate from any other
# authorization mechanism in Whalerator or Docker Registry
eventAuthorization:
```

## Workers

Options controlling worker processes

```{yaml}
# If true, this instance will process indexing requests
indexWorker: true

# If true, this instance will process static analysis requests. clairApi must be configured for the worker to start.
clairWorker { get; set; }

# Url to connect to CoreOS Clair for image analysis. Whalerator currently only supports the Clair V2 API.
clairApi: http://clair:6060
```
