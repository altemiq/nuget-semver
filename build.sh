#!/bin/sh

version=10.0.2

./test.sh
./release.sh $version
./pack.sh $version