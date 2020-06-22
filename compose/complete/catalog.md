# Welcome

This demo system includes all the major components of an anonymous-access Whalerator deployment:

- [Docker Registry](https://docs.docker.com/registry/)
- [Red Hat/CoreOS Clair](https://quay.io/repository/coreos/clair?tab=info)
- [Whalerator](https://github.com/digimarc-corp/whalerator)

To load an image, just tag and push to `localhost`:

```{sh}
docker tag whalerator/whalerator localhost/whalerator
docker push localhost/whalerator
```
