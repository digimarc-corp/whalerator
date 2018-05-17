# Whalerator

Whalerator is a portable front-end for Docker Registry/Distribution. It allows discovering Docker images available to you on a registry, as well as displaying embedded documentation from an image, without downloading entire images or even running Docker locally.

By default, Whalerator looks for a `readme.md` in the root of a given image, and if it finds one, it displays it with standard markdown formatting. It also understands multi-platform and cross-platform images, and can correllate tag references between many tags. Not sure which named version is `latest`? Just ask Whalerator.

Whalerator requires no special access to your Docker registry, and does not store any user credentials for normal operations. If your login has access to your registry's catalog, Whalerator will figure out the rest.

Planned or partially implemented features:

- Non fugly UI
- Themeing/whiteboxing for private registries
- ~~Options to handle users with no catalog access~~
  - ~~Static image list, with permsissions verification at runtime~~
  - ~~Specially configured catalog user~~
- Prefetching of some data to speed UI
- Image build history browser
- Image filesystem browser
- Edit functions; delete tags, images, or whole repositories (with appropriate permissions)
- Configured defaults and/or restrictions for remote registry
- In-image linking; allow relative links in markdown to reference other content within an image
