# Whalerator

Whalerator is a portable front-end for Docker Registry/Distribution. It allows discovering Docker images available to you on a registry, as well as displaying embedded documentation from an image, without downloading entire images or even running Docker locally.

By default, Whalerator looks for a `readme.md` in the root of a given image, and if it finds one, it displays it with standard markdown formatting. It also understands multi-platform and cross-platform images, and can correllate tag references between many tags. Assuming the correct permissions, it can also delete images and associated tags, or entire repositories.

Whalerator requires no special access to your Docker registry, and does not store any user credentials for normal operations. If your login has access to your registry's catalog, Whalerator will figure out the rest.

## Build

Official releases are available on Docker Hub:

```
docker run whalerator/whalerator
```

Or build Whalerator directly from Github:

```
docker build https://github.com/digimarc-corp/whalerator.git -t whalerator
```

You can also try out the compose samples to get a complete registry with UI, vulnerability scanning, etc:

```
(cd compose/complete && docker-compose up -d)

docker pull whalerator/whalerator
docker tag whalerator/whalerator localhost/whalerator
docker push localhost/whalerator
```

Then just point a browser at [localhost](http://localhost/)

## Production Use

Our first design goal was for Whalerator to run and be useful out-of-the-box with zero configuration. Pull the latest image (or build your own), run it, and log in.

For some users, the only configuration you may need is to set a default registry, which can be as simple as setting a single environment variable. More likely, you will want to add things like cache, a default user, and a certificate for signing and encrypting credentials. See [config.md](/config.md) for details.

Redis configuration will accept a `StackExchange.Redis` style connection string, which may specify things like default DB, timeouts, etc. in addition to the actual host.

A default catalog user and password gives Whalerator a way to scan a catalog even when the logged-in user has more limited permissions. Whalerator will still try to authorize that user against individual repositories, and only display those that they have `pull` access to. This serves as a workaround for Registy's all-or-nothing approach to catalog access.

Certificates should be PEM-encoded, and need not be signed. **This certificate is not used for SSL/TLS**. It is exclusively for encrypting and signing sessions, and is critical if you plan to run multiple instances of Whalerator in a clustered environment. SSL/TLS support requires a separate front-end component, such as `nginx`, to serve as a reverse proxy.

## Security Notes

Docker Regsitry authorization is a subject unto itself. In theory the Docker client and Registry both support OAuth, but in reality Registry just points to an external token service that end users must supply themselves. There are open source implementations available for the token service, such as ["Docker Auth"](https://github.com/cesanta/docker_auth), but Docker Hub uses its own proprietary system.

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

## Attribution

Assets:

- ["Anchors Away"](https://www.heropatterns.com/) - [Steve Schoger](https://dribbble.com/steveschoger) - [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)
- ["Bouy, etc"](https://www.zondicons.com/) - [Steve Schoger](https://dribbble.com/steveschoger) - [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)

.NET Core packages:

- [vstest](https://github.com/microsoft/vstest/) - [MIT](https://opensource.org/licenses/MIT)
- [jose-jwt](https://github.com/dvsekhvalnov/jose-jwt) - [MIT](https://opensource.org/licenses/MIT)
- [ASP.net Extensions](https://github.com/aspnet/Extensions) - [Apache-2.0](http://www.apache.org/licenses/LICENSE-2.0.html)
- [Newtonsoft.JSON](https://github.com/JamesNK/Newtonsoft.Json) - [MIT](https://opensource.org/licenses/MIT)
- [BouncyCastle](https://github.com/onovotny/bc-csharp) - [BouncyCastle/MIT](https://www.bouncycastle.org/license.html)
- [SharpZipLib](https://github.com/PingmanTools/SharpZipLib) - [MIT](https://opensource.org/licenses/MIT)
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - [MIT](https://opensource.org/licenses/MIT)
- [Netescapades.Configuration](https://github.com/andrewlock/NetEscapades.Configuration) - [MIT](https://opensource.org/licenses/MIT)
- [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis/) - [MIT](https://opensource.org/licenses/MIT)


Node packages: [attribution.txt](web/oss-attribution/attribution.txt)
