# swgoh-ui
A simple guild inspection/management tool for the mobile game "Star Wars: Galaxy of Heroes".

This program fetches information about a guild and its members, and
provides several tools potentially useful for guild management.

Note: This program was mostly thrown together over a weekend and is neither
polished nor bug-free.  Bug reports welcome.

## Setup

Before using this program, you must register an account on https://api.swgoh.help.
Individual guild members don't need to register, only the person using
this program.  Once you register, you will need four pieces of information:

 - User name
 - Password
 - User ID (click the 'My Settings' page of your swgoh.help profile page to find it)
 - Ally code (found in-game)


## Using the Program

Enter the four pieces of information above into the program and click the
"Fetch Data" button.  The program will pull information for your guild and
all of its members from the swgoh.help service.  This can be a slow process.
The status bar at the bottom will indicate what the program is currently doing.
Once all of the information has been successfully retrieved, the status bar
will read "Ready" and the UI will display your player name and guild name.
Please do not try to use any of the program's other features until the data
retrieval process is complete.

To avoid the delays associated with retrieving this data, you can use the
"File -> Export" command to export the current data set to a file.  The
next time you use the program, use "File -> Import" to load the exported
data instead of retrieving it from the web service.


## Tools


The following tools are currently available as part of this program.

### Guild Roster

This tool displays a chart showing all guild members along with their Galactic
Power and other selected stats.  Sort the chart by clicking on a column header.
Double-clicking on a player name will show detailed information about that
player.

The guild roster overview can be exported to a .csv file by clicking the
'export' button above the table.  This can be useful for tracking guild members'
progress over time.

The "Roster Report" button will generate a .csv file listing every guild member
that has certain squads.  The squads that are checked for are the squads listed
in the "presets.csv" file (see the "Squad Checker" section for details).  The
user will be prompted with additional filtering options, like specifying a
minimum gear level or character power.  When a player has a squad that meets
the specified criteria, the corresponding entry in the report will list that
squad's total power.  A blank entry indicates that the criteria were not met
or that the player does not have all of that squad's characters unlocked.

### Who Has

This tool enables you see who in the guild has a certain character.  Select a
character and the program will generate a list showing all members who have
that character, plus some details about how powerful that character is.

### Squad Checker

This tool enables you to build a squad of five characters.  It will then
generate a list of who in the guild has that particular squad, and how
powerful that squad is.

To make squad selection easier, create preset squads.  Presets are defined in
the "presets.csv" file.  The first entry on each line is a name for the squad,
followed by the full names (as they appear in-game) of the characters in the
squad.  Squads listed in this file will appear in the "Presets" menu of the
Squad Checker tool and will be used when generating a Roster Report.

### Pit Challenge Report

This tool filters the guild's roster to show only those units eligible to be
used in the Challenge mode of the Pit raid. Use this view to help decide whether
your guild is ready for Challenge mode and to help plan strategy.

### Alliance Overview

This tool provides an automated way to fetch basic summary data about the
various guilds in a multi-guild alliance.  Ally codes for one player from each
guild are required.  The list of ally codes will be remembered between sessions.

### Zeta Rankings

This tool displays a ranked list of zeta abilities.  Abilites have separate
rankings based on their usefulness in different aspects of the game.

Note: Ranking data is manually curated based on the opinions and selections of
a select group of top-tier players.  The manual nature of this data means that
it will typically not include recently-released characters.


## Misc. Notes

Fetching data from the swgoh.help service can be slow.  If this becomes a
problem, you can get priority access to the service by supporting swgoh.help
on Patreon (see their site for details).

Changes made in-game will not show up in the program instantly.  The swgoh.help
servers *do* refresh their data periodically, but free users only get updates
every few hours.  Support them on Patreon to get more frequent updates.  For
most purposes, though, the data available to free accounts is accurate enough.

The 'Who Has' and 'Squad Checker' tools get their list of characters by
searching through the rosters of the guild's members.  If a character does not
appear in the list, then nobody in the guild has unlocked that character.

The files created by the 'Export' command can be a bit large.  You probably
don't want to try and open them using Notepad, as Notepad has a habit of
hanging or crashing on large files.

The program pulls guild data based on the ally code.  If you use your own
ally code, you'll get data on your guild.  If you use someone else's ally
code, you'll get data on *their* guild.  This could potentially be useful
for scouting an opposing Territory War guild's capabilities before planning
your defense.  Currently, there's no way to pull data on a guild without
knowing the ally code of a member.


## Building

This program was created with Visual Studio 2019.  To build it, you will
need to have installed the VS components for "desktop development" and
for the .NET framework 4.7.


## License

This program is made freely available under the following license:

    Copyright 2020 Ben Allen
    
    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at
    
       http://www.apache.org/licenses/LICENSE-2.0
    
    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

## Disclaimer

This program is community-made and is in no way affiliated with Lucasfilm,
Disney, EA, Capital Games, or the swgoh.help team.
