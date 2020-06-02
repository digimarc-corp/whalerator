# Configuration

Whalerator can be started with no configuration at all, however this will leave many features disabled, like documentation discovery and static analysis. At the other end of the spectrum, you'll want multiple instances including multiple API instances and separate workers for indexing and security scanning, in which case all components will need configuration to work together correctly.

Whalerator configuration is normally applied via a `config.yaml` file. Individual options can be overridden via environment variables. For example, to override the `redisCache` setting from a `config.yaml`, just set the `REDISCACHE` environment variable. For details see [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1).

Configuration options do not need to be in any particular order, and extra options (or misspelled options) will be ignored, as long as the file follows standard YAML formatting.

## Cache Options

```{yaml}
# A StackExchange.Redis connection string. If not specified, and internal memcache will be used
redisCache: redishost

# Lifetime for cached objects. Default is 3600 (1 hour)
cacheTtl: 3600
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

## Document options

These options control the discovery of embedded documents like a `readme.md`.

```{yaml}
# Filesystem location for generate image indexes. This location must be shared between all instances. Defaults to a local temp folder.
indexFolder:

# Maximum number of layers to download and index while looking for documentation. If 0, all layers may be downloaded. Default is 0.
maxSearchDepth { get; set; }

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
