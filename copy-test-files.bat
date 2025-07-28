@echo off
echo Copying required files for integration tests...

copy "E7GEZLY API\bin\Debug\net8.0\E7GEZLY API.deps.json" "E7GEZLY_API.Tests\bin\Debug\net8.0\testhost.deps.json" /Y
copy "E7GEZLY API\bin\Debug\net8.0\E7GEZLY API.runtimeconfig.json" "E7GEZLY_API.Tests\bin\Debug\net8.0\testhost.runtimeconfig.json" /Y

echo Files copied successfully!