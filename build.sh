#!/bin/sh

version=10.0.9

./test.sh
./release.sh $version
./pack.sh $version