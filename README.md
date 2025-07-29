Allows for quick usage of the default PING, QUERY, and READ commands.

The LIST command is used in game to create, well... lists of items. This Mod leverages that created list to then help auto populate PING and QUERY (LOGS will create a completion list for READ)

Example:

LIST ZONE_49 RES

May create the following List which remains in effect until you run another LIST command.

MEDIPACK_342
TOOL_REFILL_PACK_887
AMMOPACK_290


You can now run

QUERY *

And your terminal will query each item in turn.

PING * 

Will attempt, to run all 3 PING commands, but only the first one will actually trigger.
However, this mod also supports Tab Completion from the generated list, so typing

PING AM

Then pressing the Tab key, will result in

PING AMMOPACK_290


If your LIST command generates duplicates of an item:

LIST AMMOPACK

AMMOPACK_290
AMMOPACK_324
AMMOPACK_686
AMMOPACK_701

Then pressing tab multiple times will simply cycle through each Item with the same Type from the LIST.

This will work with basically any item you can PING or QUERY.