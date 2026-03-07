# Zelda 2 Randomizer Extras
Fork of [Zelda 2 Randomizer Community Fork](https://github.com/Ellendar/Z2Randomizer)

This fork contains a few features and tweaks added on top of the main tournament build (linked above).

## Download
Download [nightly build here](https://nightly.link/initsu/Z2Randomizer/workflows/build-latest.yaml/extras). 

## Features
- Added sliders to fine-tune weights for palace styles, biomes and climates when they are set to Random. Weight values are 0 to 3. A 0 means the option is taken out of the pool. A 3 means it's 3 times as likely to appear as 1, etc.
- Add *Vanilla Everything* biome. (See below)
- Add option to reveal Maze Island locations.
- Split Bagu as an item location from other town quest items. (In case you think it's fun with the mirror & water shuffled, but not Bagu's Note.)
- Add an option to replace shuffled town quest item locations with minor items (including the table in Saria, the fountain in Nabooru, and the down & upstab guys). This is meant to reduce the need to spend time going into towns, and favor overworld and palace exploration.
- Add an option for 50% faster Fairy when combined with Dash.

## Vanilla Everything
Vanilla Everything means that for a continent, all locations will be the exact same and contain the same items as the vanilla game. This setting **overrides other shuffle settings for the selected continent**. Setting a continent to use this enables some special behaviors:
- Item hoist is brought back throughout the seed except for 1-ups. (Link holds picked up items over his head.)
- Encounter spawn timings will be determined per continent, with Vanilla Everything continents having vanilla behavior.
- Wizard houses on Vanilla Everything continents are back to their full length.

(Fast text is still enabled.)

## Reporting bugs
If there are any bugs in this version, please open an issue in this repo. Note that it is likely that this will be less maintained than the main version.