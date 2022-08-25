#!/usr/bin/env sh

PROJECT=Absolutejam.Config

if [ -z "${API_KEY}" ]; then
  echo "API_KEY is required!"
  exit 1
fi

dotnet build -c:Release

version=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' ./${PROJECT}.fsproj)

dotnet nuget push \
  bin/Release/${PROJECT}.${version}.nupkg \
  --api-key ${API_KEY} \
  --source https://api.nuget.org/v3/index.json

