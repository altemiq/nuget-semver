#!/bin/sh

version=10

./test.sh

./release.sh $version

./pack.sh $version