# Whalerator

Whalerator is a portable front-end for Docker Registry/Distribution. It allows discovering Docker images available to you on a registry, as well as displaying embedded documentation from an image, without downloading entire images or even running Docker locally.

By default, Whalerator looks for a `readme.md` in the root of a given image, and if it finds one, it displays it with standard markdown formatting. It also understands multi-platform and cross-platform images, and can correllate tag references between many tags. Not sure which named version is `latest`? Just ask Whalerator.

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

## Productionizing

Our first design goal was for Whalerator to run and be useful out-of-the-box with zero configuration. Pull the latest image (or build your own), run it, and log in. 

For some users, the only configuration you may need is to set a default registry, which can be as simple as setting a single environment variable. More likely, you will want to add things like persistent cache, a default catalog user, and a certificate for signing and encrypting credentials. See (config.yaml)[/app/config.yaml] for examples.

Redis configuration will accept a `StackExchange.Redis` style connection string, which may specify things like default DB, timeouts, etc. in addition to the actual host.

A default catalog user and password gives Whalerator a way to scan a catalog even when the logged-in user has more limited permissions. Whalerator will still try to authorize that user against individual repositories, and only display those that they have `pull` access to. This serves as a workaround for Registy's all-or-nothing approach to catalog access.

Certificates should be PEM-encoded, and need not be signed. **This certificate is not used for SSL/TLS**. It is exclusively for encrypting and signing sessions, and is critical if you plan to run multiple instances of Whalerator in a clustered environment. SSL/TLS support requires a separate front-end component, such as `nginx`, to serve as a reverse proxy.

## Security Notes

Docker uses a relatively "chatty" security protocol. Accessing each repository on a Docker Registry requires a fresh set of credentials, and most systems (including Docker Hub) do not support true OAuth with long-lived tokens and/or refreshes. That means every time you request something, you need to send a username and password, which means Whalerator needs to save your username and password as part of your session.

In the default configuration, Whalerator generates an RSA keypair at startup (you should generate and configure your own key for a production deployment). When you log in, your credentials are forwarded to the remote Registry server for validation. If they are accepted, Whalerator encrypts and signs them with it's private key and sends them back to your browser as part of a token. That token then becomes your session handle; Whalerator caches grants, but your encrypted token is required to access them. When no cached grant is available, the key in your token is decrypted and forwarded to the remote server again. The encrypted data is never stored on the server, and the encryption keys are never shared with the client.

## Attribution

Assets:

- ["Anchors Away"](https://www.heropatterns.com/) - [Steve Schoger](https://dribbble.com/steveschoger) - [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)
- ["Bouy"](https://www.zondicons.com/) - [Steve Schoger](https://dribbble.com/steveschoger) - [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)

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
