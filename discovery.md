# Whalerator Documentation Discovery

When designing Whalerator, one of the primary goals was to make registries and repositories "self-documenting". With vanilla Registry, of course, you get no UI at all, but with add-on tools it's relatively easy to see repository listings, search tags, and even dig into image metadata like build timestamps, layer sizes, etc. 

What Whalerator adds is the ability to search inside the actual layers of an image, and extract content. By placing a simple `readme.md` in the root of your images, Whalerator can find and extract that document and present it to a user without them needing to pull the image, start a container, and poke around in a filesystem. Rather than needing to configure static content in a Registry front-end like Docker Hub, you can just bake that information right into your images. Whalerator even supports linking inside an image, so you can include multiple markdown documents and link between them, or reference binary content like a JPG image from your markdown.

For end users, this ensures that when they look at a given version of an image, they're seeing the docs that go *with that version* and not some other potentially imcompatible version. For devs, it means never needing to worry about keeping repository metadata up-to-date, or reconciling multiple versions and locations for documentation. Docs can go directly from versioned git objects to a built image to Registry to end user automatically.  

## Document Search

By default, Whalerator looks for `readme.md` in the root of any image, and ignores case. If your image contains multiple files that vary only by case (e.g. `README.md` and `readme.MD`), it will only use the first match. If you add your documentation as the last step in your Dockerfile, Whalerator will find it quickly and won't even need to read/download the rest of the layers in that image. 

Whalerator can be configured to search for multiple files (e.g. `readme.md` and `relnotes.md`), or to skip document discovery entirely and just show basic metadata, or vulnerability analysis if configured. See ["config.md"]("config.md") for detailed options.

## Static content

While the focus is on dynamic content embedded in images, Whalerator can also serve static content. Common examples would be a banner message on the login form (visible to anyone), a banner message on the catalog view (visible to logged-in users only), or a default document to present for images with no embedded docs of their own. Again, see ["config.md"]("config.md") for detailed options.