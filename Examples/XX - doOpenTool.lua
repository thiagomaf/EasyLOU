function getElement(vec, index)
    local element = nil
    local ii = 1
    for id in string.gmatch(vec, "([^,]+)") do
    	if ii == index then
 	       element = id
 	       break
 	    else 
 	    	ii=ii+1
 	    end
    end
    return element
end

function doOpenTool(instructions, item)
	local b_id   = nil
    local s_item = string.lower(item)
    
    for each_instruction in string.gmatch(instructions, "([^>]+)") do
    	FindButton("CraftingWindow", each_instruction)
		ClickButton("CraftingWindow", FINDBUTTONNAME)
		sleep(250)
    end
    
    FindButton("CraftingWindow", item)
	if s_item == "lockpicks" then b_id = getElement(FINDBUTTONNAME, 3)
	else                          b_id = getElement(FINDBUTTONNAME, 1)
	end    
	ClickButton("CraftingWindow", b_id)
end

FindItem("tinker's tool", BACKPACKID)
UseSelected(FINDITEMID)

doOpenTool("Categories>Lock and Keys>Lockpicks", "Lockpicks")

FindButton("CraftingWindow", "Craft")
ClickButton("CraftingWindow", getElement(FINDBUTTONNAME, 1))
