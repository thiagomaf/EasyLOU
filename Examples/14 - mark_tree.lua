-- stand in front of a tree and play the macro
-- each time the macro is played it will add a command line to the specified file
-- results should be used in the 06 - lumberjack.lua macro

file = io.open ("E:\\rail.txt", "a")
file.write("doTree(\"" .. CHARPOSX .. "\", \"" .. CHARPOSY .. "\",  \"" .. CHARPOSZ .. "\")" .. "\n")
file.close()
