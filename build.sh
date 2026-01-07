#!/bin/sh

version=10.0.3

./test.sh
./release.sh $version
./pack.sh $version