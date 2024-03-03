#!/bin/sh
mkdir -p ./AppDir/usr/bin
#cp ../bin/Release/net8.0/linux-x64/publish/* AppDir/usr/bin
chmod 0755 ./AppDir/AppRun
chmod +x ./AppDir/AppRun
wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
chmod a+x appimagetool-x86_64.AppImage
./appimagetool-x86_64.AppImage ./AppDir
