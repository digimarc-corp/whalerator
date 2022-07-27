#!/bin/bash
#
# quick-n-dirty build script
# buildDocker.sh 0.1.2 whalerator/whalerator

# parse version string, discarding leading chars if any
rawver=$(echo $1 | sed 's/[^[:digit:]\.]//g' )
IFS='.' read -ra version <<< "$rawver"
if [ ${#version[@]} != 3 ]; then
    echo "Could not parse $1 as a version string!" && exit -1
fi

major=${version[0]}
minor=${version[1]}
revision=${version[2]}

version="$major.$minor.$revision"

# get target repo if supplied
repo=${2-whalerator/whalerator}

echo "Building $repo:$version"

hash=`git rev-parse HEAD | cut -c 1-7`

# verify working directory clean
if [ -z "$(git status --porcelain)" ]; then 
  echo "Working directory clean, HEAD $hash"
else 
  echo 'Working directory dirty; commit changes before pushing a release'
  exit -1
fi

echo "Preparing to build and release version $version ($hash)"

# build
docker build . --pull \
  --build-arg SRC_HASH=$hash \
  --build-arg RELEASE=$version \
  --tag $repo:$version \
  --tag $repo:$major.$minor \
  --tag $repo:$major \
  --tag $repo:latest

# push
#docker push $repo:$revision
#docker push $repo:$release
#docker push $repo:$major
#docker push $repo:latest

#echo -n "Done"; read