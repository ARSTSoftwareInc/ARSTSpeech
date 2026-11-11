@echo off
color a
title Virtualization init
echo Initializing scanner...
echo Transfering start
start await 20 taskkill /im vrtler.exe /f
vrtler.exe ARST root
echo Virtualization!
await 3 cmd /k taskkill /im await.exe /f || taskkill /im cmd.exe /f