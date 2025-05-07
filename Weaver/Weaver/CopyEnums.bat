@echo off
pushd "%~dp0"
copy /Y "TrixxInjection.Framework\Enums.cs" "TrixxInjection.Fody\Enums.cs"
copy /Y "TrixxInjection.Framework\Configurator.cs" "TrixxInjection.Fody\Configurator.cs"
popd
