#!/bin/bash
#
# quick-n-dirty build script
# buildDocker.sh 0 1 2 whalerator/whalerator

major=$1
release="$1.$2"
revision="$1.$2.$3"

repo=${4-whalerator/whalerator}

echo "Building $repo:$revision"

hash=`git rev-parse HEAD | cut -c 1-7`

if [ -z "$(git status --porcelain)" ]; then 
  echo "Working directory clean, HEAD $hash"
else 
  echo 'Working directory dirty; commit changes before pushing a release'
  exit -1
fi

echo "Preparing to build and release version $revision ($hash)"

# build
docker build . --pull --build-arg SRC_HASH=$hash --build-arg RELEASE=$revision -t $repo:$revision -t $repo:$release -t $repo:$major -t $repo:latest

# push
#docker push $repo:$revision
#docker push $repo:$release
#docker push $repo:$major
#docker push $repo:latest

#echo -n "Done"; read