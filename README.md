# Whalerator

Whalerator is a portable front-end for Docker Registry/Distribution. It allows discovering Docker images available to you on a registry, as well as displaying embedded documentation from an image, without downloading entire images or even running Docker locally.

By default, Whalerator looks for a `readme.md` in the root of a given image, and if it finds one, it displays it with standard markdown formatting. It also understands multi-platform and cross-platform images, and can correllate tag references between many tags. Assuming the correct permissions, it can also delete images and associated tags, or entire repositories. For more, see [discovery.md]("discovery.md").

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

For some users, the only configuration you may need is to set a default registry, which can be as simple as setting a single environment variable. More likely, you will want to add things like cache, a default user, and a certificate for signing and encrypting credentials. See [config.md](/config.md) for configuration details, or [sec.md](/sec.md) for details on Docker authorization and security considerations.

Redis configuration will accept a `StackExchange.Redis` style connection string, which may specify things like default DB, timeouts, etc. in addition to the actual host.

A default catalog user and password gives Whalerator a way to scan a catalog even when the logged-in user has more limited permissions. Whalerator will still try to authorize that user against individual repositories, and only display those that they have `pull` access to. This serves as a workaround for Registy's all-or-nothing approach to catalog access.

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
