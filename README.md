# Retroarch Bezel Extension for Playnite
![DownloadCountTotal](https://img.shields.io/github/downloads/ashpynov/RetroBezel/total?label=total%20downloads&style=plastic) ![DownloadCountLatest](https://img.shields.io/github/downloads/ashpynov/RetroBezel/latest/total?style=plastic) ![LatestVersion](https://img.shields.io/github/v/tag/ashpynov/RetroBezel?label=Latest%20version&style=plastic) ![License](https://img.shields.io/github/license/ashpynov/RetroBezel?style=plastic)

Retroarch Bezel is an extension for Playnite game manager and launcher to configure simple fullscreen bazel for retrogaming using Retroarch configuration.

Bezels are pictures aside of your wide screen when you are gaming old retrogames which are usually 4:3.

You may show on that area either game-styled images or console-style image. The good point to start is [The Bezel Project] (https://github.com/thebezelproject)

This plugin will configure retroarch overlay to show bezel png file above it.

Plugin search file to display next order :

- ExtraMetadata Game Folder/Bezel.png: like `.\ExtraMetadata\Games\{GameID}\bazel.png`
- Random png from ExtraMetadata Game Folder/Bezel folder like: `.\ExtraMetadata\Games\{GameID}\bazel\*.png`
- Best match png to Rom name / Game name from downloaded The Bezel Project Collection (specify location an settings). The level of fuzzy match also configurable in serttings.
- Random png from ExtraMetadata Platform folder (bezel.png or *.png from bezel subfolder) like `.\ExtraMetadata\Platform\{PlatformName}\bazel.png`


[Latest Release](https://github.com/ashpynov/RetroBezel/releases/latest)


### Technical problems and limitation

Although RetroArch supports more complex bezel configurations, such as multi-layered, animated, and control button displaying, the plugin only installs the simplest full-screen bezels.

Originaly The Bezel Project aimed for using with RetroPie even for other emulators, this plugin is created only for retroarch.


#### If you feel like supporting
I do everything in my spare time for free, if you feel something aided you and you want to support me, you can always buy me a "koffie" as we say in dutch, no obligations whatsoever...

<a href='https://ko-fi.com/ashpynov' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=3' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

If you may not use Ko-Fi in you country, it should not stop you! On [boosty](https://boosty.to/ashpynov/donate) you may support me and other creators.

