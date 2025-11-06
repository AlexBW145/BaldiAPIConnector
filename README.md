# BaldiAPIConnector
The connector that connects MTM101DevAPI and ThinkerAPI into removing compatability conflicts

Whatever happened, I can't do anything about it. Most of the ThinkerAPI code is messy but it does successfully patches the important stuffs like its enum extension system and its NPC/Item/Random Event creation.

See for yourself for the results! It still works.

# Prerequisites
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases)
- [MTM101BaldAPI 8.1](https://gamebanana.com/mods/383711)
- [ThinkerAPI](https://gamebanana.com/mods/606386)
- Fragile Windows (Somewhere...)
- Visual Studio

Put the BepInEx core DLLs, MTM101BaldAPI dll, the ThinkerAPI dll, and the Fragile Windows dll into a somewhere folder where it would be the place for referencing assemblies as the dll files are defaulted to be separated. (MTM101DevAPI assembly is referenced from the plugins folder from the Baldi's Basics Plus game folder to the BepInEx folder while the ThinkerAPI & Fragile Windows assembly is referenced from the root of this source code.)

Load up the solution in Visual Studio and you should be able to build.
