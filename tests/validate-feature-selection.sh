#!/usr/bin/env bash

set -euo pipefail

project="tests/FeatureSelection.proj"

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=False

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Share -p:ExpectedLoginEnabled=False -p:ExpectedShareEnabled=True

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Login%3BShare -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=True
