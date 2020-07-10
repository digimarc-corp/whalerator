# Security Notes

Docker Registry authorization is a subject unto itself. In theory the Docker client and Registry both support OAuth, but in reality Registry just points to an external token service that end users must supply themselves. There are open source implementations available for the token service, such as ["Docker Auth"](https://github.com/cesanta/docker_auth), but Docker Hub uses its own proprietary system.

The flow is something like:

### Fully anon Registry

- ➡ Request resource from Registry
- ⬅ Receive resource

### Registry + token service

- ➡ Request resource from Registry
- ⬅ Registry returns `WWW-Authenticate` challenge specifying `Bearer` auth, a token service to use, and a scope to request
- ➡ Request token from token service
- ⬅ Token service return `WWW-Authenticate` challenge specifying `Basic` auth
- ➡ Request token, with encoded username:password
- ⬅ Receive opaque token (typically JWT, but this is not guaranteed)
- ➡ Request resource from Registry, sending Bearer token
- ⬅ Receive resource

### Docker hub

- ➡ Request resource from Registry
- ⬅ Registry returns `WWW-Authenticate` challenge specifying `Bearer` auth, a token service to use, and a scope to request
- ➡ Request token from token service
- ⬅ Token service returns JWT, which must be parsed to see if it contains actual permissions (anonymous repos) or not (private repos)
- ➡ If no permissions, re-request token using `Basic` auth. Note there is no actual challenge step here; you must know how to parse the token and how to fall back to `Basic` auth for private repos.
- ⬅ (optionally) Receive updated JWT
- ➡ Request resource from Registry, sending JWT as Bearer token
- ⬅ Receive resource

Theoretically the token service can challenge with other mechanisms like `Digest` or another `Bearer` scheme, but in practice `Basic` seems to be it. The token service can also theoretically provide a refresh token but this does not seem to be widely supported, even by Docker Hub. This process must be repeated for each resource on the Registry; e.g. the catalog list and push/pull/admin rights on each repository.

So, practically speaking, every resource we look at in Registry needs to use `Basic` auth, which means keeping a username and password around, connected to a Whalerator session. To keep this as secure as possible, when a user logs in to Whalerator with their Docker username & password Whalerator encrypts those credentials using an RSA private key. By default a 2048-bit key is generated at startup, or you may generate a standard PEM-format private key and supply it via `config.yaml` to allow sessions to work across multiple instances. The encrypted credentials are returned to the client and used as a bearer token for further requests. When the user makes an authenticated request via the Whalerator browser client (or some other RESTful client), the credentials are decrypted and forwarded to the Docker token service, but never actually stored on the server. The final token received from the token service is cached locally, using a SHA256 hash.
