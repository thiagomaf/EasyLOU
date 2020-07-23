function ExtractLastTooltip(tips)
    local last_tip = nil
    for id in string.gmatch(tips, "([^|]+)") do
        last_tip = id
    end
    return last_tip
end

function ExtractFirstId(ids)
    local first_id = nil
    for id in string.gmatch(ids, "([^,]+)") do
        first_id = id
    	break
    end
    return first_id
end

function getTooltipParam(item, where, param)
	FindItem(item, where)
	local id = FINDITEMID
	GetTooltip(id)
	local last_tip = ExtractLastTooltip(TOOLTIPTEXT)
	last_tip = last_tip:gsub(param .. ": ", "")
	
	return tonumber(last_tip)
end	

function checkItem(name, where) 
	local output = nil
	
	FindItem(name, where)
	
	output = FINDITEMID != "N/A"
	
	return output
end

function checkMinResource(item, where, min)	
	return getTooltipParam(item, where, "Weight") < min
end

function doTransfer(item, origin, destination)
	-- item:		string name of item ("sand")
	-- destination: string name of container (e.g. "packhorse")
	
	-- find origin container
	FindItem(origin)
	local from = tonumber(FINDITEMID)
	UseSelected(from)
	
	-- find destination container
	FindItem(destination)
	local to = tonumber(FINDITEMID)
	UseSelected(to)
	
	-- find item
	FindItem(item, from)
	local items = FINDITEMID
		
	for each_item in string.gmatch(items, "([^,]+)") do
	    Drag(each_item)
	    Dropc(to)
	    break
	end
end

function doCraftAll(tool, item)
	FindItem(tool, BACKPACKID)
	UseSelected(FINDITEMID)
	
	FindPanel("CraftingWindow")
	if FINDPANELID == "N/A" then
		FindButton("CraftingWindow", "favorites")
		ClickButton("CraftingWindow", FINDBUTTONNAME)
	
		FindButton("CraftingWindow", item)
		ClickButton("CraftingWindow", FINDBUTTONNAME)
	end
	
	FindButton("CraftingWindow", "Craft All")
	ClickButton("CraftingWindow", FINDBUTTONNAME)
	
	return
end

function doCraft(tool, item)
	FindItem(tool, BACKPACKID)
	UseSelected(FINDITEMID)
	
	FindPanel("CraftingWindow")
	if FINDPANELID == "N/A" then
		FindButton("CraftingWindow", "Categories")
		ClickButton("CraftingWindow", FINDBUTTONNAME)
		
		FindButton("CraftingWindow", "Alchemy")
		ClickButton("CraftingWindow", ExtractFirstId(FINDBUTTONNAME))
		
		FindButton("CraftingWindow", item)
		ClickButton("CraftingWindow", ExtractFirstId(FINDBUTTONNAME))
	end

	FindButton("CraftingWindow", "Craft")
	ClickButton("CraftingWindow", ExtractFirstId(FINDBUTTONNAME))
	
	return
end

while true do
	sleep(500)
	
	if checkItem("sand", BACKPACKID) and checkMinResource("sand", BACKPACKID, 8) then
		print("Need sand!")
		doTransfer("bottle", BACKPACKID, "saddlebags")
		doTransfer("sand",   "saddlebags", BACKPACKID)
	end
	
	if not checkItem("tinker's tool", BACKPACKID) then
		print("Tool destroyed!")
		doTransfer("tinker's tool", "saddlebags", BACKPACKID)
		--doCraftAll("tinker's tool", "Empty bottle")
	end
	
	doCraft("tinker's tool", "Empty bottle")
end
